using MiscreatedServerConfigurator.Core.Theming;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class ThemeSelectionTests
{
    [Theory]
    [InlineData(null, ThemePreference.FollowSystem)]
    [InlineData("", ThemePreference.FollowSystem)]
    [InlineData("nope", ThemePreference.FollowSystem)]
    [InlineData("FollowSystem", ThemePreference.FollowSystem)]
    [InlineData("light", ThemePreference.Light)]
    [InlineData("DARK", ThemePreference.Dark)]
    public void Parse_accepts_the_three_choices_and_defaults_the_rest(string? text, ThemePreference expected)
    {
        Assert.Equal(expected, ThemeSelection.Parse(text));
    }

    [Theory]
    [InlineData(ThemePreference.Light, true, false)]
    [InlineData(ThemePreference.Light, false, false)]
    [InlineData(ThemePreference.Dark, true, true)]
    [InlineData(ThemePreference.Dark, false, true)]
    [InlineData(ThemePreference.FollowSystem, false, false)]
    [InlineData(ThemePreference.FollowSystem, true, true)]
    public void UseDarkColors_follows_the_choice_or_the_system(ThemePreference preference, bool systemIsDark, bool expected)
    {
        Assert.Equal(expected, ThemeSelection.UseDarkColors(preference, systemIsDark));
    }

    [Fact]
    public void Format_round_trips_each_choice()
    {
        foreach (var preference in new[] { ThemePreference.FollowSystem, ThemePreference.Light, ThemePreference.Dark })
        {
            Assert.Equal(preference, ThemeSelection.Parse(ThemeSelection.Format(preference)));
        }
    }
}
