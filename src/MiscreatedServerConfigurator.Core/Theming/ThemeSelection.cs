namespace MiscreatedServerConfigurator.Core.Theming;

/// <summary>
/// Resolves the saved theme choice. The Windows light/dark bit is read by the app and passed in,
/// so this decision can be tested without a desktop session.
/// </summary>
public static class ThemeSelection
{
    /// <summary>
    /// Reads a stored theme name. Blank or unrecognized text is <see cref="ThemePreference.FollowSystem"/>.
    /// </summary>
    /// <param name="text">The saved value, such as "Dark".</param>
    /// <returns>The preference to use.</returns>
    public static ThemePreference Parse(string? text)
    {
        if (string.Equals(text?.Trim(), nameof(ThemePreference.Light), StringComparison.OrdinalIgnoreCase))
        {
            return ThemePreference.Light;
        }

        if (string.Equals(text?.Trim(), nameof(ThemePreference.Dark), StringComparison.OrdinalIgnoreCase))
        {
            return ThemePreference.Dark;
        }

        return ThemePreference.FollowSystem;
    }

    /// <summary>
    /// Returns the name written to the settings file.
    /// </summary>
    /// <param name="preference">The choice to store.</param>
    /// <returns>"FollowSystem", "Light", or "Dark".</returns>
    public static string Format(ThemePreference preference)
    {
        return preference switch
        {
            ThemePreference.Light => nameof(ThemePreference.Light),
            ThemePreference.Dark => nameof(ThemePreference.Dark),
            _ => nameof(ThemePreference.FollowSystem),
        };
    }

    /// <summary>
    /// True when the editor should use the dark palette.
    /// </summary>
    /// <param name="preference">The saved choice.</param>
    /// <param name="systemIsDark">True when Windows apps are set to dark. Ignored unless the choice is follow-system.</param>
    /// <returns>True for the dark palette.</returns>
    public static bool UseDarkColors(ThemePreference preference, bool systemIsDark)
    {
        return preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            _ => systemIsDark,
        };
    }
}
