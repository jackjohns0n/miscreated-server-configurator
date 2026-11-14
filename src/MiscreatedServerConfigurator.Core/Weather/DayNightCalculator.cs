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
    /// <summary>Day divisor used by Miscalculator, in minutes.</summary>
    public const double DayDivisorMinutes = 780;

    /// <summary>Night divisor used by Miscalculator, in minutes.</summary>
    public const double NightDivisorMinutes = 660;

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
