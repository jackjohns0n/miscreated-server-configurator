using System.IO.Compression;

using Microsoft.Data.Sqlite;

using MiscreatedServerConfigurator.Core.Setup;

namespace MiscreatedServerConfigurator.Core.Tests;

public sealed class SteamCmdSetupTests
{
    [Fact]
    public async Task Successful_extract_deletes_the_zip()
    {
        var directory = Directory.CreateTempSubdirectory("steamcmd-");
        var sourceZip = Path.Combine(directory.FullName, "source.zip");
        using (var archive = ZipFile.Open(sourceZip, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("steamcmd.exe");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("stub");
        }

        await SteamCmdSetup.EnsureAsync(directory.FullName, new CopyDownloader(sourceZip), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(directory.FullName, "steamcmd.exe")));
        Assert.False(File.Exists(Path.Combine(directory.FullName, "steamcmd.zip")));
    }

    private sealed class CopyDownloader : IFileDownloader
    {
        private readonly string _source;

        public CopyDownloader(string source)
        {
            _source = source;
        }

        public Task DownloadAsync(Uri url, string destinationFile, CancellationToken cancellationToken)
        {
            File.Copy(_source, destinationFile, overwrite: true);
            return Task.CompletedTask;
        }
    }
}

public sealed class ServerSetupStoreTests
{
    [Fact]
    public void Steamcmd_path_round_trips_beside_the_server_root()
    {
        var directory = Directory.CreateTempSubdirectory("servers-json-");
        var path = Path.Combine(directory.FullName, "servers.json");
        var store = new ServerSetupStore
        {
            RootPath = @"D:\MiscreatedServers",
            SteamCmdPath = @"D:\Tools\steamcmd",
        };
        store.Save(path);

        var loaded = ServerSetupStore.Load(path);

        Assert.Equal(@"D:\MiscreatedServers", loaded.RootPath);
        Assert.Equal(@"D:\Tools\steamcmd", loaded.SteamCmdPath);
    }
}

public sealed class SteamCmdArgumentsTests
{
    [Fact]
    public void Dedicated_update_uses_anonymous_login_and_validate()
    {
        var args = SteamCmdArguments.DedicatedServerUpdate(@"D:\MiscreatedServers\One", beta: null, betaPassword: null);

        Assert.Equal(
            @"+force_install_dir D:\MiscreatedServers\One +login anonymous +app_update 302200 validate +quit",
            args);
    }

    [Fact]
    public void Dedicated_update_quotes_a_path_and_appends_a_beta()
    {
        var args = SteamCmdArguments.DedicatedServerUpdate(@"D:\Miscreated Servers\One", "experimental", "secret");

        Assert.Equal(
            @"+force_install_dir ""D:\Miscreated Servers\One"" +login anonymous +app_update 302200 -beta experimental -betapassword secret validate +quit",
            args);
    }

