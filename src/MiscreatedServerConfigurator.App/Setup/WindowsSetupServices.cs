using System.Diagnostics;
using System.IO;
using System.Net.Http;

using Microsoft.Win32;

using MiscreatedServerConfigurator.Core.Setup;

namespace MiscreatedServerConfigurator.App.Setup;

/// <summary>Runs a child process. Elevated runs show a UAC prompt and do not capture output.</summary>
public sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<int> RunAsync(ProcessRequest request, Action<string>? onChunk, CancellationToken cancellationToken)
    {
        if (request.Elevate)
        {
            return await RunElevatedAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var info = new ProcessStartInfo(request.FileName, request.Arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = string.IsNullOrEmpty(request.WorkingDirectory) ? Environment.CurrentDirectory : request.WorkingDirectory,
        };
        using var process = new Process { StartInfo = info, EnableRaisingEvents = true };
        if (!process.Start())
        {
            throw new InvalidOperationException("Could not start " + request.FileName + ".");
        }

        // Read the pipes while the process runs. Waiting first can deadlock once the buffer fills,
        // and BeginOutputReadLine hides carriage-return progress updates.
        var stdout = ReadRawAsync(process.StandardOutput, onChunk, cancellationToken);
        var stderr = ReadRawAsync(process.StandardError, onChunk, cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
        return process.ExitCode;
    }

    private static async Task ReadRawAsync(StreamReader reader, Action<string>? onChunk, CancellationToken cancellationToken)
    {
        var buffer = new char[256];
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return;
            }

            onChunk?.Invoke(new string(buffer, 0, read));
        }
    }

    private static async Task<int> RunElevatedAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(request.FileName, request.Arguments)
        {
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = string.IsNullOrEmpty(request.WorkingDirectory) ? Environment.CurrentDirectory : request.WorkingDirectory,
        };
        try
        {
            using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start " + request.FileName + ".");
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            throw new SetupCanceledException("Administrator approval was declined. Nothing was changed.");
        }
    }
}

/// <summary>Downloads a file with <see cref="HttpClient"/>.</summary>
public sealed class HttpFileDownloader : IFileDownloader
{
    /// <inheritdoc />
    public async Task DownloadAsync(Uri url, string destinationFile, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MiscreatedServerConfigurator");
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var directory = Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Reads new lines from SteamCMD's content_log.txt while an install is running.
/// The file is opened with shared access because SteamCMD keeps it open.
/// </summary>
public sealed class ContentLogTail
{
    private readonly string _path;
    private long _position;
    private string _partial = string.Empty;

    /// <summary>Starts at the current end of the log so only this run's new text is returned.</summary>
    /// <param name="path">The content_log.txt path.</param>
    public ContentLogTail(string path)
    {
        _path = path;
        if (File.Exists(path))
        {
            _position = new FileInfo(path).Length;
        }
    }

    /// <summary>Lines appended since the last call.</summary>
    /// <returns>Complete new lines, without their line endings.</returns>
    public IReadOnlyList<string> ReadNewLines()
    {
        if (!File.Exists(_path))
        {
            return Array.Empty<string>();
        }

        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (stream.Length < _position)
        {
            _position = 0;
            _partial = string.Empty;
        }

        if (stream.Length == _position)
        {
            return Array.Empty<string>();
        }

        stream.Position = _position;
        var count = (int)Math.Min(stream.Length - _position, 65536);
        var buffer = new byte[count];
        var read = stream.Read(buffer, 0, count);
        _position += read;
        var text = _partial + System.Text.Encoding.UTF8.GetString(buffer, 0, read);
        var parts = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        _partial = parts[^1];
        if (parts.Length == 1)
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>(parts.Length - 1);
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Length > 0)
            {
                lines.Add(parts[i]);
            }
        }

        return lines;
    }
}

/// <summary>Reads HKLM through the 64-bit registry view.</summary>
public sealed class WindowsRegistryView : IRegistryView
{
    /// <inheritdoc />
    public object? GetValue(string keyPath, string valueName)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(keyPath);
        return key?.GetValue(valueName);
    }
}
