using System.Globalization;

namespace MiscreatedServerConfigurator.Core.Weather;

/// <summary>
/// The two hosting.cfg scales produced for a desired real-time day and night.
/// </summary>
/// <param name="TimeScale">Rounded <c>wm_timeScale</c>.</param>
/// <param name="TimeScaleNight">Rounded <c>wm_timeScaleNight</c>.</param>
public readonly record struct DayNightScales(double TimeScale, double TimeScaleNight)
{
    /// <summary>Text written for <c>wm_timeScale</c>, without trailing zeros.</summary>
    public string TimeScaleText => DayNightCalculator.FormatScale(TimeScale);

    /// <summary>Text written for <c>wm_timeScaleNight</c>, without trailing zeros.</summary>
    public string TimeScaleNightText => DayNightCalculator.FormatScale(TimeScaleNight);
}

/// <summary>
/// Ports the Miscalculator formula. Desired real-time day and night lengths, in hours, become
/// <c>wm_timeScale</c> and <c>wm_timeScaleNight</c>.
/// </summary>
/// <remarks>
/// <para>
/// Source: jackjohns0n/Day_Night_Calculator <c>main.js</c>, the <c>item:calculate</c> handler, lines 77–83.
/// The night scale is divided by the full-precision day scale, and only then are both results rounded
/// to two decimal places the way JavaScript <c>Number.prototype.toFixed(2)</c> does (half away from zero).
/// </para>
/// <para>
/// <c>wm_timeScale = 780 / (60 × dayHours)</c>.
/// <c>wm_timeScaleNight = (660 / (60 × nightHours)) / wm_timeScale</c>, using the unrounded day scale.
/// 780 / 60 is 13 and 660 / 60 is 11, so the reverse of a pair of scales is
/// <c>dayHours = 13 / wm_timeScale</c> and <c>nightHours = 11 / (wm_timeScaleNight × wm_timeScale)</c>.
/// Reversing the already-rounded CVAR values gives the real-time lengths those numbers imply,
/// which can differ slightly from the hours that were typed.
/// </para>
/// </remarks>
public static class DayNightCalculator
{
    /// <summary>The day-speed CVAR. Edited in the day/night panel, not as its own row.</summary>
    public const string TimeScaleKey = "wm_timeScale";

    /// <summary>The night-speed CVAR. Edited in the day/night panel, not as its own row.</summary>
    public const string TimeScaleNightKey = "wm_timeScaleNight";

    /// <summary>Day divisor used by Miscalculator, in minutes. Divided by 60 this is 13 in-game hours of daytime.</summary>
    public const double DayDivisorMinutes = 780;

    /// <summary>Night divisor used by Miscalculator, in minutes. Divided by 60 this is 11 in-game hours of nighttime.</summary>
    public const double NightDivisorMinutes = 660;

    /// <summary>True for the two scale CVARs the day/night panel owns.</summary>
    /// <param name="key">A CVAR name.</param>
    /// <returns><see langword="true"/> for <c>wm_timeScale</c> and <c>wm_timeScaleNight</c>.</returns>
    public static bool IsPanelKey(string key)
    {
        return key == TimeScaleKey || key == TimeScaleNightKey;
    }

