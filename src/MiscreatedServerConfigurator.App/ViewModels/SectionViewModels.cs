using System.Collections.ObjectModel;

using MiscreatedServerConfigurator.Core.Catalog;
using MiscreatedServerConfigurator.Core.Weather;

namespace MiscreatedServerConfigurator.App.ViewModels;

/// <summary>
/// One choice in the theme setting.
/// </summary>
public sealed class ThemeOption
{
    /// <summary>
    /// Creates an option.
    /// </summary>
    /// <param name="value">The stored theme.</param>
    /// <param name="label">Text shown in the combo box.</param>
    public ThemeOption(Core.Theming.ThemePreference value, string label)
    {
        Value = value;
        Label = label;
    }

    /// <summary>The stored theme.</summary>
    public Core.Theming.ThemePreference Value { get; }

    /// <summary>Text shown in the combo box.</summary>
    public string Label { get; }

    /// <summary>Returns <see cref="Label"/> so a selection box that calls <see cref="object.ToString"/> does not show the type name.</summary>
    /// <returns>The friendly label.</returns>
    public override string ToString()
    {
        return Label;
    }
}

/// <summary>
/// One choice in the "unused CVAR" setting.
/// </summary>
public sealed class UnusedOutputOption
{
    /// <summary>Creates an option.</summary>
    /// <param name="value">The write mode.</param>
    /// <param name="label">Text shown in the combo box.</param>
    public UnusedOutputOption(Core.Hosting.UnusedCvarOutput value, string label)
    {
        Value = value;
        Label = label;
    }

    /// <summary>The write mode.</summary>
    public Core.Hosting.UnusedCvarOutput Value { get; }

    /// <summary>Text shown in the combo box.</summary>
    public string Label { get; }

    /// <summary>Returns <see cref="Label"/> so a selection box that calls <see cref="object.ToString"/> does not show the type name.</summary>
    /// <returns>The friendly label.</returns>
    public override string ToString()
    {
        return Label;
    }
}

/// <summary>
/// A headed group of CVAR rows inside a section.
/// </summary>
public sealed class BlockViewModel : ObservableObject
{
    private bool _isVisible = true;
    private bool _showRows = true;

    /// <summary>
    /// Creates a block.
    /// </summary>
    /// <param name="title">Block title from the template.</param>
    /// <param name="headerLines">Prose from inside the template banner.</param>
    /// <param name="bodyNotes">Comment lines in the block that are not CVARs.</param>
    /// <param name="rows">CVAR rows in catalog order.</param>
    public BlockViewModel(
        string title,
        IReadOnlyList<string> headerLines,
        IReadOnlyList<string> bodyNotes,
        ObservableCollection<CvarRowViewModel> rows)
    {
        Title = title;
        HeaderLines = headerLines;
        BodyNotes = bodyNotes;
        HeaderText = string.Join("\n", headerLines);
        NotesText = string.Join("\n", bodyNotes);
        Rows = rows;
    }

    /// <summary>Block title.</summary>
    public string Title { get; }

    /// <summary>Banner prose, one line per entry.</summary>
    public IReadOnlyList<string> HeaderLines { get; }

    /// <summary>Non-CVAR comments that belong to this block.</summary>
    public IReadOnlyList<string> BodyNotes { get; }

    /// <summary>Banner prose joined for display.</summary>
    public string HeaderText { get; }

    /// <summary>Block notes joined for display.</summary>
    public string NotesText { get; }

    /// <summary>CVAR rows.</summary>
    public ObservableCollection<CvarRowViewModel> Rows { get; }

    /// <summary>True when the block should be shown for the current search.</summary>
    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    /// <summary>True when the individual CVAR rows are listed. False when the damage matrix replaces them.</summary>
    public bool ShowRows
    {
        get => _showRows;
        private set => SetProperty(ref _showRows, value);
    }

    /// <summary>
    /// Applies the section search and whether rows are listed.
    /// </summary>
    /// <param name="query">Search text.</param>
    /// <param name="showRows">False when the matrix is showing instead of the row list.</param>
    public void ApplyFilter(string? query, bool showRows)
    {
        ShowRows = showRows;
        var any = false;
        foreach (var row in Rows)
        {
            row.ApplyFilter(showRows ? query : null);
            any |= row.IsVisible;
        }

        var searching = !string.IsNullOrWhiteSpace(query);
        IsVisible = !showRows || !searching || any;
    }
}

