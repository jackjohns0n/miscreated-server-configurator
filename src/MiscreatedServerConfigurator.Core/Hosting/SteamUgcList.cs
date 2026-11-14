namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// Reads and writes the <c>steam_ugc</c> value: Workshop file ids separated by commas, with no spaces.
/// </summary>
public static class SteamUgcList
{
    /// <summary>
    /// Splits a <c>steam_ugc</c> value. Whitespace around commas is ignored. Empty entries are dropped.
    /// The original tokens are kept, including duplicates, so a save can round-trip an untouched value.
    /// </summary>
    /// <param name="value">The raw CVAR value. Null or blank yields an empty list.</param>
    /// <returns>Ids in order.</returns>
    public static IReadOnlyList<string> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        var parts = value.Split(',');
        var ids = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            var id = part.Trim();
            if (id.Length > 0)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    /// <summary>
    /// Joins ids with commas and no spaces.
    /// </summary>
    /// <param name="ids">Ids to write. Blank entries are skipped.</param>
    /// <returns>The <c>steam_ugc</c> value.</returns>
    /// <exception cref="ArgumentException">An id contains a space or a comma.</exception>
    public static string Format(IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var formatted = new List<string>();
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var trimmed = id.Trim();
            if (trimmed.Contains(' ', StringComparison.Ordinal) || trimmed.Contains(',', StringComparison.Ordinal))
            {
                throw new ArgumentException("A Workshop file id cannot contain a space or a comma.", nameof(ids));
            }

            formatted.Add(trimmed);
        }

        return string.Join(",", formatted);
    }

    /// <summary>
    /// Reads one Workshop file id from a numeric token or from a Workshop URL's <c>id</c> query parameter.
    /// </summary>
    /// <param name="text">An id, or a URL such as a Steam Community file-details link.</param>
    /// <param name="id">The numeric id when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when a numeric id was found.</returns>
    public static bool TryExtractId(string? text, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (IsDigits(trimmed))
        {
            id = trimmed;
            return true;
        }

        // Only scrape an id out of something that is actually a URL.
        // A bare "id=123" in a sentence is not accepted.
        var looksLikeUrl = trimmed.Contains("://", StringComparison.Ordinal)
            || trimmed.Contains("steamcommunity.com", StringComparison.OrdinalIgnoreCase);
        if (!looksLikeUrl)
        {
            return false;
        }

        var marker = trimmed.IndexOf("id=", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            return false;
        }

        var start = marker + 3;
        var end = start;
        while (end < trimmed.Length && char.IsAsciiDigit(trimmed[end]))
        {
            end++;
        }

        if (end == start)
        {
            return false;
        }

        id = trimmed[start..end];
        return true;
    }

    /// <summary>
    /// Parses the add-field text: one id, one Workshop URL, or a comma-separated list of either.
    /// </summary>
    /// <param name="text">Text from the Workshop id field.</param>
    /// <param name="ids">Ids in order when the method returns <see langword="true"/>.</param>
    /// <param name="error">A short reason when the method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when every entry was a numeric Workshop file id.</returns>
    public static bool TryParseUserInput(string? text, out IReadOnlyList<string> ids, out string? error)
    {
        ids = Array.Empty<string>();
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter a Workshop file id or a Workshop URL.";
            return false;
        }

        var pieces = text.Split(',');
        var parsed = new List<string>(pieces.Length);
        foreach (var piece in pieces)
        {
            if (!TryExtractId(piece, out var id))
            {
                error = "Could not read a numeric Workshop file id from \"" + Clip(piece) + "\".";
                return false;
            }

            parsed.Add(id);
        }

        ids = parsed;
        return true;
    }

    private static bool IsDigits(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (var c in text)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static string Clip(string text)
    {
        var trimmed = text.Trim();
        const int limit = 80;
        if (trimmed.Length <= limit)
        {
            return trimmed;
        }

        return trimmed[..limit] + "...";
    }
}
