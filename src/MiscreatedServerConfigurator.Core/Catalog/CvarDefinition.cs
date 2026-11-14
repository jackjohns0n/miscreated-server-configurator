namespace MiscreatedServerConfigurator.Core.Catalog;

/// <summary>
/// One hosting.cfg CVAR in the catalog.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TemplateValue"/> is the value written in the 13 November 2026 reference template.
/// That file's header calls those commented values the defaults. Where official help or
/// <c>miscreatedcvars.txt</c> disagrees, the disagreement is stored in
/// <see cref="InformationalNote"/> and the template value is left unchanged.
/// </para>
/// <para>
/// A null <see cref="Description"/>, <see cref="TemplateValue"/>, or <see cref="InformationalNote"/>
/// means the sources used for this catalog do not say. Nothing is filled in by guesswork.
/// </para>
/// </remarks>
public sealed class CvarDefinition
{
    /// <summary>
    /// Creates a catalog entry. The caller sets every property.
    /// </summary>
    public CvarDefinition()
    {
    }

    /// <summary>CVAR name, spelled the way it is written to hosting.cfg.</summary>
    public required string Key { get; init; }

    /// <summary>Editor section that contains this CVAR.</summary>
    public required CvarSection Section { get; init; }

    /// <summary>Title of the template block this CVAR belongs to.</summary>
    public required string BlockTitle { get; init; }

    /// <summary>
    /// What the CVAR does, from the template comment or another cited source.
    /// Null when no source describes it.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Value recorded in the reference template. Null when the template has no line for this key.
    /// An empty string is a real template value (the line was <c>key=</c>).
    /// </summary>
    public string? TemplateValue { get; init; }

    /// <summary>True when the reference template has an assignment line for this key.</summary>
    public bool PresentInTemplate { get; init; }

    /// <summary>
    /// True for the keys the template header calls required: <c>http_password</c> and <c>sv_servername</c>.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// True when the value is a password or API key. The editor masks it and does not copy it anywhere except the file the user saves.
    /// </summary>
    public bool IsSecret { get; init; }

    /// <summary>How the editor presents the value.</summary>
    public CvarEditorKind Kind { get; init; } = CvarEditorKind.Text;

    /// <summary>
    /// Disagreement or gap worth showing next to the field. Null when there is nothing extra to say.
    /// Informational only: the editor does not rewrite template values to match it.
    /// </summary>
    public string? InformationalNote { get; init; }

    /// <summary>Source faction for a damage-matrix key; otherwise null.</summary>
    public int? DamageSource { get; init; }

    /// <summary>Target faction for a damage-matrix key; otherwise null.</summary>
    public int? DamageTarget { get; init; }
}
