using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.Core.Catalog;

/// <summary>
/// The hosting.cfg CVAR catalog built from the reference template, plus the few keys a cited source
/// tells the editor to offer even though the template has no assignment line for them.
/// </summary>
public sealed class CvarCatalog
{
    private readonly Dictionary<string, CvarDefinition> _byKey;

    private CvarCatalog(
        IReadOnlyList<string> preamble,
        IReadOnlyList<CvarSectionBlock> blocks,
        IReadOnlyList<CvarDefinition> entries,
        IReadOnlyList<CvarSection> sectionsInOrder,
        Dictionary<string, CvarDefinition> byKey)
    {
        Preamble = preamble;
        Blocks = blocks;
        Entries = entries;
        SectionsInOrder = sectionsInOrder;
        _byKey = byKey;
    }

    /// <summary>
    /// The catalog built from the embedded reference template.
    /// </summary>
    public static CvarCatalog Default => DefaultCatalog.Value;

    private static readonly Lazy<CvarCatalog> DefaultCatalog = new(static () => Create(ServerTemplate.LoadText()));

    /// <summary>Comment lines that appear before the first section banner.</summary>
    public IReadOnlyList<string> Preamble { get; }

    /// <summary>Template blocks in file order.</summary>
    public IReadOnlyList<CvarSectionBlock> Blocks { get; }

    /// <summary>Every catalog CVAR, in block order.</summary>
    public IReadOnlyList<CvarDefinition> Entries { get; }

    /// <summary>Sections in the order the template introduces them.</summary>
    public IReadOnlyList<CvarSection> SectionsInOrder { get; }

    /// <summary>
    /// Builds a catalog by reading <paramref name="templateText"/> the same way a hosting.cfg is parsed.
    /// </summary>
    /// <param name="templateText">The reference template.</param>
    /// <returns>The catalog.</returns>
    /// <exception cref="InvalidDataException">The template has a banner or a key the builder does not know how to place.</exception>
    public static CvarCatalog Create(string templateText)
    {
        ArgumentNullException.ThrowIfNull(templateText);
        var document = HostingConfigDocument.Parse(templateText);
        var preamble = new List<string>();
        var builders = new List<BlockBuilder>();
        var headerLines = new List<string>();
        var pending = new List<string>();
        BlockBuilder? current = null;
        var inHeader = false;

        foreach (var line in document.Lines)
        {
            if (line.Kind == HostingLineKind.Blank)
            {
                // A blank line ends a run of comments, so they describe the block rather than the next key.
                FlushPending(current, pending);
                continue;
            }

            if (IsBanner(line))
            {
                if (!inHeader)
                {
                    FlushPending(current, pending);
                    inHeader = true;
                    headerLines.Clear();
                }
                else
                {
                    if (headerLines.Count == 0)
                    {
                        throw new InvalidDataException("A section banner is missing its title.");
                    }

                    var title = headerLines[0];
                    if (!SectionByTitle.TryGetValue(title, out var section))
                    {
                        throw new InvalidDataException($"No section mapping for template title '{title}'.");
                    }

                    current = new BlockBuilder(section, title, headerLines.Skip(1).ToList());
                    builders.Add(current);
                    inHeader = false;
                    headerLines.Clear();
                }

                continue;
            }

            if (inHeader)
            {
                var headerText = DisplayText(line);
                if (headerText.Length > 0)
                {
                    headerLines.Add(headerText);
                }

                continue;
            }

            if (line.Key is not null)
            {
                if (current is null)
                {
                    throw new InvalidDataException($"CVAR '{line.Key}' appears before any section banner.");
                }

                string? description = line.TrailingComment;
                if (string.IsNullOrWhiteSpace(description) && pending.Count > 0)
                {
                    description = string.Join("\n", pending);
                }
                else if (pending.Count > 0)
                {
                    current.BodyNotes.AddRange(pending);
                }

                pending.Clear();
                current.Entries.Add(CreateFromLine(line, current, description));
                continue;
            }

            var note = DisplayText(line);
            if (note.Length == 0)
            {
                continue;
            }

            if (current is null)
            {
                preamble.Add(note);
            }
            else
            {
                pending.Add(note);
            }
        }

        if (inHeader)
        {
            throw new InvalidDataException("The reference template has an unclosed section banner.");
        }

        FlushPending(current, pending);
        InsertOfferedKeys(builders);

        var blocks = new List<CvarSectionBlock>(builders.Count);
        var entries = new List<CvarDefinition>();
        foreach (var builder in builders)
        {
            var frozen = builder.Entries.ToArray();
            blocks.Add(new CvarSectionBlock(builder.Section, builder.Title, builder.HeaderLines, builder.BodyNotes, frozen));
            entries.AddRange(frozen);
        }

        var byKey = new Dictionary<string, CvarDefinition>(entries.Count, StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (!byKey.TryAdd(entry.Key, entry))
            {
                throw new InvalidDataException($"Duplicate catalog key '{entry.Key}'.");
            }
        }

        var sections = new List<CvarSection>();
        foreach (var block in blocks)
        {
            if (!sections.Contains(block.Section))
            {
                sections.Add(block.Section);
            }
        }

        return new CvarCatalog(preamble, blocks, entries, sections, byKey);
    }

