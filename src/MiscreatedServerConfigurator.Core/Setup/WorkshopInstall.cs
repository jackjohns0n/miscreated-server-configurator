using MiscreatedServerConfigurator.Core.Hosting;

namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>Numeric Workshop ids read from the editor's <c>steam_ugc</c> value, plus notes for tokens that were not ids.</summary>
/// <param name="Ids">Numeric ids, in order.</param>
/// <param name="Notes">Why a token was skipped, or that <c>steam_ugc</c> is turned off.</param>
public readonly record struct WorkshopIdRead(IReadOnlyList<string> Ids, IReadOnlyList<string> Notes);

/// <summary>Reads <c>steam_ugc</c> from a hosting.cfg session. Only digit ids are downloaded.</summary>
public static class WorkshopIds
{
    /// <summary>Reads the live <c>steam_ugc</c> value. A turned-off CVAR downloads nothing.</summary>
    /// <param name="session">The editor session.</param>
    /// <returns>The ids and any notes.</returns>
    public static WorkshopIdRead FromSession(HostingConfigSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        CvarValueState state;
        try
        {
            state = session.GetRequired("steam_ugc");
        }
        catch (ArgumentException)
        {
            return new WorkshopIdRead(Array.Empty<string>(), new[] { "steam_ugc is not in this hosting.cfg." });
        }

        if (!state.Enabled)
        {
            return new WorkshopIdRead(Array.Empty<string>(), new[] { "steam_ugc is turned off, so no Workshop mods will be downloaded." });
        }

        var ids = new List<string>();
        var notes = new List<string>();
        foreach (var token in SteamUgcList.Parse(state.Value))
        {
            if (token.Length > 0 && token.All(char.IsAsciiDigit))
            {
                ids.Add(token);
            }
            else
            {
                notes.Add("Skipped \"" + token + "\" because a Workshop download needs a numeric file id.");
            }
        }

        return new WorkshopIdRead(ids, notes);
    }
}

/// <summary>
/// Copies Workshop content from the shared SteamCMD cache into one server folder. The copy is C# file IO, not robocopy.
/// </summary>
public static class WorkshopCopier
{
    /// <summary>
    /// Copies each item from <c>steamcmd\steamapps\workshop\content\299740\&lt;id&gt;</c>
    /// to the same relative path under the server, then copies <c>appworkshop_299740.acf</c> when it exists.
    /// </summary>
    /// <param name="steamCmdDirectory">The shared SteamCMD folder.</param>
    /// <param name="serverDirectory">The server folder.</param>
    /// <param name="ids">Numeric Workshop ids.</param>
    /// <returns>One message per item or manifest that could not be copied. Empty when everything was copied.</returns>
    public static IReadOnlyList<string> Copy(string steamCmdDirectory, string serverDirectory, IReadOnlyList<string> ids)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(steamCmdDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverDirectory);
        ArgumentNullException.ThrowIfNull(ids);
        var errors = new List<string>();
        foreach (var id in ids)
        {
            var source = ContentDirectory(steamCmdDirectory, id);
            if (!Directory.Exists(source))
            {
                errors.Add(
                    "Workshop item " + id + " was not in the SteamCMD cache (" + source
                    + "). Anonymous download of app 299740 is unverified and may have been refused.");
                continue;
            }

            CopyDirectory(source, ContentDirectory(serverDirectory, id));
        }

        var manifestSource = Path.Combine(steamCmdDirectory, "steamapps", "workshop", ServerLayout.WorkshopManifestFileName);
        if (ids.Count == 0)
        {
            return errors;
        }

        if (!File.Exists(manifestSource))
        {
            errors.Add(
                ServerLayout.WorkshopManifestFileName
                + " was not in the SteamCMD cache, so it was not copied. The server may not list the mods until that file exists.");
            return errors;
        }

        var manifestDestination = Path.Combine(serverDirectory, "steamapps", "workshop", ServerLayout.WorkshopManifestFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestDestination)!);
        File.Copy(manifestSource, manifestDestination, overwrite: true);
        return errors;
    }

    private static string ContentDirectory(string root, string id)
    {
        return Path.Combine(root, "steamapps", "workshop", "content", MiscreatedApps.GameAppId.ToString(System.Globalization.CultureInfo.InvariantCulture), id);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
