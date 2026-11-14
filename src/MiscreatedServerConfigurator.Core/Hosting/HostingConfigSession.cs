using MiscreatedServerConfigurator.Core.Catalog;

namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// A hosting.cfg open for editing: the original lines, the catalog, and the current value of each CVAR.
/// </summary>
public sealed class HostingConfigSession
{
    private readonly Dictionary<string, CvarValueState> _states;

    private HostingConfigSession(CvarCatalog catalog, HostingConfigDocument document, Dictionary<string, CvarValueState> states)
    {
        Catalog = catalog;
        Document = document;
        _states = states;
    }

    /// <summary>The CVAR catalog used to recognize keys.</summary>
    public CvarCatalog Catalog { get; }

    /// <summary>The file that was loaded, unchanged.</summary>
    public HostingConfigDocument Document { get; }

    /// <summary>True when any CVAR's enabled flag or value differs from the loaded file.</summary>
    public bool IsModified => _states.Values.Any(static state => state.IsModified);

    /// <summary>
    /// Loads a hosting.cfg and pairs it with the catalog.
    /// </summary>
    /// <param name="text">File contents.</param>
    /// <param name="catalog">Catalog to use. The reference catalog is used when this is null.</param>
    /// <returns>A session whose unmodified comment-out save reproduces <paramref name="text"/>.</returns>
    public static HostingConfigSession Load(string text, CvarCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        catalog ??= CvarCatalog.Default;
        var document = HostingConfigDocument.Parse(text);
        var states = new Dictionary<string, CvarValueState>(StringComparer.Ordinal);
        foreach (var entry in catalog.Entries)
        {
            states[entry.Key] = CvarValueState.Unused(entry.Key);
        }

        var linesByKey = new Dictionary<string, List<HostingLine>>(StringComparer.Ordinal);
        foreach (var line in document.Lines)
        {
            if (line.Key is null)
            {
                continue;
            }

            if (!linesByKey.TryGetValue(line.Key, out var list))
            {
                list = new List<HostingLine>();
                linesByKey[line.Key] = list;
            }

            list.Add(line);
        }

        // One editor value per key. If the file repeats a key, the last live assignment wins,
        // and a live assignment wins over a commented copy of the same key.
        foreach (var pair in linesByKey)
        {
            var list = pair.Value;
            var active = list.LastOrDefault(static line => line.Kind == HostingLineKind.Assignment);
            var chosen = active ?? list[^1];
            states[pair.Key] = CvarValueState.FromFile(
                pair.Key,
                enabled: active is not null,
                value: chosen.Value ?? string.Empty,
                trailingComment: chosen.TrailingComment);
        }

        return new HostingConfigSession(catalog, document, states);
    }

    /// <summary>
    /// Returns the state for a key that is in the catalog or in the loaded file.
    /// </summary>
    /// <param name="key">CVAR name.</param>
    /// <returns>The current state.</returns>
    /// <exception cref="ArgumentException">The key is unknown.</exception>
    public CvarValueState GetRequired(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_states.TryGetValue(key, out var state))
        {
            return state;
        }

        throw new ArgumentException($"No CVAR named '{key}' is loaded.", nameof(key));
    }

    /// <summary>
    /// Catalog entries in catalog order, then keys that appear only in the loaded file, in file order.
    /// </summary>
    /// <returns>Every editable state.</returns>
    public IEnumerable<CvarValueState> StatesInEditOrder()
    {
        foreach (var entry in Catalog.Entries)
        {
            yield return _states[entry.Key];
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in Document.Lines)
        {
            if (line.Key is null || !seen.Add(line.Key))
            {
                continue;
            }

            if (Catalog.Find(line.Key) is null)
            {
                yield return _states[line.Key];
            }
        }
    }

    /// <summary>
    /// Keys that are in the loaded file and not in the catalog, in file order.
    /// </summary>
    /// <returns>Unknown keys. They are preserved on save.</returns>
    public IEnumerable<CvarValueState> FileOnlyStates()
    {
        foreach (var state in StatesInEditOrder())
        {
            if (Catalog.Find(state.Key) is null)
            {
                yield return state;
            }
        }
    }

    /// <summary>
    /// Sets whether <paramref name="key"/> is written as a live line.
    /// </summary>
    /// <param name="key">CVAR name.</param>
    /// <param name="enabled">True to write a live <c>key=value</c> line.</param>
    public void SetEnabled(string key, bool enabled)
    {
        GetRequired(key).SetEnabled(enabled);
    }

    /// <summary>
    /// Sets the value of <paramref name="key"/>. Newlines are removed so a value cannot break the line.
    /// </summary>
    /// <param name="key">CVAR name.</param>
    /// <param name="value">The new value. Null is treated as empty.</param>
    public void SetValue(string key, string? value)
    {
        var cleaned = (value ?? string.Empty)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
        GetRequired(key).SetValue(cleaned);
    }

    /// <summary>
    /// Writes the file.
    /// </summary>
    /// <param name="unusedOutput">How to treat CVARs that are not enabled.</param>
    /// <returns>The hosting.cfg text.</returns>
    /// <remarks>
    /// <para>
    /// Lines that the user has not changed are copied as they were loaded, so comments and ordering survive.
    /// A live line the user edits is written as <c>key=value</c> with no trailing remark.
    /// A CVAR that was not in the file is appended, in catalog order, only when the user enables it.
    /// </para>
    /// <para>
    /// Repeated keys are each written from the single edited value. The last live copy is the one the editor shows.
    /// </para>
    /// </remarks>
    public string ToText(UnusedCvarOutput unusedOutput)
    {
        var output = new List<string>(Document.Lines.Count + 4);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in Document.Lines)
        {
            if (line.Key is null)
            {
                output.Add(line.Raw);
                continue;
            }

            seen.Add(line.Key);
            var state = _states[line.Key];
            var lineIsActive = line.Kind == HostingLineKind.Assignment;
            var unchanged = state.Enabled == lineIsActive
                && string.Equals(state.Value, line.Value ?? string.Empty, StringComparison.Ordinal);
            if (unchanged)
            {
                if (!state.Enabled && unusedOutput == UnusedCvarOutput.Omit)
                {
                    continue;
                }

                output.Add(line.Raw);
                continue;
            }

            if (state.Enabled)
            {
                output.Add(line.Key + "=" + state.Value);
                continue;
            }

            if (unusedOutput == UnusedCvarOutput.Omit)
            {
                continue;
            }

            output.Add(FormatCommented(line.Key, state.Value, line.TrailingComment));
        }

        var extras = new List<string>();
        foreach (var entry in Catalog.Entries)
        {
            if (seen.Contains(entry.Key))
            {
                continue;
            }

            var state = _states[entry.Key];
            if (!state.Enabled)
            {
                continue;
            }

            extras.Add(entry.Key + "=" + state.Value);
        }

        if (extras.Count > 0)
        {
            if (output.Count == 0 || output[^1].Length > 0)
            {
                output.Add(string.Empty);
            }

            output.AddRange(extras);
        }

        return HostingConfigDocument.JoinLines(output, Document.NewLine, Document.EndsWithNewLine);
    }

    private static string FormatCommented(string key, string value, string? trailingComment)
    {
        if (string.IsNullOrEmpty(trailingComment))
        {
            return "-- " + key + "=" + value;
        }

        // Empty value plus a remark matches the template's "key= - remark" shape.
        return "-- " + key + "=" + value + " - " + trailingComment;
    }
}
