using System.Globalization;

namespace MiscreatedServerConfigurator.Core.Catalog;

/// <summary>
/// Names and labels for <c>g_gameRules_factionX_dmg_fY</c> keys.
/// Faction ids are 0 through 15, from the reference template.
/// </summary>
public static class FactionDamageKey
{
    private const string Prefix = "g_gameRules_faction";
    private const string Middle = "_dmg_f";

    /// <summary>
    /// Builds the CVAR name for one cell of the damage matrix.
    /// </summary>
    /// <param name="source">The faction dealing damage, 0 through 15.</param>
    /// <param name="target">The faction receiving damage, 0 through 15.</param>
    /// <returns>The CVAR name.</returns>
    public static string Format(int source, int target)
    {
        EnsureFaction(source, nameof(source));
        EnsureFaction(target, nameof(target));
        return string.Create(CultureInfo.InvariantCulture, $"g_gameRules_faction{source}_dmg_f{target}");
    }

    /// <summary>
    /// Parses a damage-matrix CVAR name.
    /// </summary>
    /// <param name="key">The CVAR name.</param>
    /// <param name="source">The source faction when the method returns <see langword="true"/>.</param>
    /// <param name="target">The target faction when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="key"/> is one cell of the 16 by 16 matrix.</returns>
    public static bool TryParse(string key, out int source, out int target)
    {
        source = 0;
        target = 0;
        if (string.IsNullOrEmpty(key) || !key.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = key[Prefix.Length..];
        var middle = rest.IndexOf(Middle, StringComparison.Ordinal);
        if (middle <= 0)
        {
            return false;
        }

        var sourceText = rest[..middle];
        var targetText = rest[(middle + Middle.Length)..];
        if (!TryParseFaction(sourceText, out source) || !TryParseFaction(targetText, out target))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Short label for a faction id, using the words in the template's matrix header.
    /// </summary>
    /// <param name="faction">A faction id from 0 through 15.</param>
    /// <returns>A label such as "0 Players" or "3 Admin".</returns>
    public static string Label(int faction)
    {
        EnsureFaction(faction, nameof(faction));
        return faction switch
        {
            0 => "0 Players",
            1 => "1 AI",
            2 => "2 Bases",
            _ => string.Create(CultureInfo.InvariantCulture, $"{faction} Admin"),
        };
    }

    private static bool TryParseFaction(string text, out int faction)
    {
        faction = 0;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out faction))
        {
            return false;
        }

        // Reject "01" and any id outside the template's 0–15 range.
        if (text != faction.ToString(CultureInfo.InvariantCulture))
        {
            return false;
        }

        return faction is >= 0 and <= 15;
    }

    private static void EnsureFaction(int faction, string paramName)
    {
        if (faction is < 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(paramName, faction, "Faction ids in the damage matrix are 0 through 15.");
        }
    }
}
