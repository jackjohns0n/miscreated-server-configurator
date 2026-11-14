using MiscreatedServerConfigurator.Core.Catalog;
using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class CvarCatalogTests
{
    private static readonly string[] OfferedKeys =
    {
        "sv_maxplayers",
        "g_idleKickTime",
        "wm_startPattern",
        "g_respawnAtBaseTime",
    };

    [Fact]
    public void Every_template_key_is_in_the_catalog_and_only_the_offered_keys_are_added()
    {
        var document = HostingConfigDocument.Parse(ServerTemplate.LoadText());
        var templateKeys = document.Lines
            .Where(line => line.Key is not null)
            .Select(line => line.Key!)
            .ToHashSet(StringComparer.Ordinal);
        var catalog = CvarCatalog.Default;
        var catalogKeys = catalog.Entries.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);

        Assert.All(templateKeys, key => Assert.True(catalogKeys.Contains(key), key));
        var added = catalogKeys.Except(templateKeys).OrderBy(key => key, StringComparer.Ordinal).ToArray();
        Assert.Equal(OfferedKeys.OrderBy(key => key, StringComparer.Ordinal).ToArray(), added);
        Assert.Equal(templateKeys.Count + OfferedKeys.Length, catalog.Entries.Count);
        // 380 assignment lines in the reference template, plus the four offered keys.
        // The matrix header's "f0=players, f1=AI" prose is not an assignment.
        Assert.Equal(384, catalog.Entries.Count);
        Assert.Null(catalog.Find("f0"));
        var bases = catalog.Find("g_gameRules_bases");
        Assert.NotNull(bases);
        Assert.Equal("1", bases.TemplateValue);
        Assert.Contains("0=no bases", bases.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Blank_descriptions_are_only_the_keys_the_sources_do_not_describe()
    {
        var blank = CvarCatalog.Default.Entries
            .Where(entry => string.IsNullOrWhiteSpace(entry.Description))
            .Select(entry => entry.Key)
            .ToHashSet(StringComparer.Ordinal);
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "sv_maxplayers",
            "g_idleKickTime",
        };
        for (var faction = 4; faction <= 15; faction++)
        {
            expected.Add("g_gameRules_faction" + faction.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_equip");
        }

        foreach (var entry in CvarCatalog.Default.Entries)
        {
            if (entry.DamageSource is not null)
            {
                expected.Add(entry.Key);
            }
        }

        Assert.Equal(expected.OrderBy(key => key, StringComparer.Ordinal), blank.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void Offered_keys_have_no_invented_template_value()
    {
        var maxPlayers = CvarCatalog.Default.Find("sv_maxplayers");
        var idle = CvarCatalog.Default.Find("g_idleKickTime");
        var pattern = CvarCatalog.Default.Find("wm_startPattern");
        var respawn = CvarCatalog.Default.Find("g_respawnAtBaseTime");

        Assert.NotNull(maxPlayers);
        Assert.False(maxPlayers.PresentInTemplate);
        Assert.Null(maxPlayers.TemplateValue);
        Assert.Null(maxPlayers.Description);

        Assert.NotNull(idle);
        Assert.Null(idle.Description);
        Assert.Contains("does not work", idle.InformationalNote, StringComparison.Ordinal);

        Assert.NotNull(pattern);
        Assert.Null(pattern.TemplateValue);
        Assert.Contains("0 auto-selects", pattern.Description, StringComparison.Ordinal);

        Assert.NotNull(respawn);
        Assert.Null(respawn.TemplateValue);
        Assert.Equal("Adjust the Respawn At Base duration , default: 30", respawn.Description);
    }

    [Fact]
    public void Catalog_does_not_contain_client_or_rcon_commands()
    {
        foreach (var key in new[] { "cl_fov", "r_width", "schedule_shutdown_utc", "mis_ban_steamid", "g_gameRules_faction2_dmg_f2" })
        {
            Assert.Null(CvarCatalog.Default.Find(key));
        }
    }

    [Fact]
    public void Damage_matrix_covers_every_pair_except_the_one_missing_from_the_template()
    {
        var cells = CvarCatalog.Default.Entries.Where(entry => entry.DamageSource is not null).ToArray();

        Assert.Equal(255, cells.Length);
        Assert.All(cells, cell => Assert.Equal(CvarSection.FactionDamage, cell.Section));
        Assert.Null(CvarCatalog.Default.Find(FactionDamageKey.Format(2, 2)));
        Assert.NotNull(CvarCatalog.Default.Find(FactionDamageKey.Format(2, 1)));
        Assert.NotNull(CvarCatalog.Default.Find(FactionDamageKey.Format(2, 3)));
        Assert.NotNull(CvarCatalog.Default.Find(FactionDamageKey.Format(0, 0)));
        Assert.NotNull(CvarCatalog.Default.Find(FactionDamageKey.Format(15, 15)));
        Assert.Equal("1", CvarCatalog.Default.Find(FactionDamageKey.Format(0, 0))!.TemplateValue);
        Assert.Null(CvarCatalog.Default.Find(FactionDamageKey.Format(0, 0))!.Description);
    }

    [Fact]
    public void Required_and_secret_flags_match_the_template_rules()
    {
        var required = CvarCatalog.Default.Entries.Where(entry => entry.IsRequired).Select(entry => entry.Key).ToArray();
        var secret = CvarCatalog.Default.Entries.Where(entry => entry.IsSecret).Select(entry => entry.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray();

        Assert.Equal(new[] { "http_password", "sv_servername" }, required);
        Assert.Equal(new[] { "http_password", "sv_password", "sv_steamWebAPIKey" }, secret);
        Assert.Equal(CvarEditorKind.SteamUgc, CvarCatalog.Default.Find("steam_ugc")!.Kind);
    }

    [Fact]
    public void Disagreements_are_notes_and_do_not_replace_template_values()
    {
        var food = CvarCatalog.Default.Find("g_playerFoodDecay")!;
        var forceTime = CvarCatalog.Default.Find("wm_forceTime")!;
        var asm = CvarCatalog.Default.Find("asm_percent")!;
        var inventory = CvarCatalog.Default.Find("steam_inventory_enable")!;

        Assert.Equal("0.208333", food.TemplateValue);
        Assert.Contains("0.2777", food.InformationalNote, StringComparison.Ordinal);
        Assert.Equal("1", forceTime.TemplateValue);
        Assert.Contains("-1", forceTime.InformationalNote, StringComparison.Ordinal);
        Assert.Equal("50", asm.TemplateValue);
        Assert.Contains("33", asm.InformationalNote, StringComparison.Ordinal);
        Assert.Equal("1", inventory.TemplateValue);
        Assert.Contains("do not use 1 or 2", inventory.InformationalNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Http_password_description_comes_from_the_lines_above_it()
    {
        var password = CvarCatalog.Default.Find("http_password")!;
        var equip = CvarCatalog.Default.Blocks.Single(block => block.Title == "Set faction equipment for newly spawned players");

        Assert.Contains("rcon", password.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REPLACE ME", password.Description, StringComparison.Ordinal);
        Assert.Contains("Clothing first", string.Join("\n", equip.BodyNotes), StringComparison.Ordinal);
    }

    [Fact]
    public void Offered_keys_are_placed_next_to_the_related_template_keys()
    {
        var weather = KeysIn("Weather System");
        var rules = KeysIn("Game Rules & Player");
        var core = KeysIn("Core Server Settings");

        Assert.Equal("wm_pattern", weather[Array.IndexOf(weather, "wm_startPattern") - 1]);
        Assert.Equal("pcs_maxCorpses", rules[Array.IndexOf(rules, "g_respawnAtBaseTime") + 1]);
        Assert.Equal("sv_maxplayers", core[^1]);
    }

    [Fact]
    public void Section_notes_record_the_faction_range_and_the_matrix_labels()
    {
        var factions = CvarCatalog.Default.SectionNote(CvarSection.Factions);
        var damage = CvarCatalog.Default.SectionNote(CvarSection.FactionDamage);

        Assert.Contains("7 through 15", factions, StringComparison.Ordinal);
        Assert.Contains("f1 AI", damage, StringComparison.Ordinal);
        Assert.Contains("faction 2 damaging faction 2", damage, StringComparison.Ordinal);
        Assert.Equal("0 Players", FactionDamageKey.Label(0));
        Assert.Equal("1 AI", FactionDamageKey.Label(1));
        Assert.Equal("2 Bases", FactionDamageKey.Label(2));
        Assert.Equal("15 Admin", FactionDamageKey.Label(15));
    }

    private static string[] KeysIn(string blockTitle)
    {
        return CvarCatalog.Default.Blocks.Single(block => block.Title == blockTitle).Entries.Select(entry => entry.Key).ToArray();
    }
}
