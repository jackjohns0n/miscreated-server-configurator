using MiscreatedServerConfigurator.Core.Weather;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class DayNightCalculatorTests
{
    [Theory]
    [InlineData(2, 1, 6.5, 1.69)]
    [InlineData(1, 1, 13, 0.85)]
    [InlineData(12, 2, 1.08, 5.08)]
    [InlineData(24, 1, 0.54, 20.31)]
    [InlineData(0.5, 0.5, 26, 0.85)]
    [InlineData(4, 2, 3.25, 1.69)]
    [InlineData(6, 1, 2.17, 5.08)]
    [InlineData(8, 0.5, 1.63, 13.54)]
    [InlineData(100, 1, 0.13, 84.62)]
    [InlineData(1.5, 0.5, 8.67, 2.54)]
    // Night uses the unrounded day scale. Rounding the day scale first yields 10.16, not 10.15.
    [InlineData(1.2, 0.1, 10.83, 10.15)]
    public void Known_pairs_match_miscalculator(double dayHours, double nightHours, double timeScale, double timeScaleNight)
    {
        var ok = DayNightCalculator.TryFromRealHours(dayHours, nightHours, out var scales, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(timeScale, scales.TimeScale);
        Assert.Equal(timeScaleNight, scales.TimeScaleNight);
    }

    [Fact]
    public void One_point_two_hours_uses_the_unrounded_day_scale_for_night()
    {
        Assert.True(DayNightCalculator.TryFromRealHours(1.2, 0.1, out var scales, out _));
        Assert.Equal(10.83, scales.TimeScale);
        Assert.Equal(10.15, scales.TimeScaleNight);
        Assert.NotEqual(10.16, scales.TimeScaleNight);
    }

    [Fact]
    public void Formatted_scales_drop_trailing_zeros()
    {
        Assert.True(DayNightCalculator.TryFromRealHours(2, 1, out var scales, out _));
        Assert.Equal("6.5", scales.TimeScaleText);
        Assert.Equal("1.69", scales.TimeScaleNightText);
    }

    [Theory]
    [InlineData(0, 1, "Day time must be a positive number.")]
    [InlineData(-2, 1, "Day time must be a positive number.")]
    [InlineData(1, 0, "Night time must be a positive number.")]
    [InlineData(1, -0.5, "Night time must be a positive number.")]
    [InlineData(double.NaN, 1, "Day time must be a positive number.")]
    [InlineData(1, double.PositiveInfinity, "Night time must be a positive number.")]
    [InlineData(double.NegativeInfinity, 1, "Day time must be a positive number.")]
    public void Non_positive_and_non_finite_hours_are_rejected(double dayHours, double nightHours, string error)
    {
        var ok = DayNightCalculator.TryFromRealHours(dayHours, nightHours, out _, out var message);

        Assert.False(ok);
        Assert.Equal(error, message);
    }

    [Fact]
    public void Very_large_day_rounds_the_day_scale_to_zero_as_the_original_does()
    {
        var ok = DayNightCalculator.TryFromRealHours(1_000_000, 1, out var scales, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(0, scales.TimeScale);
        Assert.Equal(846153.85, scales.TimeScaleNight);
    }

    [Fact]
    public void Reverse_of_template_defaults_is_the_length_those_scales_imply()
    {
        // Template comments wm_timeScale=2 and wm_timeScaleNight=4.
        // day = 13 / 2, night = 11 / (4 * 2).
        var ok = DayNightCalculator.TryToRealHours(2, 4, out var dayHours, out var nightHours, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(6.5, dayHours);
        Assert.Equal(1.375, nightHours);
        Assert.Equal(
            "Current settings: 6.5-hour day, 1.375-hour night (7.875-hour cycle).",
            DayNightCalculator.DescribeCurrent(dayHours, nightHours, timeScaleWritten: true, timeScaleNightWritten: true));
    }

    [Fact]
    public void Stock_day_speed_with_night_at_the_same_speed_is_a_twelve_hour_cycle()
    {
        // The template comment's "full day in 12 real hours" at wm_timeScale=2.
        // That is the whole cycle only when wm_timeScaleNight=1: 6.5 + 5.5.
        Assert.True(DayNightCalculator.TryToRealHours(2, 1, out var dayHours, out var nightHours, out _));
        Assert.Equal(6.5, dayHours);
        Assert.Equal(5.5, nightHours);
        Assert.Equal(12, dayHours + nightHours);
        Assert.True(DayNightCalculator.TryFromRealHours(6.5, 5.5, out var scales, out _));
        Assert.Equal(2, scales.TimeScale);
        Assert.Equal(1, scales.TimeScaleNight);
    }

    [Fact]
    public void Stock_scales_round_trip_through_the_hour_boxes()
    {
        Assert.True(DayNightCalculator.TryFromRealHours(6.5, 1.375, out var scales, out _));
        Assert.Equal(2, scales.TimeScale);
        Assert.Equal(4, scales.TimeScaleNight);
    }

    [Fact]
    public void Describe_names_each_scale_that_is_turned_off()
    {
        var nightOff = DayNightCalculator.DescribeCurrent(6.5, 1.375, timeScaleWritten: true, timeScaleNightWritten: false);
        Assert.Equal(
            "Current settings: 6.5-hour day, 1.375-hour night (7.875-hour cycle). wm_timeScaleNight is turned off, so the server will use its default for it.",
            nightOff);

        var bothOff = DayNightCalculator.DescribeCurrent(6.5, 1.375, timeScaleWritten: false, timeScaleNightWritten: false);
        Assert.Contains("wm_timeScale is turned off, so the server will use its default for it.", bothOff, StringComparison.Ordinal);
        Assert.Contains("wm_timeScaleNight is turned off, so the server will use its default for it.", bothOff, StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_owns_the_two_scale_keys_and_not_weather_speed()
    {
        Assert.True(DayNightCalculator.IsPanelKey("wm_timeScale"));
        Assert.True(DayNightCalculator.IsPanelKey("wm_timeScaleNight"));
        Assert.False(DayNightCalculator.IsPanelKey("wm_timeScaleWeather"));
        Assert.True(DayNightCalculator.PanelMatchesSearch("wm_timeScaleNight"));
        Assert.True(DayNightCalculator.PanelMatchesSearch("day"));
        Assert.False(DayNightCalculator.PanelMatchesSearch("wm_timeScaleWeather"));
    }

    [Fact]
    public void Reverse_rejects_a_zero_scale()
    {
        var ok = DayNightCalculator.TryToRealHours(0, 1, out _, out _, out var error);

        Assert.False(ok);
        Assert.Equal("wm_timeScale must be a non-zero number to show a real-time length.", error);
    }

    [Fact]
    public void Rounded_forward_values_reverse_close_to_the_requested_hours()
    {
        Assert.True(DayNightCalculator.TryFromRealHours(2, 1, out var scales, out _));
        Assert.True(DayNightCalculator.TryToRealHours(scales.TimeScale, scales.TimeScaleNight, out var day, out var night, out _));

        Assert.Equal(2, day, 2);
        Assert.Equal(1, night, 2);
    }
}
