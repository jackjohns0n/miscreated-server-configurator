namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// One line from a hosting.cfg file, plus the assignment parsed out of it when there is one.
/// </summary>
public sealed class HostingLine
{
    /// <summary>
    /// Creates a line record.
    /// </summary>
    /// <param name="raw">The line text without the newline.</param>
    /// <param name="kind">How the line is classified.</param>
    /// <param name="key">The CVAR name when the line is an assignment; otherwise null.</param>
    /// <param name="value">
    /// The value when the line is an assignment. For a commented assignment this excludes the trailing remark.
    /// Null when the line is not an assignment.
    /// </param>
    /// <param name="trailingComment">
    /// The remark after a commented assignment, without the leading <c> - </c>.
    /// Null when the line is live, or when the comment has no remark.
    /// </param>
    public HostingLine(string raw, HostingLineKind kind, string? key, string? value, string? trailingComment)
    {
        Raw = raw;
        Kind = kind;
        Key = key;
        Value = value;
        TrailingComment = trailingComment;
    }

    /// <summary>The line text without the newline.</summary>
    public string Raw { get; }

    /// <summary>How the line is classified.</summary>
    public HostingLineKind Kind { get; }

    /// <summary>The CVAR name when the line is an assignment; otherwise null.</summary>
    public string? Key { get; }

    /// <summary>
    /// The assignment value, or null when the line is not an assignment.
    /// Commented assignments do not include the trailing remark in this value.
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// Remark that followed a commented assignment. Null for live lines and for comments with no remark.
    /// </summary>
    public string? TrailingComment { get; }
}
