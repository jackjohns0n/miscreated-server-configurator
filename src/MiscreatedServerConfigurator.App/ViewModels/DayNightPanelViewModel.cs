using System.Windows.Input;

using MiscreatedServerConfigurator.Core.Weather;

namespace MiscreatedServerConfigurator.App.ViewModels;

/// <summary>
/// The only editor for <c>wm_timeScale</c> and <c>wm_timeScaleNight</c>.
/// Hour and scale boxes are a pending preview. Apply writes them, and only when both are valid.
/// Clearing a box leaves the open file unchanged.
/// </summary>
public sealed class DayNightPanelViewModel : ObservableObject
{
    private CvarRowViewModel? _timeScale;
    private CvarRowViewModel? _timeScaleNight;
    private string _dayHours = string.Empty;
    private string _nightHours = string.Empty;
    private string _timeScaleText = string.Empty;
    private string _timeScaleNightText = string.Empty;
    private string _previewMessage = "Enter a day length to calculate wm_timeScale. Enter a night length to calculate wm_timeScaleNight.";
    private string _savedSettings = string.Empty;
    private bool _canApply;
    private int _suppress;

    /// <summary>Creates the panel. Rows are attached after a file is loaded.</summary>
    public DayNightPanelViewModel()
    {
        ApplyCommand = new RelayCommand(Apply, () => CanApply && _timeScale is not null && _timeScaleNight is not null);
    }

    /// <summary>Writes the pending scales into the open file.</summary>
    public RelayCommand ApplyCommand { get; }

    /// <summary>The <c>wm_timeScale</c> row. The checkbox writes the file; the text box is only a preview.</summary>
    public CvarRowViewModel? TimeScaleRow
    {
        get => _timeScale;
        private set => SetProperty(ref _timeScale, value);
    }

    /// <summary>The <c>wm_timeScaleNight</c> row. The checkbox writes the file; the text box is only a preview.</summary>
    public CvarRowViewModel? TimeScaleNightRow
    {
        get => _timeScaleNight;
        private set => SetProperty(ref _timeScaleNight, value);
    }

    /// <summary>Desired real-time day length, in hours. Empty means the day scale is pending.</summary>
    public string DayHours
    {
        get => _dayHours;
        set
        {
            if (!SetProperty(ref _dayHours, value) || _suppress > 0)
            {
                return;
            }

            Publish(DayNightDraft.FromHours(_dayHours, _nightHours), fromHours: true);
        }
    }

    /// <summary>Desired real-time night length, in hours. Empty means the night scale is pending.</summary>
    public string NightHours
    {
        get => _nightHours;
        set
        {
            if (!SetProperty(ref _nightHours, value) || _suppress > 0)
            {
                return;
            }

            Publish(DayNightDraft.FromHours(_dayHours, _nightHours), fromHours: true);
        }
    }

    /// <summary>Pending <c>wm_timeScale</c>. Empty when the day length cannot be calculated. Not the saved value.</summary>
    public string TimeScaleText
    {
        get => _timeScaleText;
        set
        {
            if (!SetProperty(ref _timeScaleText, value) || _suppress > 0)
            {
                return;
            }

            Publish(DayNightDraft.FromScales(_timeScaleText, _timeScaleNightText), fromHours: false);
        }
    }

    /// <summary>Pending <c>wm_timeScaleNight</c>. Empty when the night length cannot be calculated. Not the saved value.</summary>
    public string TimeScaleNightText
    {
        get => _timeScaleNightText;
        set
        {
            if (!SetProperty(ref _timeScaleNightText, value) || _suppress > 0)
            {
                return;
            }

            Publish(DayNightDraft.FromScales(_timeScaleText, _timeScaleNightText), fromHours: false);
        }
    }

    /// <summary>Pending readout. Names a missing box, or describes the scales that Apply would write.</summary>
    public string PreviewMessage
    {
        get => _previewMessage;
        private set => Assign(ref _previewMessage, value);
    }

