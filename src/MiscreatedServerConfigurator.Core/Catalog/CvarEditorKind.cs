namespace MiscreatedServerConfigurator.Core.Catalog;

/// <summary>
/// How the editor should present a CVAR. This is a UI hint, not a server type.
/// </summary>
public enum CvarEditorKind
{
    /// <summary>A single line of text.</summary>
    Text,

    /// <summary>A comma-separated list of Workshop file ids (<c>steam_ugc</c>).</summary>
    SteamUgc,
}
