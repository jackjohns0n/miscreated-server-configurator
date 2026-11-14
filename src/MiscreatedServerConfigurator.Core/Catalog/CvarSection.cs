namespace MiscreatedServerConfigurator.Core.Catalog;

/// <summary>
/// A group of hosting.cfg CVARs, matching the sections in the reference template.
/// </summary>
public enum CvarSection
{
    /// <summary>Required settings and the other core server keys.</summary>
    Core,

    /// <summary>Log CVARs.</summary>
    Logging,

    /// <summary>Ping and idle keys.</summary>
    Ping,

    /// <summary>Steam inventory, Workshop ids, and the Web API key.</summary>
    Steam,

    /// <summary>Weather and time-of-day keys.</summary>
    Weather,

    /// <summary>Game rules, player, corpse, and related keys.</summary>
    GameRulesAndPlayer,

    /// <summary>AI spawner and corpse keys.</summary>
    Ai,

    /// <summary>Faction enable, name, equipment, spawn, and Steam id keys.</summary>
    Factions,

    /// <summary>The faction damage matrix.</summary>
    FactionDamage,
}
