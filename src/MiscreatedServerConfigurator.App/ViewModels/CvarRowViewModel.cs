using System.Collections.ObjectModel;
using System.Collections.Specialized;

using MiscreatedServerConfigurator.Core.Catalog;
using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.App.ViewModels;

/// <summary>
/// One editable CVAR row.
/// </summary>
public sealed class CvarRowViewModel : ObservableObject
{
    private readonly HostingConfigSession _session;
    private readonly bool _isSteamUgc;
    private bool _enabled;
    private string _value;
    private bool _revealSecret;
    private bool _isVisible = true;
    private bool _suspendList;
    private bool _fromList;
    private string _workshopDraft = string.Empty;
    private string? _workshopError;

    /// <summary>
    /// Creates a row bound to <paramref name="state"/>.
    /// </summary>
    /// <param name="session">The open file.</param>
    /// <param name="state">Current value of the CVAR.</param>
    /// <param name="definition">Catalog entry, or null for a key that exists only in the open file.</param>
    public CvarRowViewModel(HostingConfigSession session, CvarValueState state, CvarDefinition? definition)
    {
        _session = session;
        Definition = definition;
        Key = state.Key;
        _enabled = state.Enabled;
        _value = Key == ServerNameValue.Key ? ServerNameValue.ToPlain(state.Value) : state.Value;
        _isSteamUgc = definition?.Kind == CvarEditorKind.SteamUgc;
        WorkshopIds = new ObservableCollection<string>();
        if (_isSteamUgc)
        {
            foreach (var id in SteamUgcList.Parse(state.Value))
            {
                WorkshopIds.Add(id);
            }

            WorkshopIds.CollectionChanged += OnWorkshopIdsChanged;
        }

        AddWorkshopCommand = new RelayCommand(AddWorkshop);
        RemoveWorkshopCommand = new RelayCommand<string>(RemoveWorkshop);
        UseTemplateValueCommand = new RelayCommand(UseTemplateValue, () => HasTemplateValue);
    }

    /// <summary>Raised after a change that can affect the saved file.</summary>
    public event EventHandler? Edited;

    /// <summary>Catalog entry, or null when the key exists only in the open file.</summary>
    public CvarDefinition? Definition { get; }

    /// <summary>CVAR name.</summary>
    public string Key { get; }

    /// <summary>What the CVAR does, or null when no source describes it.</summary>
    public string? Description => Definition?.Description;

    /// <summary>Informational disagreement or gap. Null when there is nothing extra to say.</summary>
    public string? InformationalNote => Definition?.InformationalNote;

    /// <summary>True when the template marks this key as required.</summary>
    public bool IsRequired => Definition?.IsRequired ?? false;

