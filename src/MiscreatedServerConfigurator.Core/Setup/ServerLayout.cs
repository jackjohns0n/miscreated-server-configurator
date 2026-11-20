namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>
/// Folder names under the server root. The root is chosen by the user. Nothing here defaults to Documents or Program Files.
/// </summary>
public static class ServerLayout
{
    /// <summary>Shared SteamCMD directory name under the root. Downloaded once.</summary>
    public const string SteamCmdDirectoryName = "steamcmd";

    /// <summary>Start script written into each server folder. The app does not launch it.</summary>
    public const string StartScriptFileName = "StartServer.bat";

    /// <summary>hosting.cfg file name. Content comes from the editor, not from a second generator.</summary>
    public const string HostingConfigFileName = "hosting.cfg";

    /// <summary>Relative path of the dedicated server executable, from the server folder.</summary>
    public const string DedicatedExeRelativePath = "Bin64_dedicated/MiscreatedServer.exe";

    /// <summary>Workshop manifest copied next to the downloaded items.</summary>
    public const string WorkshopManifestFileName = "appworkshop_299740.acf";

    /// <summary>Shared SteamCMD folder for <paramref name="root"/>.</summary>
    /// <param name="root">The folder that holds every server and the one SteamCMD install.</param>
    /// <returns>The steamcmd directory path.</returns>
    public static string SteamCmdDirectory(string root)
    {
        return Path.Combine(root, SteamCmdDirectoryName);
    }

    /// <summary><c>steamcmd.exe</c> inside the shared install.</summary>
    /// <param name="root">The server root.</param>
    /// <returns>The executable path.</returns>
    public static string SteamCmdExecutable(string root)
    {
        return Path.Combine(SteamCmdDirectory(root), "steamcmd.exe");
    }

    /// <summary>One server's folder, <c>&lt;root&gt;\&lt;name&gt;</c>.</summary>
    /// <param name="root">The server root.</param>
    /// <param name="serverName">The folder name.</param>
    /// <returns>The server directory.</returns>
    public static string ServerDirectory(string root, string serverName)
    {
        return Path.Combine(root, serverName);
    }

    /// <summary>True when <c>Bin64_dedicated\MiscreatedServer.exe</c> is present.</summary>
    /// <param name="serverDirectory">The server folder.</param>
    /// <returns><see langword="true"/> when the dedicated executable is on disk.</returns>
    public static bool DedicatedExecutableExists(string serverDirectory)
    {
        return File.Exists(Path.Combine(serverDirectory, "Bin64_dedicated", "MiscreatedServer.exe"));
    }

    /// <summary>
    /// Rejects an empty path, a relative path, Program Files, and the Documents folder itself.
    /// A subfolder the user typed is allowed. The box starts empty, so those locations are never filled in for them.
    /// </summary>
    /// <param name="path">The proposed root.</param>
    /// <param name="error">Why it was rejected, or null.</param>
    /// <returns><see langword="true"/> when the folder may be used.</returns>
    public static bool TryValidateRoot(string? path, out string? error)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Choose a folder for the servers. The app does not pick Documents or Program Files.";
            return false;
        }

        var full = Path.GetFullPath(path.Trim());
        if (!Path.IsPathRooted(full))
        {
            error = "Use a full path, for example D:\\MiscreatedServers.";
            return false;
        }

        if (IsProgramFiles(full) || IsDocumentsFolder(full))
        {
            error = "That folder is Documents or Program Files. Choose another folder, such as D:\\MiscreatedServers.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>Accepts a single folder name: letters, digits, space, underscore, and hyphen.</summary>
    /// <param name="name">The proposed server folder name.</param>
    /// <param name="error">Why it was rejected, or null.</param>
    /// <returns><see langword="true"/> when the name can be a directory.</returns>
    public static bool TryValidateServerName(string? name, out string? error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Enter a server folder name.";
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed is "." or ".." || trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            error = "The server name cannot contain a path or reserved characters.";
            return false;
        }

        foreach (var ch in trimmed)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is ' ' or '_' or '-')
            {
                continue;
            }

            error = "Use letters, digits, spaces, underscores, or hyphens in the server name.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsProgramFiles(string fullPath)
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        return IsSameOrUnder(fullPath, programFiles) || IsSameOrUnder(fullPath, programFilesX86);
    }

    private static bool IsDocumentsFolder(string fullPath)
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return PathsEqual(fullPath, documents);
    }

    private static bool IsSameOrUnder(string fullPath, string parent)
    {
        if (string.IsNullOrEmpty(parent))
        {
            return false;
        }

        var full = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var root = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return PathsEqual(full, root)
            || full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            left.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            right.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
