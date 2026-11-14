namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// Recognizes hosting.cfg lines.
/// </summary>
/// <remarks>
/// <para>
/// A line whose first non-whitespace characters are <c>--</c> is a comment.
/// The reference template writes that prefix as dash, dash, space.
/// A live setting is <c>key=value</c> with no trailing remark. The server's own header says a remark
/// after the value breaks parsing, so a live line's value is everything after the first <c>=</c>.
/// </para>
/// <para>
/// A commented assignment is a comment whose body starts with <c>key=value</c>.
/// The template separates a remark with <c> - </c>. One line (<c>sv_password</c>) has no separator:
/// the value is a quoted string and the remark follows the closing quote. Quoted values are taken
/// first so a remark marker inside quotes stays part of the value.
/// </para>
/// </remarks>
public static class HostingSyntax
{
    /// <summary>
    /// Returns <see langword="true"/> when the line is a comment (it starts with <c>--</c> after leading whitespace).
    /// </summary>
    /// <param name="raw">Line text without the newline.</param>
    /// <returns><see langword="true"/> when the line is a comment.</returns>
    public static bool IsCommentLine(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return raw.TrimStart().StartsWith("--", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the text after the <c>--</c> comment prefix.
    /// One leading space is removed, matching the template's <c>-- </c> prefix. The rest is left intact.
    /// </summary>
    /// <param name="raw">A comment line.</param>
    /// <returns>The comment body. Empty when the line is only the prefix.</returns>
    public static string CommentBody(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var trimmed = raw.TrimStart();
        if (!trimmed.StartsWith("--", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var body = trimmed[2..];
        if (body.StartsWith(' '))
        {
            body = body[1..];
        }

        return body;
    }

    /// <summary>
    /// Parses <c>key=value</c> from the start of <paramref name="text"/>.
    /// </summary>
    /// <param name="text">A full line, or the body of a comment line.</param>
    /// <param name="splitTrailingComment">
    /// <see langword="true"/> for a commented assignment, so a remark after the value is returned separately.
    /// <see langword="false"/> for a live line, so the entire remainder is the value.
    /// </param>
    /// <param name="key">The CVAR name when the method returns <see langword="true"/>.</param>
    /// <param name="value">The value when the method returns <see langword="true"/>. May be empty.</param>
    /// <param name="trailingComment">The remark, or null when there is none or splitting is off.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> starts with a CVAR assignment.</returns>
    public static bool TryParseAssignment(
        string text,
        bool splitTrailingComment,
        out string key,
        out string value,
        out string? trailingComment)
    {
        ArgumentNullException.ThrowIfNull(text);
        key = string.Empty;
        value = string.Empty;
        trailingComment = null;

        var body = text.Trim();
        if (body.Length == 0)
        {
            return false;
        }

        var separator = body.IndexOf('=', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return false;
        }

        var keyText = body[..separator];
        if (!IsKey(keyText))
        {
            return false;
        }

        key = keyText;
        var rest = body[(separator + 1)..];
        if (!splitTrailingComment)
        {
            value = rest;
        }
        else
        {
            SplitCommentedValue(rest, out value, out trailingComment);
        }

        // A remark may contain "=", as the g_gameRules_bases comment does.
        // Another "=" in the value itself means the line is prose that happens to start
        // with a token, such as "f0=players, f1=AI", not a single CVAR.
        if (ContainsUnquotedEquals(value))
        {
            key = string.Empty;
            value = string.Empty;
            trailingComment = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="key"/> is a hosting.cfg key token.
    /// </summary>
    /// <param name="key">Candidate key.</param>
    /// <returns><see langword="true"/> for a letter or underscore followed by letters, digits, or underscores.</returns>
    public static bool IsKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        if (!(char.IsAsciiLetter(key[0]) || key[0] == '_'))
        {
            return false;
        }

        for (var i = 1; i < key.Length; i++)
        {
            var c = key[i];
            if (!(char.IsAsciiLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Splits a commented value into the value itself and an optional remark.
    /// A quoted value includes its quotes. A remark may follow the closing quote with or without <c> - </c>.
    /// An unquoted value ends at the first <c> - </c>.
    /// </summary>
    private static void SplitCommentedValue(string rest, out string value, out string? trailingComment)
    {
        if (rest.Length == 0)
        {
            value = string.Empty;
            trailingComment = null;
            return;
        }

        if (rest[0] == '"')
        {
            var close = rest.IndexOf('"', 1);
            if (close >= 0)
            {
                value = rest[..(close + 1)];
                trailingComment = NormalizeTrailing(rest[(close + 1)..]);
                return;
            }
        }

        const string marker = " - ";
        var markerAt = rest.IndexOf(marker, StringComparison.Ordinal);
        if (markerAt >= 0)
        {
            value = rest[..markerAt];
            var remark = rest[(markerAt + marker.Length)..].Trim();
            trailingComment = remark.Length == 0 ? null : remark;
            return;
        }

        value = rest;
        trailingComment = null;
    }

    private static bool ContainsUnquotedEquals(string value)
    {
        var inQuotes = false;
        foreach (var c in value)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == '=' && !inQuotes)
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeTrailing(string after)
    {
        var trimmed = after.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.StartsWith("- ", StringComparison.Ordinal))
        {
            trimmed = trimmed[2..].Trim();
        }

        return trimmed.Length == 0 ? null : trimmed;
    }
}
