namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>
/// Decides whether hosting.cfg or the start script may be written into a server folder.
/// A folder is eligible only after Install server has put <c>Bin64_dedicated\MiscreatedServer.exe</c> there.
/// These writes do not create the folder and do not add a new saved server.
/// </summary>
public static class InstalledServerWrite
{
    /// <summary>Shown when the editor has no document to copy.</summary>
    public const string NothingLoaded = "Nothing is loaded in the editor, so hosting.cfg was not written.";

    /// <summary>Shown when the folder is not an installed dedicated server.</summary>
    public const string NotInstalled =
        "That folder is not an installed server. Bin64_dedicated\\MiscreatedServer.exe is missing. Install server or pick a known server. Nothing was written and no folder was created.";

    /// <summary>
    /// Allows a write into an installed server folder.
    /// </summary>
    /// <param name="serverDirectory">The server folder.</param>
    /// <param name="editorText">The editor's current hosting.cfg text. Required for a cfg write.</param>
    /// <param name="requireEditorText">True for hosting.cfg. False for the start script.</param>
    /// <param name="error">Why the write was refused, or null.</param>
    /// <returns><see langword="true"/> when the file may be written.</returns>
    public static bool TryAllow(string serverDirectory, string? editorText, bool requireEditorText, out string? error)
    {
        if (requireEditorText && string.IsNullOrWhiteSpace(editorText))
        {
            error = NothingLoaded;
            return false;
        }

        if (!ServerLayout.DedicatedExecutableExists(serverDirectory))
        {
            error = NotInstalled;
            return false;
        }

        error = null;
        return true;
    }
}