    /// <summary>
    /// True when a search should keep the Weather page from looking empty, because the day/night panel matches.
    /// </summary>
    /// <param name="query">The search box text.</param>
    /// <returns><see langword="true"/> when the query is found in the panel's keys or labels.</returns>
    public static bool PanelMatchesSearch(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var text = query.Trim();
        return TimeScaleKey.Contains(text, StringComparison.OrdinalIgnoreCase)
            || TimeScaleNightKey.Contains(text, StringComparison.OrdinalIgnoreCase)
            || "day length".Contains(text, StringComparison.OrdinalIgnoreCase)
            || "night length".Contains(text, StringComparison.OrdinalIgnoreCase)
            || "day speed".Contains(text, StringComparison.OrdinalIgnoreCase)
            || "night speed".Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates the two scales. Hours must be finite and greater than zero, matching the original check.
    /// </summary>
    /// <param name="dayHours">Desired real-time day length, in hours.</param>
    /// <param name="nightHours">Desired real-time night length, in hours.</param>
    /// <param name="scales">The rounded scales when the method returns <see langword="true"/>.</param>
    /// <param name="error">Why the input was rejected, or null on success.</param>
    /// <returns><see langword="true"/> when both scales were calculated.</returns>
    public static bool TryFromRealHours(double dayHours, double nightHours, out DayNightScales scales, out string? error)
    {
        scales = default;
        if (!IsPositiveFinite(dayHours))
        {
            error = "Day time must be a positive number.";
            return false;
        }

        if (!IsPositiveFinite(nightHours))
        {
            error = "Night time must be a positive number.";
            return false;
        }

        var dayScale = DayDivisorMinutes / (60d * dayHours);
        var nightScale = (NightDivisorMinutes / (60d * nightHours)) / dayScale;
        scales = new DayNightScales(RoundHundredths(dayScale), RoundHundredths(nightScale));
        error = null;
        return true;
    }

    /// <summary>
    /// Real-time day and night lengths implied by a pair of scale CVAR values.
    /// </summary>
    /// <param name="timeScale">The <c>wm_timeScale</c> value.</param>
    /// <param name="timeScaleNight">The <c>wm_timeScaleNight</c> value.</param>
    /// <param name="dayHours">Implied day length, in hours.</param>
    /// <param name="nightHours">Implied night length, in hours.</param>
    /// <param name="error">Why the scales cannot be reversed, or null on success.</param>
    /// <returns><see langword="true"/> when both lengths are finite.</returns>
    public static bool TryToRealHours(
        double timeScale,
        double timeScaleNight,
        out double dayHours,
        out double nightHours,
        out string? error)
    {
        dayHours = 0;
        nightHours = 0;
        if (!double.IsFinite(timeScale) || timeScale == 0)
        {
            error = "wm_timeScale must be a non-zero number to show a real-time length.";
            return false;
        }

        if (!double.IsFinite(timeScaleNight) || timeScaleNight == 0)
        {
            error = "wm_timeScaleNight must be a non-zero number to show a real-time length.";
            return false;
        }

        dayHours = (DayDivisorMinutes / 60d) / timeScale;
        nightHours = (NightDivisorMinutes / 60d) / (timeScaleNight * timeScale);
        if (!double.IsFinite(dayHours) || !double.IsFinite(nightHours))
        {
            error = "Those scales do not produce a finite day and night length.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Formats a rounded scale the way the original app displayed the number after <c>parseFloat(toFixed(2))</c>.
    /// </summary>
    /// <param name="value">A scale already rounded to hundredths.</param>
    /// <returns>Invariant text with at most two decimal places.</returns>
    public static string FormatScale(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats a real-time length with up to three decimal places and no trailing zeros.</summary>
    /// <param name="hours">A length in hours.</param>
    /// <returns>Invariant text such as <c>6.5</c> or <c>1.375</c>.</returns>
    public static string FormatHours(double hours)
    {
        return hours.ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The reverse readout: the real-time lengths implied by the current scales, plus a sentence for each scale that is turned off.
    /// </summary>
    /// <param name="dayHours">Day length implied by <c>wm_timeScale</c>, in hours.</param>
    /// <param name="nightHours">Night length implied by both scales, in hours.</param>
    /// <param name="timeScaleWritten">True when <c>wm_timeScale</c> is a live line.</param>
    /// <param name="timeScaleNightWritten">True when <c>wm_timeScaleNight</c> is a live line.</param>
    /// <returns>The sentence shown under the hour boxes.</returns>
    public static string DescribeCurrent(double dayHours, double nightHours, bool timeScaleWritten, bool timeScaleNightWritten)
    {
        var text = "Current settings: "
            + FormatHours(dayHours) + "-hour day, "
            + FormatHours(nightHours) + "-hour night ("
            + FormatHours(dayHours + nightHours) + "-hour cycle).";
        if (!timeScaleWritten)
        {
            text += " " + TimeScaleKey + " is turned off, so the server will use its default for it.";
        }

        if (!timeScaleNightWritten)
        {
            text += " " + TimeScaleNightKey + " is turned off, so the server will use its default for it.";
        }

        return text;
    }

    /// <summary>Rounds half away from zero to two decimal places, matching JavaScript <c>toFixed(2)</c> for these magnitudes.</summary>
    private static double RoundHundredths(double value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static bool IsPositiveFinite(double value)
    {
        return double.IsFinite(value) && value > 0;
    }
}
