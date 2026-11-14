namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// The role of one physical line in a hosting.cfg file.
/// </summary>
public enum HostingLineKind
{
    /// <summary>An empty line, or a line that is only whitespace.</summary>
    Blank,

    /// <summary>A comment that is not a commented-out <c>key=value</c> assignment.</summary>
    Comment,

    /// <summary>A live <c>key=value</c> assignment. The server will read it.</summary>
    Assignment,

    /// <summary>
    /// A comment whose text is a <c>key=value</c> assignment.
    /// The server ignores it and uses the factory default.
    /// </summary>
    CommentedAssignment,

    /// <summary>A line that is neither a comment nor an assignment. Preserved so a save cannot drop it.</summary>
    Text,
}
