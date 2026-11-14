namespace MiscreatedServerConfigurator.Core.Catalog;

/// <summary>
/// One headed block from the reference template, such as "Ping Settings" or "Configure faction names".
/// </summary>
public sealed class CvarSectionBlock
{
    /// <summary>
    /// Creates a block.
    /// </summary>
    /// <param name="section">The editor section this block belongs to.</param>
    /// <param name="title">The title line between the template's banner rules.</param>
    /// <param name="headerLines">Prose inside the banner, after the title.</param>
    /// <param name="bodyNotes">Comment lines in the block that are not themselves CVARs.</param>
    /// <param name="entries">CVAR entries in template order.</param>
    public CvarSectionBlock(
        CvarSection section,
        string title,
        IReadOnlyList<string> headerLines,
        IReadOnlyList<string> bodyNotes,
        IReadOnlyList<CvarDefinition> entries)
    {
        Section = section;
        Title = title;
        HeaderLines = headerLines;
        BodyNotes = bodyNotes;
        Entries = entries;
    }

    /// <summary>Editor section this block belongs to.</summary>
    public CvarSection Section { get; }

    /// <summary>Title line from the template.</summary>
    public string Title { get; }

    /// <summary>Prose inside the banner, after the title.</summary>
    public IReadOnlyList<string> HeaderLines { get; }

    /// <summary>Comment lines in the block that are not CVAR assignments.</summary>
    public IReadOnlyList<string> BodyNotes { get; }

    /// <summary>CVAR entries in template order, including keys offered because a cited source names them.</summary>
    public IReadOnlyList<CvarDefinition> Entries { get; }
}