    /// <summary>True when the value is masked in the editor.</summary>
    public bool IsSecret =>
        (Definition?.IsSecret ?? false)
        || Key.Contains("password", StringComparison.OrdinalIgnoreCase)
        || Key.Contains("apikey", StringComparison.OrdinalIgnoreCase)
        || Key.Contains("api_key", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when this row edits <c>steam_ugc</c> as a list of Workshop ids.</summary>
    public bool IsSteamUgc => _isSteamUgc;

    /// <summary>True when the reference template recorded a value worth copying into the field.</summary>
    public bool HasTemplateValue => TemplateValuePolicy.IsReference(Definition?.TemplateValue);

    /// <summary>Label for the reference-template value, or empty when there is none.</summary>
    public string TemplateValueLabel =>
        HasTemplateValue ? "Reference template: " + Definition!.TemplateValue : string.Empty;

    /// <summary>
    /// Extra hint for the value field. Set for <c>sv_servername</c>, where the field is the plain name.
    /// </summary>
    public string? FieldHint => Key == ServerNameValue.Key
        ? "Your server name goes between the quotes; the app adds them."
        : null;

    /// <summary>True when the row matches the current search, or when there is no search.</summary>
    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// True when the CVAR is written as a live line.
    /// Unchecked means the server keeps its factory default.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (!SetProperty(ref _enabled, value))
            {
                return;
            }

            _session.SetEnabled(Key, value);
            Raise(nameof(LivePreview));
            Edited?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>The value written when <see cref="Enabled"/> is true.</summary>
    public string Value
    {
        get => _value;
        set
        {
            var cleaned = (value ?? string.Empty)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal);
            if (Key == ServerNameValue.Key)
            {
                cleaned = ServerNameValue.ToPlain(cleaned);
            }

            if (!SetProperty(ref _value, cleaned))
            {
                return;
            }

            var fileValue = Key == ServerNameValue.Key ? ServerNameValue.ToFile(cleaned) : cleaned;
            _session.SetValue(Key, fileValue);
            if (_isSteamUgc && !_fromList)
            {
                ReloadWorkshopIds();
            }

            Raise(nameof(LivePreview));
            Edited?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>True when a secret value is shown as plain text.</summary>
    public bool RevealSecret
    {
        get => _revealSecret;
        set => SetProperty(ref _revealSecret, value);
    }

    /// <summary>Workshop ids parsed from <see cref="Value"/>.</summary>
    public ObservableCollection<string> WorkshopIds { get; }

    /// <summary>Text in the "add Workshop id" box.</summary>
    public string WorkshopDraft
    {
        get => _workshopDraft;
        set => SetProperty(ref _workshopDraft, value);
    }

    /// <summary>Why the last add failed, or null.</summary>
    public string? WorkshopError
    {
        get => _workshopError;
        private set => SetProperty(ref _workshopError, value);
    }

    /// <summary>The line that will be written for <c>steam_ugc</c>, or a note that it will not be written.</summary>
    public string LivePreview => Enabled ? "steam_ugc=" + Value : "Not written. The server keeps its factory default.";

    /// <summary>Adds the id or URL currently in <see cref="WorkshopDraft"/>.</summary>
    public RelayCommand AddWorkshopCommand { get; }

    /// <summary>Removes one Workshop id.</summary>
    public RelayCommand<string> RemoveWorkshopCommand { get; }

    /// <summary>Copies the reference-template value into the field and enables the CVAR.</summary>
    public RelayCommand UseTemplateValueCommand { get; }

    /// <summary>
    /// Shows or hides the row for a search query. An empty query shows the row.
    /// </summary>
    /// <param name="query">Text to find in the key, description, or note.</param>
    public void ApplyFilter(string? query)
    {
        var text = query?.Trim() ?? string.Empty;
        var visible = text.Length == 0
            || Key.Contains(text, StringComparison.OrdinalIgnoreCase)
            || (Description?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
            || (InformationalNote?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false);
        IsVisible = visible;
    }

    private void UseTemplateValue()
    {
        if (!HasTemplateValue || Definition?.TemplateValue is null)
        {
            return;
        }

        Value = Definition.TemplateValue;
        Enabled = true;
    }

    private void AddWorkshop()
    {
        if (!SteamUgcList.TryParseUserInput(WorkshopDraft, out var ids, out var error))
        {
            WorkshopError = error;
            return;
        }

        var added = false;
        foreach (var id in ids)
        {
            if (!WorkshopIds.Contains(id))
            {
                WorkshopIds.Add(id);
                added = true;
            }
        }

        WorkshopError = added ? null : "Those Workshop ids are already in the list.";
        if (added)
        {
            WorkshopDraft = string.Empty;
            Enabled = true;
        }
    }

    private void RemoveWorkshop(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        WorkshopIds.Remove(id);
    }

    private void OnWorkshopIdsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suspendList)
        {
            return;
        }

        _fromList = true;
        Value = SteamUgcList.Format(WorkshopIds);
        _fromList = false;
    }

    private void ReloadWorkshopIds()
    {
        _suspendList = true;
        WorkshopIds.Clear();
        foreach (var id in SteamUgcList.Parse(_value))
        {
            WorkshopIds.Add(id);
        }

        _suspendList = false;
    }
}
