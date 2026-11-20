using System.Net;
using System.Net.Sockets;

namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>
/// Values written into the generated start script. hosting.cfg is not generated from these.
/// </summary>
public sealed class LaunchSettings
{
    /// <summary>Map name passed as <c>+map</c>. The editor default is <c>islands</c>, not a value read from the server.</summary>
    public string Map { get; set; } = "islands";

    /// <summary>Base game port passed as <c>-sv_port</c>. Firewall rules use this through this plus 4.</summary>
    public int GamePort { get; set; } = 64090;

    /// <summary>Passed as <c>+sv_maxplayers</c>. The editor default is 50. The server's own stock value is separate.</summary>
    public int MaxPlayers { get; set; } = 50;

    /// <summary>When true, the script adds <c>-mis_whitelist</c>.</summary>
    public bool Whitelisted { get; set; }

    /// <summary>Passed as <c>-mis_gameserverid</c>.</summary>
    public int MisGameServerId { get; set; } = 100;

    /// <summary>Passed as <c>-sv_bind</c>. IPv4 only, so the batch file can pass it unquoted.</summary>
    public string SvBind { get; set; } = "0.0.0.0";

    /// <summary>
    /// When true, every account receives all guides. That applies to all rows in <c>ServerAccountData</c> on purpose.
    /// </summary>
    public bool GrantAllGuides { get; set; }

    /// <summary>Steam beta branch name, or empty for the default branch. There is no beta.ini.</summary>
    public string Beta { get; set; } = string.Empty;

    /// <summary>Password for <see cref="Beta"/>, or empty. It is written into the start script when a beta is set.</summary>
    public string BetaPassword { get; set; } = string.Empty;

    /// <summary>A new settings object with the editor defaults.</summary>
    /// <returns>The defaults.</returns>
    public static LaunchSettings CreateDefault()
    {
        return new LaunchSettings();
    }

    /// <summary>A copy, so a loaded script can be compared with later edits.</summary>
    /// <returns>A separate instance with the same values.</returns>
    public LaunchSettings Clone()
    {
        return new LaunchSettings
        {
            Map = Map,
            GamePort = GamePort,
            MaxPlayers = MaxPlayers,
            Whitelisted = Whitelisted,
            MisGameServerId = MisGameServerId,
            SvBind = SvBind,
            GrantAllGuides = GrantAllGuides,
            Beta = Beta,
            BetaPassword = BetaPassword,
        };
    }
}

/// <summary>One problem with a launch setting. Shown in the setup window, not on the hosting.cfg pages.</summary>
/// <param name="Field">The field name.</param>
/// <param name="Message">What to fix.</param>
public readonly record struct LaunchSettingIssue(string Field, string Message);

/// <summary>Checks launch settings and describes risky changes to map, port, server id, and bind address.</summary>
public static class LaunchSettingsValidator
{
    /// <summary>Tooltip for the map box.</summary>
    public const string MapTip = "Map passed to the server as +map. The editor default is islands. This does not write hosting.cfg.";

    /// <summary>Tooltip for the port box.</summary>
    public const string PortTip = "Base game port (-sv_port). Firewall rules cover this port through four above it, UDP and TCP.";

    /// <summary>Tooltip for the max-players box.</summary>
    public const string MaxPlayersTip = "Passed as +sv_maxplayers. The editor default is 50. This is not copied into hosting.cfg.";

    /// <summary>Tooltip for the whitelist box.</summary>
    public const string WhitelistTip = "When on, the start script adds -mis_whitelist.";

    /// <summary>Tooltip for the server-id box.</summary>
    public const string ServerIdTip = "Passed as -mis_gameserverid. The default is 100. Changing it can point this folder at a different server database.";

    /// <summary>Tooltip for the bind box.</summary>
    public const string BindTip = "IPv4 address passed as -sv_bind. 0.0.0.0 listens on every interface.";

    /// <summary>Tooltip for the guides box.</summary>
    public const string GuidesTip = "When on, every player account is given all guides. That is every row in ServerAccountData, on purpose. The database appears after the first server start.";

