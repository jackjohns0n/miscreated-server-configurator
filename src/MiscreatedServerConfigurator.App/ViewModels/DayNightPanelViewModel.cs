using System.Globalization;
using System.Windows.Input;

using MiscreatedServerConfigurator.Core.Weather;

namespace MiscreatedServerConfigurator.App.ViewModels;

/// <summary>
/// Day/night panel for the weather section. Desired hours in, <c>wm_timeScale</c> and <c>wm_timeScaleNight</c> out.
/// </summary>
public sealed class DayNightPanelViewModel : ObservableObject
{
    private CvarRowViewModel? _timeScale;
    private CvarRowViewModel? _timeScaleNight;
    private string _dayHours = string.Empty;
    private string _nightHours = string.Empty;
    private string _preview = "Enter a day length and a night length in real hours.";
    private string _currentCycle = string.Empty;
    private string? _validationMessage;

    /// <summary>Creates the panel. Rows are attached after a file is loaded.</summary>
    public DayNightPanelViewModel()
    {
        ApplyCommand = new RelayCommand(Apply, CanApply);
    }

    /// <summary>Writes the calculated scales into the open document.</summary>
    public RelayCommand ApplyCommand { get; }

    /// <summary>Desired real-time day length, in hours.</summary>
    public string DayHours
    {
        get => _dayHours;
        set
        {
            if (SetProperty(ref _dayHours, value))
            {
                UpdatePreview();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>Desired real-time night length, in hours.</summary>
    public string NightHours
    {
        get => _nightHours;
        set
        {
            if (SetProperty(ref _nightHours, value))
            {
                UpdatePreview();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>Live preview of the scales and the requested cycle length.</summary>
    public string Preview
    {
        get => _preview;
        private set => SetProperty(ref _preview, value);
    }

    /// <summary>Real-time lengths implied by the current scale CVARs.</summary>
    public string CurrentCycle
    {
        get => _currentCycle;
        private set => SetProperty(ref _currentCycle, value);
    }

    /// <summary>Shown only in this panel. Null when the inputs are empty or valid.</summary>
    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>Points the panel at the two weather rows in the open document.</summary>
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

        UpdateCurrent();
    }

    private void OnScaleEdited(object? sender, EventArgs e)
    {
        UpdateCurrent();
    }

    private void UpdatePreview()
    {
        if (string.IsNullOrWhiteSpace(_dayHours) && string.IsNullOrWhiteSpace(_nightHours))
        {
            ValidationMessage = null;
            Preview = "Enter a day length and a night length in real hours.";
            return;
        }

        if (!TryParseHours(_dayHours, out var day) || !TryParseHours(_nightHours, out var night))
        {
            ValidationMessage = "Day and night lengths must be numbers of hours.";
            Preview = string.Empty;
            return;
        }

        if (!DayNightCalculator.TryFromRealHours(day, night, out var scales, out var error))
        {
            ValidationMessage = error;
            Preview = string.Empty;
            return;
        }

        ValidationMessage = null;
        var cycle = day + night;
        Preview = "wm_timeScale=" + scales.TimeScaleText
            + "    wm_timeScaleNight=" + scales.TimeScaleNightText
            + Environment.NewLine
            + "Real-time cycle: " + FormatHours(day) + " h day + " + FormatHours(night) + " h night = " + FormatHours(cycle) + " h";
    }

    private void UpdateCurrent()
    {
        if (_timeScale is null || _timeScaleNight is null
            || !TryParseHours(_timeScale.Value, out var scale)
            || !TryParseHours(_timeScaleNight.Value, out var scaleNight)
            || !DayNightCalculator.TryToRealHours(scale, scaleNight, out var day, out var night, out _))
        {
            CurrentCycle = "Current wm_timeScale and wm_timeScaleNight do not imply a real-time length yet.";
            return;
        }

        var implied = "The numbers in the boxes imply " + FormatHours(day) + " h day and " + FormatHours(night) + " h night ("
            + FormatHours(day + night) + " h cycle).";
        CurrentCycle = _timeScale.Enabled && _timeScaleNight.Enabled
            ? implied
            : implied + " One or both are unused, so the server is not using them.";
    }

    private bool CanApply()
    {
        return TryParseHours(_dayHours, out var day)
            && TryParseHours(_nightHours, out var night)
            && DayNightCalculator.TryFromRealHours(day, night, out _, out _);
    }

    private void Apply()
    {
        if (_timeScale is null || _timeScaleNight is null)
        {
            ValidationMessage = "wm_timeScale and wm_timeScaleNight are not in this file's catalog.";
            return;
        }

        if (!TryParseHours(_dayHours, out var day) || !TryParseHours(_nightHours, out var night))
        {
            ValidationMessage = "Day and night lengths must be numbers of hours.";
            return;
        }

        if (!DayNightCalculator.TryFromRealHours(day, night, out var scales, out var error))
        {
            ValidationMessage = error;
            return;
        }

        _timeScale.Value = scales.TimeScaleText;
        _timeScale.Enabled = true;
        _timeScaleNight.Value = scales.TimeScaleNightText;
        _timeScaleNight.Enabled = true;
        ValidationMessage = null;
        UpdateCurrent();
    }

    private static bool TryParseHours(string text, out double hours)
    {
        return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out hours);
    }

    private static string FormatHours(double hours)
    {
        return hours.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
