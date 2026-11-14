namespace MiscreatedServerConfigurator.Core.Hosting;

/// <summary>
/// The editor's value for one CVAR, and the value it had when the file was loaded.
/// </summary>
public sealed class CvarValueState
{
    private CvarValueState(
        string key,
        bool enabled,
        string value,
        bool originallyEnabled,
        string originalValue,
        string? trailingComment,
        bool presentInFile)
    {
        Key = key;
        Enabled = enabled;
        Value = value;
        OriginallyEnabled = originallyEnabled;
        OriginalValue = originalValue;
        TrailingComment = trailingComment;
        PresentInFile = presentInFile;
    }

    /// <summary>CVAR name.</summary>
    public string Key { get; }

    /// <summary>
    /// True when the CVAR will be written as a live <c>key=value</c> line.
    /// False when it is unused and the server keeps its factory default.
    /// </summary>
    public bool Enabled { get; private set; }

    /// <summary>The value to write when <see cref="Enabled"/> is true. May be empty.</summary>
    public string Value { get; private set; }

    /// <summary>True when the loaded file had this key as a live assignment.</summary>
    public bool OriginallyEnabled { get; }

    /// <summary>The value parsed from the loaded file, or empty when the file had no line for this key.</summary>
    public string OriginalValue { get; }

    /// <summary>
    /// Remark that followed the line this state was loaded from.
    /// Used when a changed line is written back as a comment. Null when there was no remark.
    /// </summary>
    public string? TrailingComment { get; }

    /// <summary>True when the loaded file contained a line for this key.</summary>
    public bool PresentInFile { get; }

    /// <summary>
    /// True when the enabled flag or the value differs from the loaded file.
    /// </summary>
    public bool IsModified =>
        Enabled != OriginallyEnabled || !string.Equals(Value, OriginalValue, StringComparison.Ordinal);

    /// <summary>Sets whether the CVAR is written as a live line.</summary>
    /// <param name="enabled">True to write a live line.</param>
    internal void SetEnabled(bool enabled)
    {
        Enabled = enabled;
    }

    /// <summary>Replaces the value. The caller has already removed newlines.</summary>
    /// <param name="value">The new value.</param>
    internal void SetValue(string value)
    {
        Value = value;
    }

    internal static CvarValueState Unused(string key)
    {
        return new CvarValueState(key, enabled: false, value: string.Empty, originallyEnabled: false, originalValue: string.Empty, trailingComment: null, presentInFile: false);
    }

    internal static CvarValueState FromFile(string key, bool enabled, string value, string? trailingComment)
    {
        return new CvarValueState(key, enabled, value, enabled, value, trailingComment, presentInFile: true);
    }
}
