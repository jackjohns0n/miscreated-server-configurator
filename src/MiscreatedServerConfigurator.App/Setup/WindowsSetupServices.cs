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
    public async Task<int> RunAsync(ProcessRequest request, Action<string>? onLine, CancellationToken cancellationToken)
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
        process.OutputDataReceived += (_, args) => Report(onLine, args.Data);
        process.ErrorDataReceived += (_, args) => Report(onLine, args.Data);
        if (!process.Start())
        {
            throw new InvalidOperationException("Could not start " + request.FileName + ".");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
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

    private static void Report(Action<string>? onLine, string? line)
    {
        if (onLine is not null && !string.IsNullOrEmpty(line))
        {
            onLine(line);
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