/// <summary>
/// One cell of the faction damage matrix.
/// </summary>
public sealed class DamageCellViewModel
{
    private DamageCellViewModel(string key, bool isMissing, CvarRowViewModel? row)
    {
        Key = key;
        IsMissing = isMissing;
        Row = row;
    }

    /// <summary>Creates a cell that edits <paramref name="row"/>.</summary>
    /// <param name="row">The CVAR row for this pair.</param>
    /// <returns>The cell.</returns>
    public static DamageCellViewModel Present(CvarRowViewModel row)
    {
        return new DamageCellViewModel(row.Key, isMissing: false, row);
    }

    /// <summary>
    /// Creates a non-editable cell. <c>g_gameRules_faction2_dmg_f2</c> is the pair the catalog omits on purpose:
    /// faction 2 is bases and vehicles, a base cannot damage a base, and vehicle damage is handled elsewhere.
    /// </summary>
    /// <param name="key">The name the documented pattern would have used.</param>
    /// <returns>A non-editable cell.</returns>
    public static DamageCellViewModel Missing(string key)
    {
        return new DamageCellViewModel(key, isMissing: true, row: null);
    }

    /// <summary>CVAR name, or the name the pattern would have used.</summary>
    public string Key { get; }

    /// <summary>True when the template has no line for this pair.</summary>
    public bool IsMissing { get; }

    /// <summary>True when the cell can be edited.</summary>
    public bool IsPresent => !IsMissing;

    /// <summary>The row, or null when <see cref="IsMissing"/> is true.</summary>
    public CvarRowViewModel? Row { get; }

    /// <summary>Tooltip text.</summary>
    public string ToolTip => IsMissing
        ? Key + " is intentionally absent. Faction 2 is bases and vehicles: a base cannot damage a base, and vehicle damage is handled elsewhere. Unspecified values use the server's stock value."
        : Key;
}

/// <summary>
/// One source-faction row of the damage matrix.
/// </summary>
public sealed class DamageRowViewModel
{
    /// <summary>
    /// Creates a row.
    /// </summary>
    /// <param name="label">Source faction label.</param>
    /// <param name="cells">One cell per target faction, in order from 0 through 15.</param>
    public DamageRowViewModel(string label, IReadOnlyList<DamageCellViewModel> cells)
    {
        Label = label;
        Cells = cells;
    }

    /// <summary>Source faction label.</summary>
    public string Label { get; }

    /// <summary>Cells for targets 0 through 15.</summary>
    public IReadOnlyList<DamageCellViewModel> Cells { get; }
}

/// <summary>
/// The 16 by 16 faction damage matrix.
/// </summary>
public sealed class DamageMatrixViewModel
{
    /// <summary>
    /// Creates the matrix.
    /// </summary>
    /// <param name="columnHeaders">Target labels, faction 0 through 15.</param>
    /// <param name="rows">One row per source faction.</param>
    public DamageMatrixViewModel(IReadOnlyList<string> columnHeaders, IReadOnlyList<DamageRowViewModel> rows)
    {
        ColumnHeaders = columnHeaders;
        Rows = rows;
    }

    /// <summary>Target labels across the top.</summary>
    public IReadOnlyList<string> ColumnHeaders { get; }

    /// <summary>Source rows.</summary>
    public IReadOnlyList<DamageRowViewModel> Rows { get; }
}

/// <summary>
/// One navigation section: Core, Logging, the damage matrix, or keys found only in the open file.
/// </summary>
public sealed class SectionViewModel : ObservableObject
{
    private readonly List<CvarRowViewModel> _unlistedRows = new();
    private int _enabledCount;
    private int _warningCount;
    private bool _showMatrix;
    private bool _showEmptySearch;