    /// <summary>What the open file holds right now, including lengths and any scale that is turned off.</summary>
    public string SavedSettings
    {
        get => _savedSettings;
        private set => Assign(ref _savedSettings, value);
    }

    /// <summary>True when Apply would write both scales. False while either side is empty or invalid.</summary>
    public bool CanApply
    {
        get => _canApply;
        private set => Assign(ref _canApply, value);
    }

    /// <summary>Points the panel at the two scale rows and copies their saved values into the pending boxes.</summary>
    /// <param name="timeScale">The <c>wm_timeScale</c> row.</param>
    /// <param name="timeScaleNight">The <c>wm_timeScaleNight</c> row.</param>
    public void Attach(CvarRowViewModel? timeScale, CvarRowViewModel? timeScaleNight)
    {
        if (_timeScale is not null)
        {
            _timeScale.Edited -= OnSavedEdited;
        }

        if (_timeScaleNight is not null)
        {
            _timeScaleNight.Edited -= OnSavedEdited;
        }

        _timeScale = timeScale;
        _timeScaleNight = timeScaleNight;
        if (_timeScale is not null)
        {
            _timeScale.Edited += OnSavedEdited;
        }

        if (_timeScaleNight is not null)
        {
            _timeScaleNight.Edited += OnSavedEdited;
        }

        Raise(nameof(TimeScaleRow));
        Raise(nameof(TimeScaleNightRow));

        var savedScale = _timeScale?.Value ?? string.Empty;
        var savedNight = _timeScaleNight?.Value ?? string.Empty;
        var draft = DayNightDraft.FromScales(savedScale, savedNight);
        _suppress++;
        DayHours = draft.DayHours;
        NightHours = draft.NightHours;
        TimeScaleText = savedScale;
        TimeScaleNightText = savedNight;
        _suppress--;
        Publish(draft, fromHours: false);
        UpdateSaved();
    }

    private void OnSavedEdited(object? sender, EventArgs e)
    {
        if (_suppress > 0)
        {
            return;
        }

        UpdateSaved();
    }

    private void Publish(DayNightDraft draft, bool fromHours)
    {
        _suppress++;
        if (fromHours)
        {
            Assign(ref _timeScaleText, draft.TimeScale, nameof(TimeScaleText));
            Assign(ref _timeScaleNightText, draft.TimeScaleNight, nameof(TimeScaleNightText));
        }
        else
        {
            Assign(ref _dayHours, draft.DayHours, nameof(DayHours));
            Assign(ref _nightHours, draft.NightHours, nameof(NightHours));
        }

        _suppress--;
        PreviewMessage = draft.Message;
        CanApply = draft.CanApply;
        CommandManager.InvalidateRequerySuggested();
    }

    private void Apply()
    {
        if (_timeScale is null || _timeScaleNight is null)
        {
            return;
        }

        var draft = DayNightDraft.FromScales(_timeScaleText, _timeScaleNightText);
        if (!draft.CanApply)
        {
            Publish(draft, fromHours: false);
            return;
        }

        _suppress++;
        _timeScale.Value = draft.TimeScale;
        _timeScaleNight.Value = draft.TimeScaleNight;
        TimeScaleText = draft.TimeScale;
        TimeScaleNightText = draft.TimeScaleNight;
        DayHours = draft.DayHours;
        NightHours = draft.NightHours;
        _suppress--;
        Publish(draft, fromHours: false);
        UpdateSaved();
    }

    private void UpdateSaved()
    {
        SavedSettings = DayNightDraft.DescribeSaved(
            _timeScale?.Value,
            _timeScaleNight?.Value,
            _timeScale?.Enabled ?? false,
            _timeScaleNight?.Enabled ?? false);
    }

    /// <summary>Sets a field and still notifies when the value is unchanged, so an empty box refreshes its dependents.</summary>
    private void Assign<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            Raise(propertyName);
        }
    }
}
