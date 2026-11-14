using System.Globalization;

namespace MiscreatedServerConfigurator.Core.Weather;

/// <summary>
/// Pending day/night edit. The file is not part of this value.
/// </summary>
/// <remarks>
/// An empty, blank, or invalid box does not produce a scale to write. Callers leave the document unchanged
/// unless <see cref="CanApply"/> is true. The pending texts can be blank while the saved CVARs still hold
/// their previous numbers; show those saved numbers separately.
/// </remarks>
/// <param name="DayHours">Pending day length, or empty when it cannot be calculated.</param>
/// <param name="NightHours">Pending night length, or empty when it cannot be calculated.</param>
/// <param name="TimeScale">Pending <c>wm_timeScale</c>, or empty when it cannot be calculated.</param>
/// <param name="TimeScaleNight">Pending <c>wm_timeScaleNight</c>, or empty when it cannot be calculated.</param>
/// <param name="Message">What the pending boxes currently mean, including what is still missing.</param>
/// <param name="CanApply">True only when both scales are ready to write.</param>
public readonly record struct DayNightDraft(
    string DayHours,
    string NightHours,
    string TimeScale,
    string TimeScaleNight,
    string Message,
    bool CanApply)
{
    /// <summary>
    /// Recomputes the pending scales from the hour boxes. A missing or invalid hour clears the scale that depends on it.
    /// </summary>
    /// <param name="dayText">Day-length box, in real hours.</param>
    /// <param name="nightText">Night-length box, in real hours.</param>
    /// <returns>The pending state. <see cref="CanApply"/> is false until both hours are positive finite numbers.</returns>
    public static DayNightDraft FromHours(string? dayText, string? nightText)
    {
        var dayKind = Classify(dayText, out var dayHours, positiveOnly: true);
        var nightKind = Classify(nightText, out var nightHours, positiveOnly: true);
        var timeScale = string.Empty;
        var timeScaleNight = string.Empty;
        if (dayKind == InputKind.Ok)
        {
            timeScale = DayNightCalculator.FormatScale(RoundHundredths(DayNightCalculator.DayDivisorMinutes / (60d * dayHours)));
        }

        if (dayKind == InputKind.Ok && nightKind == InputKind.Ok
            && DayNightCalculator.TryFromRealHours(dayHours, nightHours, out var scales, out _))
        {
            var message = "Pending: wm_timeScale=" + scales.TimeScaleText
                + ", wm_timeScaleNight=" + scales.TimeScaleNightText
                + ". " + DayNightCalculator.FormatHours(dayHours) + "-hour day, "
                + DayNightCalculator.FormatHours(nightHours) + "-hour night ("
                + DayNightCalculator.FormatHours(dayHours + nightHours) + "-hour cycle).";
            return new DayNightDraft(
                DayNightCalculator.FormatHours(dayHours),
                DayNightCalculator.FormatHours(nightHours),
                scales.TimeScaleText,
                scales.TimeScaleNightText,
                message,
                CanApply: true);
        }

        return new DayNightDraft(
            dayKind == InputKind.Ok ? DayNightCalculator.FormatHours(dayHours) : string.Empty,
            nightKind == InputKind.Ok ? DayNightCalculator.FormatHours(nightHours) : string.Empty,
            dayKind == InputKind.Ok ? timeScale : string.Empty,
            timeScaleNight,
            Join(HourProblem(isDay: true, dayKind), HourProblem(isDay: false, nightKind)),
            CanApply: false);
    }

    /// <summary>
    /// Recomputes the pending hours from the scale boxes. A missing or invalid scale clears the length that depends on it.
    /// </summary>
    /// <param name="timeScaleText"><c>wm_timeScale</c> box.</param>
    /// <param name="timeScaleNightText"><c>wm_timeScaleNight</c> box.</param>
    /// <returns>The pending state. <see cref="CanApply"/> is false until both scales are finite and non-zero.</returns>
    public static DayNightDraft FromScales(string? timeScaleText, string? timeScaleNightText)
    {
        var dayKind = Classify(timeScaleText, out var timeScale, positiveOnly: false);
        var nightKind = Classify(timeScaleNightText, out var timeScaleNight, positiveOnly: false);
        var dayHours = string.Empty;
        var nightHours = string.Empty;
        if (dayKind == InputKind.Ok)
        {
            var hours = (DayNightCalculator.DayDivisorMinutes / 60d) / timeScale;
            if (double.IsFinite(hours))
            {
                dayHours = DayNightCalculator.FormatHours(hours);
            }
            else
            {
                dayKind = InputKind.Invalid;
            }
        }

        if (dayKind == InputKind.Ok && nightKind == InputKind.Ok)
        {
            if (!DayNightCalculator.TryToRealHours(timeScale, timeScaleNight, out var day, out var night, out var error))
            {
                return new DayNightDraft(
                    dayHours,
                    string.Empty,
                    DayNightCalculator.FormatScale(timeScale),
                    DayNightCalculator.FormatScale(timeScaleNight),
                    error ?? "Those scales do not produce a finite day and night length.",
                    CanApply: false);
            }

            return new DayNightDraft(
                DayNightCalculator.FormatHours(day),
                DayNightCalculator.FormatHours(night),
                DayNightCalculator.FormatScale(timeScale),
                DayNightCalculator.FormatScale(timeScaleNight),
                "Pending: wm_timeScale=" + DayNightCalculator.FormatScale(timeScale)
                    + ", wm_timeScaleNight=" + DayNightCalculator.FormatScale(timeScaleNight)
                    + ". " + DayNightCalculator.FormatHours(day) + "-hour day, "
                    + DayNightCalculator.FormatHours(night) + "-hour night ("
                    + DayNightCalculator.FormatHours(day + night) + "-hour cycle).",
                CanApply: true);
        }

        return new DayNightDraft(
            dayKind == InputKind.Ok ? dayHours : string.Empty,
            dayKind == InputKind.Ok ? nightHours : string.Empty,
            dayKind == InputKind.Ok ? DayNightCalculator.FormatScale(timeScale) : string.Empty,
            nightKind == InputKind.Ok ? DayNightCalculator.FormatScale(timeScaleNight) : string.Empty,
            Join(ScaleProblem(isNight: false, dayKind), ScaleProblem(isNight: true, nightKind)),
            CanApply: false);
    }

    /// <summary>
    /// What the open file holds, independent of the pending boxes.
    /// </summary>
    /// <param name="timeScaleText">Saved <c>wm_timeScale</c> text.</param>
    /// <param name="timeScaleNightText">Saved <c>wm_timeScaleNight</c> text.</param>
    /// <param name="timeScaleWritten">True when <c>wm_timeScale</c> is a live line.</param>
    /// <param name="timeScaleNightWritten">True when <c>wm_timeScaleNight</c> is a live line.</param>
    /// <returns>A sentence that quotes the saved values and, when they reverse, the real-time lengths.</returns>
    public static string DescribeSaved(
        string? timeScaleText,
        string? timeScaleNightText,
        bool timeScaleWritten,
        bool timeScaleNightWritten)
    {
        var text = "Saved in this file: wm_timeScale=" + Display(timeScaleText)
            + ", wm_timeScaleNight=" + Display(timeScaleNightText) + ".";
        if (TryParse(timeScaleText, out var timeScale)
            && TryParse(timeScaleNightText, out var timeScaleNight)
            && DayNightCalculator.TryToRealHours(timeScale, timeScaleNight, out var dayHours, out var nightHours, out _))
        {
            return text + " " + DayNightCalculator.DescribeCurrent(dayHours, nightHours, timeScaleWritten, timeScaleNightWritten);
        }

        if (!timeScaleWritten)
        {
            text += " wm_timeScale is turned off, so the server will use its default for it.";
        }

        if (!timeScaleNightWritten)
        {
            text += " wm_timeScaleNight is turned off, so the server will use its default for it.";
        }

        return text;
    }

    private static string Display(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? "(blank)" : text.Trim();
    }

    private static string HourProblem(bool isDay, InputKind kind)
    {
        return kind switch
        {
            InputKind.Empty => isDay
                ? "Enter a day length to calculate wm_timeScale."
                : "Enter a night length to calculate wm_timeScaleNight.",
            InputKind.Invalid => isDay
                ? "Day length must be a number of hours."
                : "Night length must be a number of hours.",
            InputKind.NotPositive => isDay
                ? "Day time must be a positive number."
                : "Night time must be a positive number.",
            _ => string.Empty,
        };
    }

    private static string ScaleProblem(bool isNight, InputKind kind)
    {
        var key = isNight ? DayNightCalculator.TimeScaleNightKey : DayNightCalculator.TimeScaleKey;
        var length = isNight ? "the night length" : "the day length";
        return kind switch
        {
            InputKind.Empty => "Enter a " + key + " value to calculate " + length + ".",
            InputKind.Invalid => key + " must be a number.",
            InputKind.NotPositive => key + " must be a non-zero number to show a real-time length.",
            _ => string.Empty,
        };
    }

    private static string Join(string first, string second)
    {
        if (first.Length == 0)
        {
            return second;
        }

        if (second.Length == 0)
        {
            return first;
        }

        return first + " " + second;
    }

    private static InputKind Classify(string? text, out double value, bool positiveOnly)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return InputKind.Empty;
        }

        if (!TryParse(text, out value))
        {
            return InputKind.Invalid;
        }

        if (positiveOnly)
        {
            return value > 0 ? InputKind.Ok : InputKind.NotPositive;
        }

        return value != 0 ? InputKind.Ok : InputKind.NotPositive;
    }

    private static bool TryParse(string? text, out double value)
    {
        if (string.IsNullOrWhiteSpace(text)
            || !double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            || !double.IsFinite(value))
        {
            value = 0;
            return false;
        }

        return true;
    }

    private static double RoundHundredths(double value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private enum InputKind
    {
        Empty,
        Invalid,
        NotPositive,
        Ok,
    }
}
