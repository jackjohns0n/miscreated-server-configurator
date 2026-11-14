using System.Globalization;

using MiscreatedServerConfigurator.Core.Weather;

namespace MiscreatedServerConfigurator.App.ViewModels;

/// <summary>
/// The only editor for <c>wm_timeScale</c> and <c>wm_timeScaleNight</c>.
/// Hours and the two scales stay in sync, and each scale can be turned off.
/// </summary>
public sealed class DayNightPanelViewModel : ObservableObject
{
    private CvarRowViewModel? _timeScale;
    private CvarRowViewModel? _timeScaleNight;
    private string _dayHours = string.Empty;
    private string _nightHours = string.Empty;
    private string _currentSettings = string.Empty;
    private string? _validationMessage;
    private int _suppress;

    /// <summary>The <c>wm_timeScale</c> row, edited inside this panel.</summary>
    public CvarRowViewModel? TimeScaleRow
    {
        get => _timeScale;
        private set => SetProperty(ref _timeScale, value);
    }

    /// <summary>The <c>wm_timeScaleNight</c> row, edited inside this panel.</summary>
    public CvarRowViewModel? TimeScaleNightRow
    {
        get => _timeScaleNight;
        private set => SetProperty(ref _timeScaleNight, value);
    }

    /// <summary>Desired real-time day length, in hours.</summary>
    public string DayHours
    {
        get => _dayHours;
        set
        {
            if (!SetProperty(ref _dayHours, value) || _suppress > 0)
            {
                return;
            }

            PushHoursToScales();
        }
    }

    /// <summary>Desired real-time night length, in hours.</summary>
    public string NightHours
    {
        get => _nightHours;
        set
        {
            if (!SetProperty(ref _nightHours, value) || _suppress > 0)
            {
                return;
            }

            PushHoursToScales();
        }
    }

    /// <summary>Real-time lengths implied by the current scales, and which scales are turned off.</summary>
    public string CurrentSettings
    {
        get => _currentSettings;
        private set => SetProperty(ref _currentSettings, value);
    }

    /// <summary>Shown only in this panel, when the hour boxes are not a usable pair. Null otherwise.</summary>
    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>Points the panel at the two scale rows and fills the hour boxes from them.</summary>
    /// <param name="timeScale">The <c>wm_timeScale</c> row.</param>
    /// <param name="timeScaleNight">The <c>wm_timeScaleNight</c> row.</param>
    public void Attach(CvarRowViewModel? timeScale, CvarRowViewModel? timeScaleNight)
    {
        if (_timeScale is not null)
        {
            _timeScale.Edited -= OnScaleEdited;
        }

        if (_timeScaleNight is not null)
        {
            _timeScaleNight.Edited -= OnScaleEdited;
        }

        _timeScale = timeScale;
        _timeScaleNight = timeScaleNight;
        if (_timeScale is not null)
        {
            _timeScale.Edited += OnScaleEdited;
        }

        if (_timeScaleNight is not null)
        {
            _timeScaleNight.Edited += OnScaleEdited;
        }

        Raise(nameof(TimeScaleRow));
        Raise(nameof(TimeScaleNightRow));
        _suppress++;
        if (TryReadScales(out var day, out var night, out _))
        {
            DayHours = DayNightCalculator.FormatHours(day);
            NightHours = DayNightCalculator.FormatHours(night);
        }
        else
        {
            DayHours = string.Empty;
            NightHours = string.Empty;
        }

        _suppress--;
        ValidationMessage = null;
        UpdateStatus();
    }

    private void OnScaleEdited(object? sender, EventArgs e)
    {
        if (_suppress > 0)
        {
            return;
        }

        PullHoursFromScales();
        UpdateStatus();
    }

    private void PushHoursToScales()
    {
        if (string.IsNullOrWhiteSpace(_dayHours) && string.IsNullOrWhiteSpace(_nightHours))
        {
            ValidationMessage = null;
            UpdateStatus();
            return;
        }

        if (!TryParse(_dayHours, out var day) || !TryParse(_nightHours, out var night))
        {
            ValidationMessage = "Day and night lengths must be numbers of hours.";
            UpdateStatus();
            return;
        }

        if (!DayNightCalculator.TryFromRealHours(day, night, out var scales, out var error))
        {
            ValidationMessage = error;
            UpdateStatus();
            return;
        }

        if (_timeScale is null || _timeScaleNight is null)
        {
            ValidationMessage = "wm_timeScale and wm_timeScaleNight are not in this file.";
            UpdateStatus();
            return;
        }

        ValidationMessage = null;
        _suppress++;
        _timeScale.Value = scales.TimeScaleText;
        _timeScaleNight.Value = scales.TimeScaleNightText;
        _suppress--;
        UpdateStatus();
    }

    private void PullHoursFromScales()
    {
        if (!TryReadScales(out var day, out var night, out _))
        {
            return;
        }

        _suppress++;
        DayHours = DayNightCalculator.FormatHours(day);
        NightHours = DayNightCalculator.FormatHours(night);
        _suppress--;
        ValidationMessage = null;
    }

    private void UpdateStatus()
    {
        if (_timeScale is null || _timeScaleNight is null)
        {
            CurrentSettings = "wm_timeScale and wm_timeScaleNight are not in this file.";
            return;
        }

        if (!TryReadScales(out var day, out var night, out var error))
        {
            CurrentSettings = error ?? "wm_timeScale and wm_timeScaleNight must be numbers to show a day and night length.";
            return;
        }

        CurrentSettings = DayNightCalculator.DescribeCurrent(day, night, _timeScale.Enabled, _timeScaleNight.Enabled);
    }

    private bool TryReadScales(out double dayHours, out double nightHours, out string? error)
    {
        dayHours = 0;
        nightHours = 0;
        if (_timeScale is null || _timeScaleNight is null)
        {
            error = "wm_timeScale and wm_timeScaleNight are not in this file.";
            return false;
        }

        if (!TryParse(_timeScale.Value, out var scale) || !TryParse(_timeScaleNight.Value, out var scaleNight))
        {
            error = "wm_timeScale and wm_timeScaleNight must be numbers to show a day and night length.";
            return false;
        }

        return DayNightCalculator.TryToRealHours(scale, scaleNight, out dayHours, out nightHours, out error);
    }

    private static bool TryParse(string text, out double hours)
    {
        return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out hours);
    }
}
