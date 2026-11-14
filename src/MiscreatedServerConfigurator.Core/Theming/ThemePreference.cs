namespace MiscreatedServerConfigurator.Core.Theming;

/// <summary>
/// The appearance choice stored for the editor.
/// </summary>
public enum ThemePreference
{
    /// <summary>Match the Windows app theme, and switch when Windows does.</summary>
    FollowSystem,

    /// <summary>Always use the light palette.</summary>
    Light,

    /// <summary>Always use the dark palette.</summary>
    Dark,
}