    /// <summary>
    /// Creates a section.
    /// </summary>
    /// <param name="section">Catalog section, or null for keys that are only in the open file.</param>
    /// <param name="title">Navigation title.</param>
    /// <param name="note">Section note, or null.</param>
    /// <param name="preamble">Opening comments. Only the Core section has them.</param>
    /// <param name="blocks">Blocks of rows.</param>
    /// <param name="matrix">Damage matrix, or null.</param>
    public SectionViewModel(
        CvarSection? section,
        string title,
        string? note,
        IReadOnlyList<string> preamble,
        ObservableCollection<BlockViewModel> blocks,
        DamageMatrixViewModel? matrix)
    {
        Section = section;
        Title = title;
        Note = note;
        PreambleText = string.Join("\n\n", preamble);
        Blocks = blocks;
        Matrix = matrix;
        RefreshEnabledCount();
        ApplyFilter(null);
    }

    /// <summary>Catalog section, or null for the "Other keys in this file" page.</summary>
    public CvarSection? Section { get; }

    /// <summary>Navigation title.</summary>
    public string Title { get; }

    /// <summary>True on the faction damage matrix page, including while a search replaces the grid with a list.</summary>
    public bool IsDamageSection => Section == CvarSection.FactionDamage;

    /// <summary>True on the weather page, where the day/night calculator is shown.</summary>
    public bool IsWeatherSection => Section == CvarSection.Weather;

    /// <summary>Informational note for the section, or null.</summary>
    public string? Note { get; }

    /// <summary>Opening comments from the reference template. Empty except on Core.</summary>
    public string PreambleText { get; }

    /// <summary>Blocks in template order.</summary>
    public ObservableCollection<BlockViewModel> Blocks { get; }

    /// <summary>Damage matrix, or null when this section is not the matrix.</summary>
    public DamageMatrixViewModel? Matrix { get; }

    /// <summary>How many CVARs in this section will be written as live lines.</summary>
    public int EnabledCount
    {
        get => _enabledCount;
        private set => SetProperty(ref _enabledCount, value);
    }

    /// <summary>How many validation warnings belong to this section.</summary>
    public int WarningCount
    {
        get => _warningCount;
        private set
        {
            if (SetProperty(ref _warningCount, value))
            {
                Raise(nameof(HasWarnings));
            }
        }
    }

    /// <summary>True when the nav entry should show a warning badge.</summary>
    public bool HasWarnings => _warningCount > 0;

    /// <summary>Updates the nav badge from the warnings for this section.</summary>
    /// <param name="count">Warning count. Zero hides the badge.</param>
    public void SetWarningCount(int count)
    {
        WarningCount = count;
    }

    /// <summary>True when the matrix is on screen instead of the row list.</summary>
    public bool ShowMatrix
    {
        get => _showMatrix;
        private set => SetProperty(ref _showMatrix, value);
    }

    /// <summary>True when a search matches nothing in this section.</summary>
    public bool ShowEmptySearch
    {
        get => _showEmptySearch;
        private set => SetProperty(ref _showEmptySearch, value);
    }

    /// <summary>
    /// Counts CVARs that stay out of the row list. The day/night scales are edited in the panel instead.
    /// </summary>
    /// <param name="rows">Rows to include in the live-setting count.</param>
    public void TrackUnlistedRows(IEnumerable<CvarRowViewModel> rows)
    {
        _unlistedRows.Clear();
        _unlistedRows.AddRange(rows);
        RefreshEnabledCount();
    }

    /// <summary>Recounts live CVARs.</summary>
    public void RefreshEnabledCount()
    {
        var count = 0;
        foreach (var block in Blocks)
        {
            foreach (var row in block.Rows)
            {
                if (row.Enabled)
                {
                    count++;
                }
            }
        }

        foreach (var row in _unlistedRows)
        {
            if (row.Enabled)
            {
                count++;
            }
        }

        EnabledCount = count;
    }

    /// <summary>
    /// Filters rows. An empty query shows everything. The matrix is replaced by the row list while searching.
    /// </summary>
    /// <param name="query">Search text.</param>
    public void ApplyFilter(string? query)
    {
        var searching = !string.IsNullOrWhiteSpace(query);
        ShowMatrix = Matrix is not null && !searching;
        var any = false;
        foreach (var block in Blocks)
        {
            block.ApplyFilter(query, showRows: !ShowMatrix);
            if (ShowMatrix)
            {
                continue;
            }

            foreach (var row in block.Rows)
            {
                any |= row.IsVisible;
            }
        }

        var panelMatch = Section == CvarSection.Weather && DayNightCalculator.PanelMatchesSearch(query);
        ShowEmptySearch = searching && !any && !panelMatch;
    }
}
