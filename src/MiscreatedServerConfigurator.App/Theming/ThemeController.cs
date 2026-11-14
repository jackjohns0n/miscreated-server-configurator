using System.IO;
using System.Windows;
using System.Windows.Media;

using Microsoft.Win32;

using MiscreatedServerConfigurator.Core.Theming;

namespace MiscreatedServerConfigurator.App.Theming;

/// <summary>
/// Applies the light or dark palette and keeps the choice in the per-user settings file.
/// Follow system re-reads the Windows app theme when Windows broadcasts a settings change.
/// </summary>
internal static class ThemeController
{
    private const string SettingsName = "theme.txt";
    private static bool _watching;

    /// <summary>The saved choice. Follow system until the user picks otherwise.</summary>
    public static ThemePreference Preference { get; private set; } = ThemePreference.FollowSystem;

    /// <summary>Loads the saved choice, paints the brushes, and listens for Windows theme changes.</summary>
    public static void Initialize()
    {
        Preference = ReadPreference();
        Apply();
        if (_watching)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _watching = true;
    }

    /// <summary>Stops listening for Windows theme changes.</summary>
    public static void Shutdown()
    {
        if (!_watching)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _watching = false;
    }

    /// <summary>Saves <paramref name="preference"/> and repaints.</summary>
    /// <param name="preference">Dark, light, or follow system.</param>
    public static void SetPreference(ThemePreference preference)
    {
        Preference = preference;
        WritePreference(preference);
        Apply();
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (Preference != ThemePreference.FollowSystem)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        // The event arrives off the UI thread. Brush changes have to happen on the dispatcher.
        dispatcher.BeginInvoke(Apply);
    }

    private static void Apply()
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var dark = ThemeSelection.UseDarkColors(Preference, SystemIsDark());
        var colors = dark ? Dark : Light;
        foreach (var pair in colors)
        {
            SetColor(resources, pair.Key, pair.Value);
        }
    }

    private static void SetColor(ResourceDictionary resources, string key, Color color)
    {
        if (resources[key] is SolidColorBrush existing)
        {
            if (existing.IsFrozen)
            {
                var thawed = existing.Clone();
                thawed.Color = color;
                resources[key] = thawed;
            }
            else
            {
                existing.Color = color;
            }

            return;
        }

        resources[key] = new SolidColorBrush(color);
    }

    private static bool SystemIsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int light)
            {
                return light == 0;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // No theme key, or this process cannot read it. Stay on the light palette.
        }

        return false;
    }

    private static ThemePreference ReadPreference()
    {
        try
        {
            var path = SettingsPath();
            if (!File.Exists(path))
            {
                return ThemePreference.FollowSystem;
            }

            return ThemeSelection.Parse(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ThemePreference.FollowSystem;
        }
    }

    private static void WritePreference(ThemePreference preference)
    {
        try
        {
            var path = SettingsPath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, ThemeSelection.Format(preference));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The palette still changes for this session. The next launch falls back to follow system.
        }
    }

    private static string SettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiscreatedServerConfigurator",
            SettingsName);
    }

    // Light surfaces stay off white. #FFFFFF is only Brush.OnAccent, the label on the blue Save button.
    // Ink and muted both clear WCAG AA on the page, cards, and fields.
    private static readonly Dictionary<string, Color> Light = new(StringComparer.Ordinal)
    {
        ["Brush.Window"] = Color.FromRgb(0xE9, 0xEC, 0xEF),
        ["Brush.Surface"] = Color.FromRgb(0xF4, 0xF5, 0xF7),
        ["Brush.Field"] = Color.FromRgb(0xF1, 0xF3, 0xF5),
        ["Brush.Ink"] = Color.FromRgb(0x1F, 0x29, 0x33),
        ["Brush.Muted"] = Color.FromRgb(0x44, 0x52, 0x66),
        ["Brush.Line"] = Color.FromRgb(0xD0, 0xD4, 0xD8),
        ["Brush.Accent"] = Color.FromRgb(0x2F, 0x6F, 0xA3),
        ["Brush.AccentSoft"] = Color.FromRgb(0xE2, 0xE8, 0xEF),
        ["Brush.AccentHover"] = Color.FromRgb(0x3D, 0x82, 0xBC),
        ["Brush.NavHover"] = Color.FromRgb(0xE3, 0xE6, 0xEA),
        ["Brush.OnAccent"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["Brush.WarnBg"] = Color.FromRgb(0xF3, 0xED, 0xE3),
        ["Brush.WarnInk"] = Color.FromRgb(0x8A, 0x5A, 0x00),
        ["Brush.WarnLine"] = Color.FromRgb(0xE4, 0xD3, 0xB0),
        ["Brush.InfoBg"] = Color.FromRgb(0xE7, 0xEE, 0xF4),
        ["Brush.InfoInk"] = Color.FromRgb(0x24, 0x3B, 0x53),
        ["Brush.DangerBg"] = Color.FromRgb(0xF6, 0xE8, 0xE8),
        ["Brush.DangerInk"] = Color.FromRgb(0x9B, 0x1C, 0x1C),
    };

    private static readonly Dictionary<string, Color> Dark = new(StringComparer.Ordinal)
    {
        ["Brush.Window"] = Color.FromRgb(0x1B, 0x1D, 0x21),
        ["Brush.Surface"] = Color.FromRgb(0x25, 0x28, 0x2E),
        ["Brush.Field"] = Color.FromRgb(0x1B, 0x1D, 0x21),
        ["Brush.Ink"] = Color.FromRgb(0xE8, 0xEA, 0xED),
        ["Brush.Muted"] = Color.FromRgb(0xC3, 0xCC, 0xD5),
        ["Brush.Line"] = Color.FromRgb(0x3A, 0x3F, 0x48),
        ["Brush.Accent"] = Color.FromRgb(0x2F, 0x6F, 0xA3),
        ["Brush.AccentSoft"] = Color.FromRgb(0x1A, 0x30, 0x44),
        ["Brush.AccentHover"] = Color.FromRgb(0x3D, 0x82, 0xBC),
        ["Brush.NavHover"] = Color.FromRgb(0x2C, 0x31, 0x38),
        ["Brush.OnAccent"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["Brush.WarnBg"] = Color.FromRgb(0x3A, 0x2E, 0x16),
        ["Brush.WarnInk"] = Color.FromRgb(0xF3, 0xD1, 0x9C),
        ["Brush.WarnLine"] = Color.FromRgb(0x6B, 0x54, 0x24),
        ["Brush.InfoBg"] = Color.FromRgb(0x1E, 0x2A, 0x36),
        ["Brush.InfoInk"] = Color.FromRgb(0xD5, 0xE4, 0xF2),
        ["Brush.DangerBg"] = Color.FromRgb(0x3A, 0x22, 0x24),
        ["Brush.DangerInk"] = Color.FromRgb(0xF5, 0xC2, 0xC2),
    };
}
