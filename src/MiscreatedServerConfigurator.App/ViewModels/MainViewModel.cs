using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

using MiscreatedServerConfigurator.Core.Catalog;
using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.App.ViewModels;

/// <summary>
/// The hosting.cfg editor: the open file, the section list, and the unused-CVAR choice.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private HostingConfigSession _session;
    private string _loadedText;
    private string? _filePath;
    private UnusedCvarOutput _unusedOutput = UnusedCvarOutput.CommentOut;
    private string _searchText = string.Empty;
    private SectionViewModel? _selectedSection;
    private bool _isDirty;
    private string _statusText = string.Empty;

    /// <summary>
    /// Creates the editor and loads the reference template as a new, unsaved document.
    /// </summary>
    public MainViewModel()
    {
        UnusedOptions = new[]
        {
            new UnusedOutputOption(UnusedCvarOutput.CommentOut, "Comment them out"),
            new UnusedOutputOption(UnusedCvarOutput.Omit, "Omit them"),
        };
        Sections = new ObservableCollection<SectionViewModel>();
        Warnings = new ObservableCollection<ConfigWarning>();
        _session = HostingConfigSession.Load(string.Empty);
        _loadedText = string.Empty;
        Load(ServerTemplate.LoadText(), path: null);
    }

    /// <summary>Sections shown in the navigation list.</summary>
    public ObservableCollection<SectionViewModel> Sections { get; }

    /// <summary>Non-blocking checks against the template's own rules.</summary>
    public ObservableCollection<ConfigWarning> Warnings { get; }

    /// <summary>The two ways to write an unused CVAR.</summary>
    public IReadOnlyList<UnusedOutputOption> UnusedOptions { get; }

    /// <summary>The section currently on screen.</summary>
    public SectionViewModel? SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    /// <summary>
    /// How unused CVARs are written. Unused means the server keeps its factory default.
    /// </summary>
    public UnusedCvarOutput UnusedOutput
    {
        get => _unusedOutput;
        set
        {
            if (!SetProperty(ref _unusedOutput, value))
            {
                return;
            }

            Refresh();
        }
    }

    /// <summary>Filters CVAR rows by key, description, or note.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
            {
                return;
            }

            foreach (var section in Sections)
            {
                section.ApplyFilter(value);
            }
        }
    }

    /// <summary>Full path of the open file, or null for a new document.</summary>
    public string? FilePath
    {
        get => _filePath;
        private set => SetProperty(ref _filePath, value);
    }

    /// <summary>True when saving would change the file.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    /// <summary>True when Save should write: the document is new, or it has changes.</summary>
    public bool CanSave => IsDirty || string.IsNullOrEmpty(FilePath);

    /// <summary>Window title, including the file name and a dirty marker.</summary>
    public string WindowTitle
    {
        get
        {
            var name = string.IsNullOrEmpty(FilePath) ? "New hosting.cfg" : Path.GetFileName(FilePath);
            return "Miscreated Server Configurator — " + name + (IsDirty ? "*" : string.Empty);
        }
    }

    /// <summary>Status bar text. Does not include secret values.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Replaces the open document with the reference template.
    /// </summary>
    public void LoadTemplate()
    {
        SearchText = string.Empty;
        Load(ServerTemplate.LoadText(), path: null);
    }

    /// <summary>
    /// Replaces the open document with <paramref name="text"/>.
    /// </summary>
    /// <param name="text">File contents.</param>
    /// <param name="path">Full path, or null when the text is not yet saved.</param>
    public void LoadFile(string text, string? path)
    {
        ArgumentNullException.ThrowIfNull(text);
        SearchText = string.Empty;
        Load(text, path);
    }

    /// <summary>
    /// Builds the hosting.cfg text for the current values and unused-CVAR choice.
    /// </summary>
    /// <returns>The file text.</returns>
    public string BuildText()
    {
        return _session.ToText(UnusedOutput);
    }

    /// <summary>
    /// Marks the current text as the contents of <paramref name="path"/> after a successful save.
    /// </summary>
    /// <param name="path">The path that was written.</param>
    /// <param name="text">The text that was written.</param>
    public void NoteSaved(string path, string text)
    {
        _loadedText = text;
        FilePath = path;
        Refresh();
    }

    private void Load(string text, string? path)
    {
        var previous = SelectedSection?.Section;
        var keepOther = SelectedSection?.Section is null && SelectedSection is not null;
        _session = HostingConfigSession.Load(text);
        _loadedText = text;
        FilePath = path;
        Sections.Clear();

        var rowsByKey = new Dictionary<string, CvarRowViewModel>(StringComparer.Ordinal);
        foreach (var section in _session.Catalog.SectionsInOrder)
        {
            var blocks = new ObservableCollection<BlockViewModel>();
            foreach (var block in _session.Catalog.Blocks)
            {
                if (block.Section != section)
                {
                    continue;
                }

                var rows = new ObservableCollection<CvarRowViewModel>();
                foreach (var entry in block.Entries)
                {
                    var row = CreateRow(_session.GetRequired(entry.Key), entry);
                    rows.Add(row);
                    rowsByKey[entry.Key] = row;
                }

                blocks.Add(new BlockViewModel(block.Title, block.HeaderLines, block.BodyNotes, rows));
            }

            var preamble = section == CvarSection.Core
                ? _session.Catalog.Preamble
                : Array.Empty<string>();
            Sections.Add(new SectionViewModel(
                section,
                CvarSections.DisplayName(section),
                _session.Catalog.SectionNote(section),
                preamble,
                blocks,
                matrix: null));
        }

        AttachFileOnlyKeys(rowsByKey);
        AttachMatrix(rowsByKey);

        SectionViewModel? selected = null;
        if (keepOther)
        {
            selected = Sections.FirstOrDefault(section => section.Section is null);
        }

        selected ??= Sections.FirstOrDefault(section => section.Section == previous) ?? Sections.FirstOrDefault();
        SelectedSection = selected;
        Refresh();
    }

    private void AttachFileOnlyKeys(Dictionary<string, CvarRowViewModel> rowsByKey)
    {
        var damageRows = new ObservableCollection<CvarRowViewModel>();
        var otherRows = new ObservableCollection<CvarRowViewModel>();
        foreach (var state in _session.FileOnlyStates())
        {
            var row = CreateRow(state, definition: null);
            rowsByKey[state.Key] = row;
            if (FactionDamageKey.TryParse(state.Key, out _, out _))
            {
                damageRows.Add(row);
            }
            else
            {
                otherRows.Add(row);
            }
        }

        if (damageRows.Count > 0)
        {
            var damage = Sections.First(section => section.Section == CvarSection.FactionDamage);
            damage.Blocks.Add(new BlockViewModel(
                "In this file",
                Array.Empty<string>(),
                new[] { "These damage keys are in the open file and are not in the reference template." },
                damageRows));
        }

        if (otherRows.Count > 0)
        {
            Sections.Add(new SectionViewModel(
                section: null,
                title: "Other keys in this file",
                note: "These keys are in the open file and are not in the reference catalog. They are kept so a save does not drop them.",
                preamble: Array.Empty<string>(),
                blocks: new ObservableCollection<BlockViewModel>
                {
                    new("Keys", Array.Empty<string>(), Array.Empty<string>(), otherRows),
                },
                matrix: null));
        }
    }

    private void AttachMatrix(Dictionary<string, CvarRowViewModel> rowsByKey)
    {
        var damage = Sections.First(section => section.Section == CvarSection.FactionDamage);
        var headers = new List<string>(16);
        var matrixRows = new List<DamageRowViewModel>(16);

        // g_gameRules_faction2_dmg_f2 is intentionally absent, so the catalog has no row for it.
        // Faction 2 is bases/vehicles: a base cannot damage a base, and vehicle damage is handled elsewhere.
        // That cell is left out of the file. Any unspecified matrix value uses the server's stock value.
        for (var source = 0; source < 16; source++)
        {
            headers.Add(FactionDamageKey.Label(source));
            var cells = new List<DamageCellViewModel>(16);
            for (var target = 0; target < 16; target++)
            {
                var key = FactionDamageKey.Format(source, target);
                cells.Add(rowsByKey.TryGetValue(key, out var row)
                    ? DamageCellViewModel.Present(row)
                    : DamageCellViewModel.Missing(key));
            }

            matrixRows.Add(new DamageRowViewModel(FactionDamageKey.Label(source), cells));
        }

        var index = Sections.IndexOf(damage);
        var rebuilt = new SectionViewModel(
            damage.Section,
            damage.Title,
            damage.Note,
            damage.PreambleText.Length == 0 ? Array.Empty<string>() : new[] { damage.PreambleText },
            damage.Blocks,
            new DamageMatrixViewModel(headers, matrixRows));
        foreach (var block in rebuilt.Blocks)
        {
            foreach (var row in block.Rows)
            {
                row.Edited -= OnRowEdited;
                row.Edited += OnRowEdited;
            }
        }

        Sections[index] = rebuilt;
    }

    private CvarRowViewModel CreateRow(CvarValueState state, CvarDefinition? definition)
    {
        var row = new CvarRowViewModel(_session, state, definition);
        row.Edited += OnRowEdited;
        return row;
    }

    private void OnRowEdited(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        var live = 0;
        foreach (var section in Sections)
        {
            section.RefreshEnabledCount();
            section.ApplyFilter(SearchText);
            live += section.EnabledCount;
        }

        var written = _session.ToText(UnusedOutput);
        IsDirty = !string.Equals(written, _loadedText, StringComparison.Ordinal);
        Warnings.Clear();
        foreach (var warning in ConfigWarnings.Evaluate(_session))
        {
            Warnings.Add(warning);
        }

        var where = string.IsNullOrEmpty(FilePath) ? "New hosting.cfg" : FilePath;
        var mode = UnusedOutput == UnusedCvarOutput.Omit ? "unused CVARs omitted" : "unused CVARs commented out";
        StatusText = where + "  ·  " + live.ToString(System.Globalization.CultureInfo.InvariantCulture) + " live settings  ·  " + mode;
        Raise(nameof(WindowTitle));
        Raise(nameof(CanSave));
        CommandManager.InvalidateRequerySuggested();
    }
}