    /// <summary>
    /// Looks up a CVAR by its exact name.
    /// </summary>
    /// <param name="key">CVAR name.</param>
    /// <returns>The entry, or null when the catalog does not contain <paramref name="key"/>.</returns>
    public CvarDefinition? Find(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _byKey.TryGetValue(key, out var entry) ? entry : null;
    }

    /// <summary>
    /// Returns the section note, or null when the section has none.
    /// </summary>
    /// <param name="section">Editor section.</param>
    /// <returns>Informational text, or null.</returns>
    public string? SectionNote(CvarSection section)
    {
        return ReferenceNotes.SectionNotes.TryGetValue(section, out var note) ? note : null;
    }

    private static CvarDefinition CreateFromLine(HostingLine line, BlockBuilder block, string? description)
    {
        return CreateDefinition(
            line.Key!,
            block,
            description,
            templateValue: line.Value ?? string.Empty,
            presentInTemplate: true);
    }

    private static CvarDefinition CreateDefinition(
        string key,
        BlockBuilder block,
        string? description,
        string? templateValue,
        bool presentInTemplate)
    {
        var required = false;
        var secret = false;
        var kind = CvarEditorKind.Text;
        string? informationalNote = null;
        if (ReferenceNotes.TryGet(key, out var note))
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                description = note.Description;
            }

            required = note.IsRequired;
            secret = note.IsSecret;
            kind = note.Kind;
            informationalNote = note.InformationalNote;
        }

        int? damageSource = null;
        int? damageTarget = null;
        if (FactionDamageKey.TryParse(key, out var source, out var target))
        {
            damageSource = source;
            damageTarget = target;
        }

        return new CvarDefinition
        {
            Key = key,
            Section = block.Section,
            BlockTitle = block.Title,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            TemplateValue = templateValue,
            PresentInTemplate = presentInTemplate,
            IsRequired = required,
            IsSecret = secret,
            Kind = kind,
            InformationalNote = informationalNote,
            DamageSource = damageSource,
            DamageTarget = damageTarget,
        };
    }

    private static void InsertOfferedKeys(List<BlockBuilder> builders)
    {
        foreach (var offered in ReferenceNotes.OfferedKeys)
        {
            var block = builders.SingleOrDefault(item => item.Title == offered.BlockTitle)
                ?? throw new InvalidDataException($"Cannot place '{offered.Key}'. Block '{offered.BlockTitle}' was not found.");
            var definition = CreateDefinition(offered.Key, block, description: null, templateValue: null, presentInTemplate: false);
            if (offered.InsertBeforeKey is not null)
            {
                var index = block.Entries.FindIndex(item => item.Key == offered.InsertBeforeKey);
                if (index < 0)
                {
                    throw new InvalidDataException($"Cannot place '{offered.Key}' before missing key '{offered.InsertBeforeKey}'.");
                }

                block.Entries.Insert(index, definition);
            }
            else if (offered.InsertAfterKey is not null)
            {
                var index = block.Entries.FindIndex(item => item.Key == offered.InsertAfterKey);
                if (index < 0)
                {
                    throw new InvalidDataException($"Cannot place '{offered.Key}' after missing key '{offered.InsertAfterKey}'.");
                }

                block.Entries.Insert(index + 1, definition);
            }
            else
            {
                block.Entries.Add(definition);
            }
        }
    }

    private static void FlushPending(BlockBuilder? current, List<string> pending)
    {
        if (current is not null && pending.Count > 0)
        {
            current.BodyNotes.AddRange(pending);
        }

        pending.Clear();
    }

    private static bool IsBanner(HostingLine line)
    {
        if (line.Kind != HostingLineKind.Comment)
        {
            return false;
        }

        return DisplayText(line).StartsWith("===", StringComparison.Ordinal);
    }

    private static string DisplayText(HostingLine line)
    {
        var text = line.Kind is HostingLineKind.Comment or HostingLineKind.CommentedAssignment
            ? HostingSyntax.CommentBody(line.Raw)
            : line.Raw;
        return text.Trim();
    }

    private static readonly Dictionary<string, CvarSection> SectionByTitle = new(StringComparer.Ordinal)
    {
        ["Required Server Settings"] = CvarSection.Core,
        ["Core Server Settings"] = CvarSection.Core,
        ["Logging"] = CvarSection.Logging,
        ["Ping Settings"] = CvarSection.Ping,
        ["Steam"] = CvarSection.Steam,
        ["Weather System"] = CvarSection.Weather,
        ["Game Rules & Player"] = CvarSection.GameRulesAndPlayer,
        ["AI"] = CvarSection.Ai,
        ["Enable additional factions"] = CvarSection.Factions,
        ["Configure faction names"] = CvarSection.Factions,
        ["Set faction equipment for newly spawned players"] = CvarSection.Factions,
        ["Set fixed spawn locations for factions"] = CvarSection.Factions,
        ["Set Steam IDs for accounts allowed to use extra factions"] = CvarSection.Factions,
        ["Faction damage multipliers"] = CvarSection.FactionDamage,
    };

    private sealed class BlockBuilder
    {
        public BlockBuilder(CvarSection section, string title, List<string> headerLines)
        {
            Section = section;
            Title = title;
            HeaderLines = headerLines;
        }

        public CvarSection Section { get; }

        public string Title { get; }

        public List<string> HeaderLines { get; }

        public List<string> BodyNotes { get; } = new();

        public List<CvarDefinition> Entries { get; } = new();
    }
}
