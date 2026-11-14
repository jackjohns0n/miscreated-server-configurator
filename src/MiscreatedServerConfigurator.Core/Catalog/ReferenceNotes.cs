namespace MiscreatedServerConfigurator.Core.Catalog;

/// <summary>
/// Notes and extra keys taken from cited sources other than a template assignment line.
/// </summary>
/// <remarks>
/// The reference template wins when it disagrees with official help or with miscreatedcvars.txt.
/// A note records the disagreement. It is never used to overwrite a template value.
/// Keys listed in <see cref="OfferedKeys"/> are named by the approved outline or by miscreatedcvars.txt
/// and are not assignment lines in the template. No other keys are added.
/// </remarks>
internal static class ReferenceNotes
{
    internal sealed record Note(
        bool IsRequired,
        bool IsSecret,
        CvarEditorKind Kind,
        string? Description,
        string? InformationalNote);

    internal sealed class OfferedKey
    {
        public required string Key { get; init; }

        public required string BlockTitle { get; init; }

        public string? InsertAfterKey { get; init; }

        public string? InsertBeforeKey { get; init; }
    }

    private static readonly Dictionary<string, Note> Notes = BuildNotes();

    internal static IReadOnlyList<OfferedKey> OfferedKeys { get; } = new OfferedKey[]
    {
        new()
        {
            Key = "sv_maxplayers",
            BlockTitle = "Core Server Settings",
        },
        new()
        {
            Key = "g_idleKickTime",
            BlockTitle = "Ping Settings",
        },
        new()
        {
            Key = "wm_startPattern",
            BlockTitle = "Weather System",
            InsertAfterKey = "wm_pattern",
        },
        new()
        {
            Key = "g_respawnAtBaseTime",
            BlockTitle = "Game Rules & Player",
            InsertBeforeKey = "pcs_maxCorpses",
        },
    };

    internal static IReadOnlyDictionary<CvarSection, string> SectionNotes { get; } = new Dictionary<CvarSection, string>
    {
        [CvarSection.Factions] =
            "Factions 7 through 15 are real server settings. Official help is outdated and only documents custom factions 3 through 6. "
            + "This catalog follows the reference template: enable keys for 3 through 15; names, equipment, and spawn positions for 0 and 3 through 15; "
            + "and Steam id lists for 3 through 15. Factions 0, 1, and 2 have no enable line in the template.",
        [CvarSection.FactionDamage] =
            "Labels follow the template header: f0 players, f1 AI, f2 bases and vehicles, f3 through f15 admin factions. "
            + "Official help instead describes built-in factions as 0 unaffiliated players, 1 environment, and 2 bases. "
            + "The template has no line for faction 2 damaging faction 2, so that cell is not a catalog CVAR. "
            + "The template header says 1.0 is normal damage, 2.0 is double damage, and 0 is no damage.",
    };

    internal static bool TryGet(string key, out Note note)
    {
        return Notes.TryGetValue(key, out note!);
    }

