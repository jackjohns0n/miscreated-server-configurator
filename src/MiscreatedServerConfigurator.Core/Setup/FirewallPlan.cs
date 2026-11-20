using System.Globalization;
using System.Text;

namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>
/// Inbound firewall rules for one server. The base port and the next four ports are opened for both UDP and TCP.
/// </summary>
/// <remarks>
/// Published notes disagree about whether the dedicated server needs TCP as well as UDP.
/// Both are opened so a ruleset that mentions only one of them is still covered. The rules are named
/// <c>Miscreated Server (&lt;name&gt;) TCP</c> and <c>Miscreated Server (&lt;name&gt;) UDP</c> so a later run can find and replace them.
/// Adding them needs an elevated process. The script deletes the same name first, so running it again does not stack duplicates.
/// </remarks>
public static class FirewallPlan
{
    /// <summary>How many ports are opened, starting at the base port. The range is base through base+4.</summary>
    public const int PortCount = 5;

    /// <summary>Calculates the inclusive port range base..base+4.</summary>
    /// <param name="basePort">The game port.</param>
    /// <param name="first">The first port, when the method returns true.</param>
    /// <param name="last">The last port, when the method returns true.</param>
    /// <param name="error">Why the port was rejected, or null.</param>
    /// <returns><see langword="true"/> when the whole range fits in 1..65535.</returns>
    public static bool TryGetRange(int basePort, out int first, out int last, out string? error)
    {
        first = 0;
        last = 0;
        if (basePort < 1)
        {
            error = "Game port must be at least 1.";
            return false;
        }

        if (basePort > 65535 - (PortCount - 1))
        {
            error = "Game port must be " + (65535 - (PortCount - 1)).ToString(CultureInfo.InvariantCulture) + " or less so the next four ports still fit.";
            return false;
        }

        first = basePort;
        last = basePort + (PortCount - 1);
        error = null;
        return true;
    }

    /// <summary>The firewall rule name for one protocol.</summary>
    /// <param name="serverName">The server folder name.</param>
    /// <param name="protocol"><c>TCP</c> or <c>UDP</c>.</param>
    /// <returns>A stable rule name.</returns>
    public static string RuleName(string serverName, string protocol)
    {
        return "Miscreated Server (" + serverName.Trim() + ") " + protocol;
    }

    /// <summary>
    /// A batch script that deletes then adds the TCP and UDP rules. Delete errors are ignored so a missing rule is not a failure.
    /// </summary>
    /// <param name="serverName">The server folder name.</param>
    /// <param name="basePort">The game port.</param>
    /// <returns>The script text, CRLF line endings.</returns>
    public static string BuildAddScript(string serverName, int basePort)
    {
        if (!TryGetRange(basePort, out var first, out var last, out var error))
        {
            throw new ArgumentOutOfRangeException(nameof(basePort), error);
        }

        return Build(serverName, first, last, add: true);
    }

    /// <summary>A batch script that removes the TCP and UDP rules for this server and leaves every other rule alone.</summary>
    /// <param name="serverName">The server folder name.</param>
    /// <returns>The script text, CRLF line endings.</returns>
    public static string BuildRemoveScript(string serverName)
    {
        return Build(serverName, first: 0, last: 0, add: false);
    }

    /// <summary>The sentence shown before Windows asks for administrator approval.</summary>
    /// <param name="serverName">The server folder name.</param>
    /// <param name="basePort">The game port.</param>
    /// <returns>The notice.</returns>
    public static string ElevationNotice(string serverName, int basePort)
    {
        TryGetRange(basePort, out var first, out var last, out _);
        return "Windows Firewall needs inbound rules so players can reach "
            + serverName + " on UDP and TCP ports " + first.ToString(CultureInfo.InvariantCulture)
            + "-" + last.ToString(CultureInfo.InvariantCulture)
            + ". Notes on the server disagree about TCP versus UDP, so both are opened. "
            + "Windows will ask for administrator approval. If you decline, no rules are changed and the rest of the setup stays as it is.";
    }

    private static string Build(string serverName, int first, int last, bool add)
    {
        var tcp = RuleName(serverName, "TCP");
        var udp = RuleName(serverName, "UDP");
        var builder = new StringBuilder();
        builder.Append("@echo off\r\n");
        AppendDelete(builder, tcp);
        AppendDelete(builder, udp);
        if (add)
        {
            var ports = first.ToString(CultureInfo.InvariantCulture) + "-" + last.ToString(CultureInfo.InvariantCulture);
            AppendAdd(builder, tcp, "TCP", ports);
            AppendAdd(builder, udp, "UDP", ports);
        }

        builder.Append("exit /b 0\r\n");
        return builder.ToString();
    }

    private static void AppendDelete(StringBuilder builder, string name)
    {
        builder.Append("netsh advfirewall firewall delete rule name=\"").Append(name).Append("\" >nul 2>&1\r\n");
    }

    private static void AppendAdd(StringBuilder builder, string name, string protocol, string ports)
    {
        builder.Append("netsh advfirewall firewall add rule name=\"").Append(name)
            .Append("\" dir=in action=allow protocol=").Append(protocol)
            .Append(" localport=").Append(ports).Append("\r\n");
        builder.Append("if errorlevel 1 exit /b 1\r\n");
    }
}
