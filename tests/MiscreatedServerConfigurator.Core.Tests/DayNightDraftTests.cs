using MiscreatedServerConfigurator.Core.Weather;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class DayNightDraftTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_day_hours_clear_both_scales(string? dayHours)
    {
        var draft = DayNightDraft.FromHours(dayHours, "1");

        Assert.Equal(string.Empty, draft.DayHours);
        Assert.Equal(string.Empty, draft.TimeScale);
        Assert.Equal(string.Empty, draft.TimeScaleNight);
        Assert.False(draft.CanApply);
        Assert.Equal("Enter a day length to calculate wm_timeScale.", draft.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_night_hours_clear_the_night_scale_and_say_so(string nightHours)
    {
        var draft = DayNightDraft.FromHours("2", nightHours);

        Assert.Equal("6.5", draft.TimeScale);
        Assert.Equal(string.Empty, draft.TimeScaleNight);
        Assert.False(draft.CanApply);
        Assert.Equal("Enter a night length to calculate wm_timeScaleNight.", draft.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    public void Non_numeric_day_hours_do_not_keep_a_scale(string dayHours)
    {
        var draft = DayNightDraft.FromHours(dayHours, "1");

        Assert.Equal(string.Empty, draft.TimeScale);
        Assert.Equal(string.Empty, draft.TimeScaleNight);
        Assert.False(draft.CanApply);
        Assert.Equal("Day length must be a number of hours.", draft.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-2")]
    public void Non_positive_hours_cannot_be_applied(string dayHours)
    {
        var draft = DayNightDraft.FromHours(dayHours, "1");

        Assert.Equal(string.Empty, draft.TimeScale);
        Assert.False(draft.CanApply);
        Assert.Equal("Day time must be a positive number.", draft.Message);
    }

    [Fact]
    public void Both_hour_boxes_blank_name_each_missing_scale()
    {
        var draft = DayNightDraft.FromHours("  ", null);

        Assert.Equal(string.Empty, draft.TimeScale);
        Assert.Equal(string.Empty, draft.TimeScaleNight);
        Assert.False(draft.CanApply);
        Assert.Equal(
            "Enter a day length to calculate wm_timeScale. Enter a night length to calculate wm_timeScaleNight.",
            draft.Message);
    }

    [Fact]
    public void Valid_hours_fill_both_scales_and_allow_apply()
    {
        var draft = DayNightDraft.FromHours("2", "1");

        Assert.Equal("6.5", draft.TimeScale);
        Assert.Equal("1.69", draft.TimeScaleNight);
        Assert.True(draft.CanApply);
        Assert.StartsWith("Pending: wm_timeScale=6.5, wm_timeScaleNight=1.69.", draft.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_day_scale_clears_both_lengths(string? timeScale)
    {
        var draft = DayNightDraft.FromScales(timeScale, "4");

        Assert.Equal(string.Empty, draft.DayHours);
        Assert.Equal(string.Empty, draft.NightHours);
        Assert.Equal(string.Empty, draft.TimeScale);
        Assert.False(draft.CanApply);
        Assert.Equal("Enter a wm_timeScale value to calculate the day length.", draft.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_night_scale_keeps_the_day_length_only(string timeScaleNight)
    {
        var draft = DayNightDraft.FromScales("2", timeScaleNight);

        Assert.Equal("6.5", draft.DayHours);
        Assert.Equal(string.Empty, draft.NightHours);
        Assert.Equal(string.Empty, draft.TimeScaleNight);
        Assert.False(draft.CanApply);
        Assert.Equal("Enter a wm_timeScaleNight value to calculate the night length.", draft.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("2.2.2")]
    public void Non_numeric_scale_does_not_keep_a_length(string timeScale)
    {
        var draft = DayNightDraft.FromScales(timeScale, "4");

        Assert.Equal(string.Empty, draft.DayHours);
        Assert.Equal(string.Empty, draft.NightHours);
        Assert.False(draft.CanApply);
        Assert.Equal("wm_timeScale must be a number.", draft.Message);
    }

    [Fact]
    public void Zero_scale_cannot_be_applied()
    {
        var draft = DayNightDraft.FromScales("0", "4");

        Assert.Equal(string.Empty, draft.DayHours);
        Assert.Equal(string.Empty, draft.NightHours);
        Assert.False(draft.CanApply);
        Assert.Equal("wm_timeScale must be a non-zero number to show a real-time length.", draft.Message);
    }

    [Fact]
    public void Zero_night_scale_keeps_the_day_length_only()
    {
        var draft = DayNightDraft.FromScales("2", "0");

        Assert.Equal("6.5", draft.DayHours);
        Assert.Equal(string.Empty, draft.NightHours);
        Assert.False(draft.CanApply);
        Assert.Equal("wm_timeScaleNight must be a non-zero number to show a real-time length.", draft.Message);
    }

    [Fact]
    public void Valid_scales_fill_both_lengths_and_allow_apply()
    {
        var draft = DayNightDraft.FromScales("2", "4");

        Assert.Equal("6.5", draft.DayHours);
        Assert.Equal("1.375", draft.NightHours);
        Assert.Equal("2", draft.TimeScale);
        Assert.Equal("4", draft.TimeScaleNight);
        Assert.True(draft.CanApply);
    }

    [Fact]
    public void Saved_line_keeps_the_file_values_when_a_preview_would_be_blank()
    {
        var saved = DayNightDraft.DescribeSaved("2", "4", timeScaleWritten: false, timeScaleNightWritten: true);

        Assert.StartsWith("Saved in this file: wm_timeScale=2, wm_timeScaleNight=4.", saved, StringComparison.Ordinal);
        Assert.Contains("Current settings: 6.5-hour day, 1.375-hour night (7.875-hour cycle).", saved, StringComparison.Ordinal);
        Assert.Contains("wm_timeScale is turned off, so the server will use its default for it.", saved, StringComparison.Ordinal);
        Assert.DoesNotContain("wm_timeScaleNight is turned off", saved, StringComparison.Ordinal);
    }
}