    /// <summary>Tooltip for the beta box.</summary>
    public const string BetaTip = "Optional Steam beta branch for app 302200. Leave blank for the default branch. There is no beta.ini.";

    /// <summary>Tooltip for the beta-password box.</summary>
    public const string BetaPasswordTip = "Password for the beta branch, if that branch has one. It is stored in the start script. Leave blank when the branch has no password.";

    /// <summary>Validates a complete settings object.</summary>
    /// <param name="settings">The values to check.</param>
    /// <returns>Problems, in field order. Empty when the settings can be written.</returns>
    public static IReadOnlyList<LaunchSettingIssue> Validate(LaunchSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var issues = new List<LaunchSettingIssue>();
        if (!IsToken(settings.Map))
        {
            issues.Add(new LaunchSettingIssue("Map", "Map must be letters, digits, underscore, or hyphen."));
        }

        if (!FirewallPlan.TryGetRange(settings.GamePort, out _, out _, out var portError))
        {
            issues.Add(new LaunchSettingIssue("Game port", portError ?? "Game port is not valid."));
        }

        if (settings.MaxPlayers is < 1 or > 128)
        {
            issues.Add(new LaunchSettingIssue("Max players", "Max players must be from 1 to 128. That range is the editor's, not a limit published by the server."));
        }

        if (settings.MisGameServerId is < 1 or > 999999)
        {
            issues.Add(new LaunchSettingIssue("Server id", "Server id must be from 1 to 999999."));
        }

        if (!IsIpv4(settings.SvBind))
        {
            issues.Add(new LaunchSettingIssue("Bind IP", "Bind IP must be an IPv4 address, such as 0.0.0.0."));
        }

        if (!string.IsNullOrEmpty(settings.Beta) && !IsToken(settings.Beta))
        {
            issues.Add(new LaunchSettingIssue("Beta", "Beta branch must be letters, digits, underscore, hyphen, or dot."));
        }

        if (!IsSafePassword(settings.BetaPassword))
        {
            issues.Add(new LaunchSettingIssue("Beta password", "Beta password cannot contain quotes, spaces, percent signs, or command characters."));
        }

        if (string.IsNullOrEmpty(settings.Beta) && !string.IsNullOrEmpty(settings.BetaPassword))
        {
            issues.Add(new LaunchSettingIssue("Beta password", "A beta password needs a beta branch name."));
        }

        return issues;
    }

    /// <summary>
    /// Warning shown before saving when the map, port, server id, or bind address differ from the script already on disk.
    /// Max players and whitelist are not included.
    /// </summary>
    /// <param name="before">Settings parsed from the existing script.</param>
    /// <param name="after">Settings about to be written.</param>
    /// <returns>The warning, or null when those four fields are unchanged.</returns>
    public static string? SensitiveChangeWarning(LaunchSettings before, LaunchSettings after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        var parts = new List<string>();
        if (!string.Equals(before.Map, after.Map, StringComparison.Ordinal))
        {
            parts.Add("The map changed. Players can end up in a different world.");
        }

        if (before.GamePort != after.GamePort)
        {
            parts.Add("The game port changed. Port forwarding and the port players use have to match.");
        }

        if (before.MisGameServerId != after.MisGameServerId)
        {
            parts.Add("The server id changed. This can point the folder at a different server database.");
        }

        if (!string.Equals(before.SvBind, after.SvBind, StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("The bind IP changed. The server will listen on a different address.");
        }

        if (parts.Count == 0)
        {
            return null;
        }

        return string.Join(" ", parts) + " Save the start script anyway?";
    }

    private static bool IsToken(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (!char.IsAsciiLetterOrDigit(ch) && ch is not '_' and not '-' and not '.')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsIpv4(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && IPAddress.TryParse(value.Trim(), out var address)
            && address.AddressFamily == AddressFamily.InterNetwork;
    }

    private static bool IsSafePassword(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch) || ch is '"' or '%' or '&' or '|' or '<' or '>' or '^' or '\\')
            {
                return false;
            }
        }

        return true;
    }
}
