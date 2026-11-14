namespace MiscreatedServerConfigurator.Core.Catalog;

/// <summary>
/// Display names for <see cref="CvarSection"/>.
/// </summary>
public static class CvarSections
{
    /// <summary>
    /// Returns the name shown in the editor for <paramref name="section"/>.
    /// </summary>
    /// <param name="section">The catalog section.</param>
    /// <returns>The display name used by the reference template's grouping.</returns>
    public static string DisplayName(CvarSection section)
    {
        return section switch
        {
            CvarSection.Core => "Core",
            CvarSection.Logging => "Logging",
            CvarSection.Ping => "Ping",
            CvarSection.Steam => "Steam",
            CvarSection.Weather => "Weather",
            CvarSection.GameRulesAndPlayer => "Game Rules & Player",
            CvarSection.Ai => "AI",
            CvarSection.Factions => "Factions",
            CvarSection.FactionDamage => "Faction damage matrix",
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown CVAR section."),
        };
    }
}
