namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>
/// Steam app ids used to install a Miscreated dedicated server. The app does not start the server.
/// </summary>
public static class MiscreatedApps
{
    /// <summary>Dedicated server app. Installed with SteamCMD <c>app_update</c>.</summary>
    public const int DedicatedServerAppId = 302200;

    /// <summary>The game, and the Workshop app id used for <c>workshop_download_item</c>.</summary>
    public const int GameAppId = 299740;

    /// <summary>
    /// Valve's published SteamCMD zip. One copy is extracted into <c>&lt;root&gt;\steamcmd</c>.
    /// </summary>
    public static readonly Uri SteamCmdZipUrl = new("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip");
}
