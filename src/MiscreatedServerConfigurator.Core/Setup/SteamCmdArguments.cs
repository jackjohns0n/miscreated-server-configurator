namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>
/// Builds SteamCMD argument strings. The process is started by the app and its output is shown in the setup window.
/// </summary>
/// <remarks>
/// Workshop items are downloaded with no <c>force_install_dir</c>, so they land in the shared SteamCMD cache
/// (<c>steamcmd\steamapps\workshop</c>) and are copied into the server folder afterward.
/// Whether <c>force_install_dir</c> can download Workshop items straight into the server folder was not confirmed, and this app does not depend on that.
/// Anonymous login for Workshop app 299740 is also unverified. A failed download is reported, not hidden.
/// </remarks>
public static class SteamCmdArguments
{
    /// <summary>
    /// <c>+force_install_dir</c> the server folder, anonymous login, <c>app_update 302200</c>, optional beta, <c>validate +quit</c>.
    /// </summary>
    /// <param name="installDirectory">The server folder.</param>
    /// <param name="beta">Beta branch, or null/blank for the default branch.</param>
    /// <param name="betaPassword">Beta password, or null/blank when the branch has none.</param>
    /// <returns>The argument string passed to <c>steamcmd.exe</c>.</returns>
    public static string DedicatedServerUpdate(string installDirectory, string? beta, string? betaPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installDirectory);
        var args = "+force_install_dir " + Quote(installDirectory)
            + " +login anonymous +app_update " + MiscreatedApps.DedicatedServerAppId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(beta))
        {
            args += " -beta " + Quote(beta.Trim());
            if (!string.IsNullOrEmpty(betaPassword))
            {
                args += " -betapassword " + Quote(betaPassword);
            }
        }

        return args + " validate +quit";
    }

    /// <summary>
    /// Anonymous <c>workshop_download_item 299740</c> for one numeric id, then quit.
    /// No install directory is forced, so the item stays in the shared SteamCMD cache.
    /// </summary>
    /// <param name="workshopId">A numeric Workshop file id.</param>
    /// <returns>The argument string.</returns>
    /// <exception cref="ArgumentException">The id is not all digits.</exception>
    public static string WorkshopDownload(string workshopId)
    {
        if (string.IsNullOrEmpty(workshopId) || !workshopId.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Workshop download needs a numeric file id.", nameof(workshopId));
        }

        return "+login anonymous +workshop_download_item "
            + MiscreatedApps.GameAppId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " " + workshopId + " +quit";
    }

    /// <summary>Quotes a SteamCMD token when it contains spaces or quotes.</summary>
    /// <param name="value">A path or a beta name.</param>
    /// <returns>The token, quoted when needed.</returns>
    public static string Quote(string value)
    {
        if (value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
