namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// A hosting.cfg file as an ordered list of lines. Parsing does not drop unknown lines.
/// </summary>
public sealed class HostingConfigDocument
{
    private HostingConfigDocument(IReadOnlyList<HostingLine> lines, string newLine, bool endsWithNewLine)
    {
        Lines = lines;
        NewLine = newLine;
        EndsWithNewLine = endsWithNewLine;
    }

    /// <summary>Lines in file order.</summary>
    public IReadOnlyList<HostingLine> Lines { get; }

    /// <summary>The newline this file used: <c>\r\n</c> when the text contained CRLF, otherwise <c>\n</c>.</summary>
    public string NewLine { get; }

    /// <summary>True when the file ended with a newline. A save keeps that choice.</summary>
    public bool EndsWithNewLine { get; }

    /// <summary>
    /// Parses hosting.cfg text.
    /// </summary>
    /// <param name="text">The file contents. A leading UTF-8 BOM is ignored.</param>
    /// <returns>The document.</returns>
    public static HostingConfigDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var endsWithNewLine = text.EndsWith('\n') || text.EndsWith('\r');
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        if (endsWithNewLine && normalized.EndsWith('\n'))
        {
            normalized = normalized[..^1];
        }

        if (normalized.Length == 0)
        {
            return new HostingConfigDocument(Array.Empty<HostingLine>(), newLine, endsWithNewLine);
        }

        var rawLines = normalized.Split('\n');
        var lines = new HostingLine[rawLines.Length];
        for (var i = 0; i < rawLines.Length; i++)
        {
            lines[i] = Classify(rawLines[i]);
        }

        return new HostingConfigDocument(lines, newLine, endsWithNewLine);
    }

    /// <summary>
    /// Joins lines with the newline style of the document they came from.
    /// </summary>
    /// <param name="lines">Line texts without newlines.</param>
    /// <param name="newLine">Newline sequence.</param>
    /// <param name="endsWithNewLine">Whether to put a newline after the last line.</param>
    /// <returns>The file text.</returns>
    public static string JoinLines(IReadOnlyList<string> lines, string newLine, bool endsWithNewLine)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentException.ThrowIfNullOrEmpty(newLine);

        if (lines.Count == 0)
        {
            return endsWithNewLine ? newLine : string.Empty;
        }

        var text = string.Join(newLine, lines);
        if (endsWithNewLine)
        {
            text += newLine;
        }

        return text;
    }

    private static HostingLine Classify(string raw)
    {
        if (raw.Trim().Length == 0)
        {
            return new HostingLine(raw, HostingLineKind.Blank, null, null, null);
        }

        if (HostingSyntax.IsCommentLine(raw))
        {
            if (HostingSyntax.TryParseAssignment(HostingSyntax.CommentBody(raw), splitTrailingComment: true, out var key, out var value, out var trailing))
            {
                return new HostingLine(raw, HostingLineKind.CommentedAssignment, key, value, trailing);
            }

            return new HostingLine(raw, HostingLineKind.Comment, null, null, null);
        }

        if (HostingSyntax.TryParseAssignment(raw, splitTrailingComment: false, out var liveKey, out var liveValue, out _))
        {
            return new HostingLine(raw, HostingLineKind.Assignment, liveKey, liveValue, null);
        }

        return new HostingLine(raw, HostingLineKind.Text, null, null, null);
    }
}
