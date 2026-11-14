namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// The server-browser name stored in <c>sv_servername</c>.
/// The file wraps the name in quotes. The editor shows the name itself and adds the quotes when it writes.
/// </summary>
public static class ServerNameValue
{
    /// <summary>The hosting.cfg key this quoting applies to.</summary>
    public const string Key = "sv_servername";

    /// <summary>
    /// Returns the name without one surrounding pair of quotes.
    /// <c>""</c> and a value that was not quoted both become the text the field should show.
    /// </summary>
    /// <param name="fileValue">The value as stored after <c>sv_servername=</c>.</param>
    /// <returns>The plain server name.</returns>
    public static string ToPlain(string? fileValue)
    {
        var text = fileValue ?? string.Empty;
        return TryUnwrap(text, out var inner) ? inner : text;
    }

    /// <summary>
    /// Writes the name the way hosting.cfg expects: <c>"name"</c>.
    /// A name that already has one surrounding pair is not wrapped a second time.
    /// An empty name is written as <c>""</c>.
    /// </summary>
    /// <param name="plainOrQuoted">The name from the field, with or without a surrounding pair of quotes.</param>
    /// <returns>The file value, including the quotes.</returns>
    public static string ToFile(string? plainOrQuoted)
    {
        var plain = ToPlain(plainOrQuoted);
        return "\"" + plain + "\"";
    }

    private static bool TryUnwrap(string text, out string inner)
    {
        inner = string.Empty;
        if (text.Length < 2 || text[0] != '"' || text[^1] != '"')
        {
            return false;
        }

        inner = text[1..^1];
        return true;
    }
}