    private static Dictionary<string, Note> BuildNotes()
    {
        var notes = new Dictionary<string, Note>(StringComparer.Ordinal);

        Add(notes, "http_password", required: true, secret: true, note:
            "RCON password. The template says this and sv_servername are required for a uniquely named server that can be administered over RCON, and that it must not match sv_password. Not listed on the official help page.");

        Add(notes, "sv_servername", required: true, note:
            "Name shown in the server browser. The template says to wrap the name in quotes.");

        Add(notes, "sv_password", secret: true, note:
            "Join password. The template says players also need this value in user.cfg, and not to set it if the server should stay public. It must not match http_password. Not listed on the official help page.");

        Add(notes, "sv_maxplayers", note:
            "Named in miscreatedcvars.txt, with no description and no default there. Not an assignment line in the reference template. Leave it unused to keep the server's factory default.");

        Add(notes, "sv_maxuptime", note:
            "The template comments 25.3588 and says the server will not stay up more than 24 hours. miscreatedcvars.txt says the default is 24 hours on a whitelisted server and 12 otherwise, and that the value cannot exceed 24 hours.");

        Add(notes, "g_pinglimit", note:
            "The template comments 0 and spells the key g_pinglimit. miscreatedcvars.txt spells it g_pingLimit and says the default is 1000. The template spelling is the one this catalog writes.");

        Add(notes, "g_pinglimitGraceTime", note:
            "Not listed on the official help page. The template comments 15.");

        Add(notes, "g_idleKickTime", note:
            "Official help lists this CVAR and says it does not work, pending a bug-fix. It is not an assignment line in the reference template. No default is recorded in the reference files. Do not treat it as reliable.");

        Add(notes, "steam_inventory_enable", note:
            "The template comment says 1 enabled and 0 disabled, and the commented value is 1. miscreatedcvars.txt says the default is 0, that 0 means fully enabled, that 3 means admin disabled, and do not use 1 or 2. Official help has no 0/1 table. The template value is left as written.");

        Add(notes, "steam_ugc", kind: CvarEditorKind.SteamUgc, note:
            "Comma-separated Workshop file ids with no spaces. The id is the number in the Workshop file URL. Community host guides describe that order as load order. Not listed on the official help page. Both official Entrada dedicated-server videos use this CVAR.");

        Add(notes, "sv_steamWebAPIKey", secret: true, note:
            "The template says this is needed for the Steam inventory kiosk. Not listed on the official help page. The value is a secret.");

        Add(notes, "wm_disable", note:
            "Not listed on the official help page. The template and miscreatedcvars.txt both include it.");

        Add(notes, "wm_forceTime", note:
            "The template's commented value is 1. That same comment, and miscreatedcvars.txt, say the default is -1 (not frozen). The commented 1 is left as written.");

        Add(notes, "wm_timeScaleWeather", note:
            "Official help says weather speed is independent of day and night. The template comment describes this as a multiplier on wm_timeScale.");

        Add(notes, "wm_startPattern", description:
            "Start a pattern by name or number; 0 auto-selects.", note:
            "Official help documents this. miscreatedcvars.txt names it and gives no default. It is not an assignment line in the reference template. The template's wm_pattern comment says to execute wm_startPattern in RCON to list patterns.");

        Add(notes, "g_playerFoodDecay", note:
            "miscreatedcvars.txt lists the default as 0.2777. The reference template comments 0.208333. The template value is left as written.");

        Add(notes, "g_playerFoodDecaySprinting", note:
            "miscreatedcvars.txt lists the default as 0.34722. The reference template comments 0.260416. The template value is left as written.");

        Add(notes, "g_playerWaterDecay", note:
            "miscreatedcvars.txt lists the default as 0.4861. The reference template comments 0.32407. The template value is left as written.");

        Add(notes, "g_playerWaterDecaySprinting", note:
            "miscreatedcvars.txt lists the default as 0.607638. The reference template comments 0.40509. The template value is left as written.");

        Add(notes, "g_playerTemperatureEnvRate", note:
            "Official help lists the default as 0.0005. The reference template comments 0.00125. The template value is left as written.");

        Add(notes, "g_respawnAtBaseTime", description:
            "Adjust the Respawn At Base duration , default: 30", note:
            "The description is the miscreatedcvars.txt line, including the space before the comma. Official help also lists this CVAR. It is not an assignment line in the reference template, so there is no template value.");

        Add(notes, "g_gameRules_bases", note:
            "miscreatedcvars.txt says option 2 (bases everywhere) is a whitelisted feature only. Official help warns that building anywhere can delete bases that sit in exclusion zones.");

        Add(notes, "asm_percent", note:
            "miscreatedcvars.txt and official help list the default as 33. The reference template comments 50. The template also says the maximum is 90. The template value is left as written.");

        Add(notes, "ism_maxCount", note:
            "Official help lists the default as 750. The reference template comments 500 and says the range is 0 to 5000. The template value is left as written.");

        return notes;
    }

    private static void Add(
        Dictionary<string, Note> notes,
        string key,
        string? note = null,
        string? description = null,
        bool required = false,
        bool secret = false,
        CvarEditorKind kind = CvarEditorKind.Text)
    {
        notes.Add(key, new Note(required, secret, kind, description, note));
    }
}
