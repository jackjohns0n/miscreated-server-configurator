using System.IO.Compression;

namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>Downloads Valve's SteamCMD zip once and extracts it into the shared folder.</summary>
public static class SteamCmdSetup
{
    /// <summary>
    /// Ensures <c>steamcmd.exe</c> exists. A second call does not download again when the executable is already there.
    /// </summary>
    /// <param name="steamCmdDirectory">The shared SteamCMD folder.</param>
    /// <param name="downloader">Downloads <see cref="MiscreatedApps.SteamCmdZipUrl"/>.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>A task that completes when <c>steamcmd.exe</c> is on disk.</returns>
    public static async Task EnsureAsync(string steamCmdDirectory, IFileDownloader downloader, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(steamCmdDirectory);
        ArgumentNullException.ThrowIfNull(downloader);
        var executable = Path.Combine(steamCmdDirectory, "steamcmd.exe");
        if (File.Exists(executable))
        {
            return;
        }

        Directory.CreateDirectory(steamCmdDirectory);
        var zipPath = Path.Combine(steamCmdDirectory, "steamcmd.zip");
        await downloader.DownloadAsync(MiscreatedApps.SteamCmdZipUrl, zipPath, cancellationToken).ConfigureAwait(false);
        ZipFile.ExtractToDirectory(zipPath, steamCmdDirectory, overwriteFiles: true);
        if (!File.Exists(executable))
        {
            throw new InvalidOperationException("The SteamCMD zip did not contain steamcmd.exe.");
        }
    }
}
