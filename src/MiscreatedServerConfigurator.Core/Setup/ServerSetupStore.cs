using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiscreatedServerConfigurator.Core.Setup;

/// <summary>One server remembered between launches. The folder is <c>&lt;root&gt;\&lt;name&gt;</c>.</summary>
public sealed class SavedServer
{
    /// <summary>Folder name under the root.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Last launch settings written for this server.</summary>
    public LaunchSettings Launch { get; set; } = LaunchSettings.CreateDefault();
}

/// <summary>
/// The root folder and the servers the app knows about.
/// Stored at <c>%LOCALAPPDATA%\MiscreatedServerConfigurator\servers.json</c>.
/// The theme choice is a separate file, <c>theme.txt</c>, in that same directory.
/// </summary>
public sealed class ServerSetupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>The chosen root. Empty until the user picks one.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>Servers recorded under <see cref="RootPath"/>.</summary>
    public List<SavedServer> Servers { get; set; } = new();

    /// <summary><c>%LOCALAPPDATA%\MiscreatedServerConfigurator</c>.</summary>
    /// <returns>The settings directory.</returns>
    public static string DefaultDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiscreatedServerConfigurator");
    }

    /// <summary>The settings file path.</summary>
    /// <returns><c>servers.json</c> inside <see cref="DefaultDirectory"/>.</returns>
    public static string DefaultFilePath()
    {
        return Path.Combine(DefaultDirectory(), "servers.json");
    }

    /// <summary>Loads the file, or an empty store when it is missing or unreadable.</summary>
    /// <param name="path">The json file.</param>
    /// <returns>The store.</returns>
    public static ServerSetupStore Load(string path)
    {
        if (!File.Exists(path))
        {
            return new ServerSetupStore();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ServerSetupStore>(json, JsonOptions) ?? new ServerSetupStore();
        }
        catch (JsonException)
        {
            return new ServerSetupStore();
        }
    }

    /// <summary>Writes the store, creating the directory when needed.</summary>
    /// <param name="path">The json file.</param>
    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    /// <summary>Inserts or replaces a server by folder name.</summary>
    /// <param name="name">The folder name.</param>
    /// <param name="launch">The launch settings to remember.</param>
    public void Remember(string name, LaunchSettings launch)
    {
        var existing = Servers.Find(server => string.Equals(server.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            Servers.Add(new SavedServer { Name = name, Launch = launch.Clone() });
            return;
        }

        existing.Name = name;
        existing.Launch = launch.Clone();
    }
}
