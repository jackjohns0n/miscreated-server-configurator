namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>How to start one child process. Elevation is for firewall rules and runtime installers only.</summary>
/// <param name="FileName">Executable path.</param>
/// <param name="Arguments">Argument string.</param>
/// <param name="WorkingDirectory">Working directory. Empty uses the current directory.</param>
/// <param name="Elevate">When true, the Windows host shows a UAC prompt. Output cannot be captured.</param>
public readonly record struct ProcessRequest(string FileName, string Arguments, string WorkingDirectory, bool Elevate);

/// <summary>Runs a process and reports each output line. The Windows implementation lives in the app project.</summary>
public interface IProcessRunner
{
    /// <summary>Starts the process and waits for it to exit.</summary>
    /// <param name="request">What to run.</param>
    /// <param name="onLine">Called for each stdout or stderr line. Null skips capture. Not called when <see cref="ProcessRequest.Elevate"/> is true.</param>
    /// <param name="cancellationToken">Cancels the wait. The process is killed when the token fires.</param>
    /// <returns>The exit code. A declined UAC prompt throws <see cref="SetupCanceledException"/>.</returns>
    Task<int> RunAsync(ProcessRequest request, Action<string>? onLine, CancellationToken cancellationToken);
}

/// <summary>Downloads one file. Used for SteamCMD and the Visual C++ installers.</summary>
public interface IFileDownloader
{
    /// <summary>Saves <paramref name="url"/> to <paramref name="destinationFile"/>, replacing it when it exists.</summary>
    /// <param name="url">The file to download.</param>
    /// <param name="destinationFile">The full destination path.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>A task that completes when the file is on disk.</returns>
    Task DownloadAsync(Uri url, string destinationFile, CancellationToken cancellationToken);
}

/// <summary>The user declined an elevation prompt, or cancelled a running install.</summary>
public sealed class SetupCanceledException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">A sentence the setup window can show.</param>
    public SetupCanceledException(string message)
        : base(message)
    {
    }
}
