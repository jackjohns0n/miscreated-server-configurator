namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// What to do with a CVAR the user is not setting.
/// An unused CVAR means the server keeps its factory default.
/// </summary>
public enum UnusedCvarOutput
{
    /// <summary>
    /// Write the CVAR as a comment (<c>-- key=value</c>), or keep the original comment line when it has not changed.
    /// </summary>
    CommentOut,

    /// <summary>
    /// Leave the CVAR out of the file. Prose comments that are not CVAR lines are kept.
    /// </summary>
    Omit,
}