    [Fact]
    public void Workshop_download_does_not_force_an_install_directory()
    {
        var args = SteamCmdArguments.WorkshopDownload("123456");

        Assert.Equal("+login anonymous +workshop_download_item 299740 123456 +quit", args);
        Assert.DoesNotContain("force_install_dir", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Workshop_download_rejects_a_non_numeric_id()
    {
        Assert.Throws<ArgumentException>(() => SteamCmdArguments.WorkshopDownload("12 34"));
    }
}

public sealed class StartScriptTests
{
    [Fact]
    public void Generated_script_round_trips_and_is_a_short_batch_file()
    {
        var settings = new LaunchSettings
        {
            Map = "islands",
            GamePort = 64090,
            MaxPlayers = 40,
            Whitelisted = true,
            MisGameServerId = 100,
            SvBind = "0.0.0.0",
            GrantAllGuides = true,
            Beta = "experimental",
            BetaPassword = "secret",
        };

        var text = StartScript.Generate(@"D:\MiscreatedServers\One", @"D:\Tools\steamcmd\steamcmd.exe", settings);
        var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
        var nonEmpty = lines.Count(line => line.Length > 0);
        Assert.InRange(nonEmpty, 20, 40);

        var parsed = StartScript.Parse(text);
        Assert.True(parsed.IsOurs);
        Assert.Empty(parsed.Skipped);
        Assert.Equal(@"D:\MiscreatedServers\One", parsed.ServerDirectory);
        Assert.Equal(@"D:\Tools\steamcmd\steamcmd.exe", parsed.SteamCmdPath);
        Assert.Contains("set \"STEAMCMD=D:\\Tools\\steamcmd\\steamcmd.exe\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain(@"D:\MiscreatedServers\steamcmd", text, StringComparison.Ordinal);
        Assert.Equal("islands", parsed.Settings.Map);
        Assert.Equal(64090, parsed.Settings.GamePort);
        Assert.Equal(40, parsed.Settings.MaxPlayers);
        Assert.True(parsed.Settings.Whitelisted);
        Assert.Equal(100, parsed.Settings.MisGameServerId);
        Assert.Equal("0.0.0.0", parsed.Settings.SvBind);
        Assert.Equal("experimental", parsed.Settings.Beta);
        Assert.Equal("secret", parsed.Settings.BetaPassword);
        Assert.Contains("stop.txt", text, StringComparison.Ordinal);
        Assert.Contains("rmdir /s /q \"%SERVER%\\Mods\"", text, StringComparison.Ordinal);
        Assert.Contains("choice /c QC", text, StringComparison.Ordinal);
        Assert.Contains("MiscreatedServer.exe", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Foreign_script_keeps_recognized_flags_and_reports_the_rest()
    {
        const string Foreign = """
            @echo off
            echo Custom launcher
            set EXTRA_THING=hello
            C:\Games\Bin64_dedicated\MiscreatedServer.exe -sv_bind 10.0.0.5 -mis_whitelist -sv_port 64100 -mis_gameserverid 222 +sv_maxplayers 32 +map islands +http_startserver
            echo leftover note
            """;

        var parsed = StartScript.Parse(Foreign);

        Assert.False(parsed.IsOurs);
        Assert.Equal("10.0.0.5", parsed.Settings.SvBind);
        Assert.True(parsed.Settings.Whitelisted);
        Assert.Equal(64100, parsed.Settings.GamePort);
        Assert.Equal(222, parsed.Settings.MisGameServerId);
        Assert.Equal(32, parsed.Settings.MaxPlayers);
        Assert.Equal("islands", parsed.Settings.Map);
        Assert.Contains(parsed.Skipped, line => line.Contains("EXTRA_THING", StringComparison.Ordinal));
        Assert.DoesNotContain(parsed.Skipped, line => line.Contains("MiscreatedServer.exe", StringComparison.Ordinal));
    }
}

public sealed class FirewallPlanTests
{
    [Fact]
    public void Range_is_the_base_port_through_the_next_four()
    {
        Assert.True(FirewallPlan.TryGetRange(64090, out var first, out var last, out var error));
        Assert.Null(error);
        Assert.Equal(64090, first);
        Assert.Equal(64094, last);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65532)]
    [InlineData(65535)]
    public void Range_rejects_ports_that_cannot_fit_five_ports(int port)
    {
        Assert.False(FirewallPlan.TryGetRange(port, out _, out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Add_script_deletes_then_adds_named_tcp_and_udp_rules()
    {
        var script = FirewallPlan.BuildAddScript("One", 64090);

        Assert.Contains("delete rule name=\"Miscreated Server (One) TCP\"", script, StringComparison.Ordinal);
        Assert.Contains("delete rule name=\"Miscreated Server (One) UDP\"", script, StringComparison.Ordinal);
        Assert.Contains("protocol=TCP localport=64090-64094", script, StringComparison.Ordinal);
        Assert.Contains("protocol=UDP localport=64090-64094", script, StringComparison.Ordinal);
    }
}

public sealed class GrantGuidesTests
{
    [Fact]
    public void Apply_updates_existing_rows_and_the_trigger_covers_later_updates()
    {
        var directory = Directory.CreateTempSubdirectory("guides-");
        var database = Path.Combine(directory.FullName, "miscreated.db");
        CreateDatabase(database);

        var applied = GrantGuides.Apply(database);

        Assert.True(applied.Changed);
        Assert.True(GrantGuides.TriggerExists(database));
        Assert.Equal("-1", ReadGuide(database, "Guide00"));
        Assert.Equal("-1", ReadGuide(database, "Guide01"));

        SetGuides(database, "0", "0");
        TouchCharacter(database);
        Assert.Equal("-1", ReadGuide(database, "Guide00"));
        Assert.Equal("-1", ReadGuide(database, "Guide01"));

        var removed = GrantGuides.Remove(database);
        Assert.True(removed.Changed);
        Assert.False(GrantGuides.TriggerExists(database));
        SetGuides(database, "0", "0");
        TouchCharacter(database);
        Assert.Equal("0", ReadGuide(database, "Guide00"));
        Assert.NotNull(applied.BackupPath);
        Assert.True(File.Exists(applied.BackupPath));
    }

    [Fact]
    public void Apply_explains_a_missing_database()
    {
        var result = GrantGuides.Apply(Path.Combine(Path.GetTempPath(), "missing-miscreated.db"));

        Assert.False(result.Changed);
        Assert.Contains("first time it starts", result.Message, StringComparison.Ordinal);
    }

    private static void CreateDatabase(string path)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Characters (Id INTEGER PRIMARY KEY, Name TEXT);
            CREATE TABLE ServerAccountData (AccountId INTEGER, Guide00 TEXT, Guide01 TEXT);
            INSERT INTO Characters (Id, Name) VALUES (1, 'a');
            INSERT INTO ServerAccountData (AccountId, Guide00, Guide01) VALUES (1, '0', '0');
            """;
        command.ExecuteNonQuery();
    }

    private static void TouchCharacter(string path)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Characters SET Name = 'b' WHERE Id = 1;";
        command.ExecuteNonQuery();
    }

    private static void SetGuides(string path, string guide00, string guide01)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE ServerAccountData SET Guide00 = $a, Guide01 = $b;";
        command.Parameters.AddWithValue("$a", guide00);
        command.Parameters.AddWithValue("$b", guide01);
        command.ExecuteNonQuery();
    }

    private static string? ReadGuide(string path, string column)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT " + column + " FROM ServerAccountData LIMIT 1;";
        return command.ExecuteScalar()?.ToString();
    }

    private static SqliteConnection Open(string path)
    {
        var connection = new SqliteConnection("Data Source=" + path);
        connection.Open();
        return connection;
    }
}

public sealed class ServerLayoutTests
{
    [Fact]
    public void Root_is_not_filled_in_and_program_files_is_rejected_when_that_folder_exists()
    {
        Assert.False(ServerLayout.TryValidateRoot("  ", out var empty));
        Assert.Contains("Documents", empty, StringComparison.Ordinal);

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrEmpty(programFiles))
        {
            Assert.False(ServerLayout.TryValidateRoot(programFiles, out _));
        }
    }

    [Fact]
    public void Legacy_steamcmd_folder_stays_under_the_server_root()
    {
        Assert.Equal(
            Path.Combine(@"D:\MiscreatedServers", "steamcmd"),
            ServerLayout.LegacySteamCmdDirectory(@"D:\MiscreatedServers"));
        Assert.False(ServerLayout.TryValidateSteamCmdDirectory("  ", out var error));
        Assert.Contains("SteamCMD", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Server_name_rejects_a_path()
    {
        Assert.False(ServerLayout.TryValidateServerName(@"..\One", out _));
        Assert.True(ServerLayout.TryValidateServerName("My Server", out var error));
        Assert.Null(error);
    }
}
