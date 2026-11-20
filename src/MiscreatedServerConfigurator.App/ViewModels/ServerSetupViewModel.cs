using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

using MiscreatedServerConfigurator.App.Setup;
using MiscreatedServerConfigurator.Core.Hosting;
using MiscreatedServerConfigurator.Core.Setup;

namespace MiscreatedServerConfigurator.App.ViewModels;

/// <summary>
/// Downloads and prepares a dedicated server. It does not start <c>MiscreatedServer.exe</c>.
/// The user starts the generated <c>StartServer.bat</c>.
/// </summary>
public sealed class ServerSetupViewModel : ObservableObject
{
    private readonly MainViewModel _editor;
    private readonly IProcessRunner _processes;
    private readonly IFileDownloader _downloader;
    private readonly IRegistryView _registry;
    private readonly string _storePath;
    private readonly ServerSetupStore _store;
    private string _rootPath = string.Empty;
    private string _serverName = string.Empty;
    private string _map = "islands";
    private string _gamePort = "64090";
    private string _maxPlayers = "50";
    private bool _whitelisted;
    private string _serverId = "100";
    private string _svBind = "0.0.0.0";
    private bool _grantAllGuides;
    private string _beta = string.Empty;
    private string _betaPassword = string.Empty;
    private string _log = string.Empty;
    private string? _message;
    private bool _busy;

    /// <summary>Creates the setup model and loads the saved root and servers.</summary>
    /// <param name="editor">The hosting.cfg editor. Its current text is the only cfg that gets written.</param>
    /// <param name="processes">Runs SteamCMD, installers, and elevated firewall scripts.</param>
    /// <param name="downloader">Downloads SteamCMD and the Visual C++ installers.</param>
    /// <param name="registry">Detects the Visual C++ runtimes.</param>
    /// <param name="storePath">The <c>servers.json</c> path. Null uses the LocalAppData file.</param>
    public ServerSetupViewModel(
        MainViewModel editor,
        IProcessRunner processes,
        IFileDownloader downloader,
        IRegistryView registry,
        string? storePath = null)
    {
        _editor = editor;
        _processes = processes;
        _downloader = downloader;
        _registry = registry;
        _storePath = storePath ?? ServerSetupStore.DefaultFilePath();
        _store = ServerSetupStore.Load(_storePath);
        _rootPath = _store.RootPath;
        if (_store.Servers.Count > 0)
        {
            ApplySaved(_store.Servers[0]);
        }

        DownloadSteamCmdCommand = new RelayCommand(() => _ = DownloadSteamCmdAsync(), () => !IsBusy);
        InstallServerCommand = new RelayCommand(() => _ = InstallServerAsync(), () => !IsBusy);
        DownloadModsCommand = new RelayCommand(() => _ = DownloadModsAsync(), () => !IsBusy);
        InstallRuntimesCommand = new RelayCommand(() => _ = InstallRuntimesAsync(), () => !IsBusy);
        FirewallCommand = new RelayCommand(() => _ = AddFirewallAsync(), () => !IsBusy);
        RemoveFirewallCommand = new RelayCommand(() => _ = RemoveFirewallAsync(), () => !IsBusy);
        WriteScriptCommand = new RelayCommand(() => _ = WriteScriptAsync(), () => !IsBusy);
        WriteConfigCommand = new RelayCommand(WriteConfig, () => !IsBusy);
        GuidesCommand = new RelayCommand(ApplyGuidesNow, () => !IsBusy);
        SaveRootCommand = new RelayCommand(SaveRoot, () => !IsBusy);
    }

    /// <summary>Downloads SteamCMD into the shared folder when it is not already there.</summary>
    public RelayCommand DownloadSteamCmdCommand { get; }

    /// <summary>Runs SteamCMD <c>app_update 302200</c> into the selected server folder.</summary>
    public RelayCommand InstallServerCommand { get; }

    /// <summary>Downloads numeric <c>steam_ugc</c> ids from the open editor document and copies them into the server.</summary>
    public RelayCommand DownloadModsCommand { get; }

    /// <summary>Installs missing Visual C++ x64 runtimes after the window has told the user.</summary>
    public RelayCommand InstallRuntimesCommand { get; }

    /// <summary>Adds the inbound firewall rules. The window shows the reason first.</summary>
    public RelayCommand FirewallCommand { get; }

    /// <summary>Removes this server's firewall rules.</summary>
    public RelayCommand RemoveFirewallCommand { get; }

    /// <summary>Writes <c>StartServer.bat</c>. Does not run it.</summary>
    public RelayCommand WriteScriptCommand { get; }

    /// <summary>Writes the editor's current hosting.cfg into the server folder.</summary>
    public RelayCommand WriteConfigCommand { get; }

    /// <summary>Applies or removes the grant-all-guides trigger, matching the checkbox.</summary>
    public RelayCommand GuidesCommand { get; }

    /// <summary>Stores the root path.</summary>
    public RelayCommand SaveRootCommand { get; }

    /// <summary>True when the editor has unsaved hosting.cfg edits.</summary>
    public bool EditorHasUnsavedChanges => _editor.IsDirty;

    /// <summary>Servers remembered in <c>servers.json</c>.</summary>
    public IReadOnlyList<SavedServer> KnownServers => _store.Servers.ToList();

    /// <summary>The folder that holds SteamCMD and every server. Empty until the user chooses one.</summary>
    public string RootPath
    {
        get => _rootPath;
        set => SetProperty(ref _rootPath, value);
    }

    /// <summary>Server folder name under the root.</summary>
    public string ServerName
    {
        get => _serverName;
        set => SetProperty(ref _serverName, value);
    }

    /// <summary>Map passed as <c>+map</c>.</summary>
    public string Map
    {
        get => _map;
        set => SetProperty(ref _map, value);
    }

    /// <summary>Base game port.</summary>
    public string GamePort
    {
        get => _gamePort;
        set => SetProperty(ref _gamePort, value);
    }

    /// <summary>Max players passed as <c>+sv_maxplayers</c>.</summary>
    public string MaxPlayers
    {
        get => _maxPlayers;
        set => SetProperty(ref _maxPlayers, value);
    }

    /// <summary>When true, the script adds <c>-mis_whitelist</c>.</summary>
    public bool Whitelisted
    {
        get => _whitelisted;
        set => SetProperty(ref _whitelisted, value);
    }

    /// <summary>Server id passed as <c>-mis_gameserverid</c>.</summary>
    public string ServerId
    {
        get => _serverId;
        set => SetProperty(ref _serverId, value);
    }

    /// <summary>Bind address passed as <c>-sv_bind</c>.</summary>
    public string SvBind
    {
        get => _svBind;
        set => SetProperty(ref _svBind, value);
    }

    /// <summary>When true, every account is given all guides once the database exists.</summary>
    public bool GrantAllGuides
    {
        get => _grantAllGuides;
        set => SetProperty(ref _grantAllGuides, value);
    }

    /// <summary>Optional Steam beta branch.</summary>
    public string Beta
    {
        get => _beta;
        set => SetProperty(ref _beta, value);
    }

    /// <summary>Optional beta password. It is written into the start script, not into hosting.cfg.</summary>
    public string BetaPassword
    {
        get => _betaPassword;
        set => SetProperty(ref _betaPassword, value);
    }

    /// <summary>SteamCMD and setup output.</summary>
    public string Log
    {
        get => _log;
        private set => SetProperty(ref _log, value);
    }

    /// <summary>A validation or status sentence for this window.</summary>
    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    /// <summary>True while a download or process is running.</summary>
    public bool IsBusy
    {
        get => _busy;
        private set
        {
            if (SetProperty(ref _busy, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>Tooltip for the map field.</summary>
    public string MapTip => LaunchSettingsValidator.MapTip;

    /// <summary>Tooltip for the port field.</summary>
    public string PortTip => LaunchSettingsValidator.PortTip;

    /// <summary>Tooltip for the max-players field.</summary>
    public string MaxPlayersTip => LaunchSettingsValidator.MaxPlayersTip;

    /// <summary>Tooltip for the whitelist field.</summary>
    public string WhitelistTip => LaunchSettingsValidator.WhitelistTip;

    /// <summary>Tooltip for the server-id field.</summary>
    public string ServerIdTip => LaunchSettingsValidator.ServerIdTip;

    /// <summary>Tooltip for the bind field.</summary>
    public string BindTip => LaunchSettingsValidator.BindTip;

    /// <summary>Tooltip for the guides field.</summary>
    public string GuidesTip => LaunchSettingsValidator.GuidesTip;

    /// <summary>Tooltip for the beta field.</summary>
    public string BetaTip => LaunchSettingsValidator.BetaTip;

    /// <summary>Tooltip for the beta-password field.</summary>
    public string BetaPasswordTip => LaunchSettingsValidator.BetaPasswordTip;

    /// <summary>Fills the form from a remembered server.</summary>
    /// <param name="server">The saved server.</param>
    public void ApplySaved(SavedServer server)
    {
        ServerName = server.Name;
        Map = server.Launch.Map;
        GamePort = server.Launch.GamePort.ToString(CultureInfo.InvariantCulture);
        MaxPlayers = server.Launch.MaxPlayers.ToString(CultureInfo.InvariantCulture);
        Whitelisted = server.Launch.Whitelisted;
        ServerId = server.Launch.MisGameServerId.ToString(CultureInfo.InvariantCulture);
        SvBind = server.Launch.SvBind;
        GrantAllGuides = server.Launch.GrantAllGuides;
        Beta = server.Launch.Beta;
        BetaPassword = server.Launch.BetaPassword;
    }

    /// <summary>Loads a server folder: hosting.cfg into the editor, and the start script into this form.</summary>
    /// <param name="serverDirectory">The folder the user picked.</param>
    /// <param name="replaceEditor">Called when a hosting.cfg is present. Return false to leave the editor alone.</param>
    public void LoadServer(string serverDirectory, Func<string, bool> replaceEditor)
    {
        ServerName = Path.GetFileName(serverDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var parent = Path.GetDirectoryName(serverDirectory);
        if (!string.IsNullOrEmpty(parent))
        {
            RootPath = parent;
        }

        var configPath = Path.Combine(serverDirectory, ServerLayout.HostingConfigFileName);
        if (File.Exists(configPath))
        {
            if (!replaceEditor(configPath))
            {
                Message = "Left the editor's hosting.cfg as it was.";
            }
            else
            {
                _editor.LoadFile(HostingConfigFile.Read(configPath), configPath);
                Message = "Loaded hosting.cfg into the editor.";
            }
        }
        else
        {
            Message = "That folder has no hosting.cfg yet.";
        }

        var database = GrantGuides.DatabasePath(serverDirectory);
        if (File.Exists(database))
        {
            GrantAllGuides = GrantGuides.TriggerExists(database);
        }

        var scriptPath = FindScript(serverDirectory);
        if (scriptPath is null)
        {
            AppendLog("No batch file was found in " + serverDirectory + ".");
            return;
        }

        var parsed = StartScript.Parse(File.ReadAllText(scriptPath));
        ApplyParsed(parsed);
        if (parsed.Skipped.Count > 0)
        {
            AppendLog("Skipped lines in " + Path.GetFileName(scriptPath) + ":");
            foreach (var line in parsed.Skipped)
            {
                AppendLog("  " + line);
            }
        }
        else
        {
            AppendLog(parsed.IsOurs
                ? "Read the start script."
                : "Read recognizable values from " + Path.GetFileName(scriptPath) + ".");
        }
    }

    /// <summary>Runtimes that are not installed.</summary>
    /// <returns>The missing runtimes.</returns>
    public IReadOnlyList<VcRuntime> MissingRuntimes()
    {
        return VcRuntimes.Missing(_registry);
    }

    private void SaveRoot()
    {
        if (!ServerLayout.TryValidateRoot(RootPath, out var error))
        {
            Message = error;
            return;
        }

        _store.RootPath = Path.GetFullPath(RootPath.Trim());
        RootPath = _store.RootPath;
        _store.Save(_storePath);
        Message = "Saved the server root.";
        Raise(nameof(KnownServers));
    }

    private async Task DownloadSteamCmdAsync()
    {
        if (!ServerLayout.TryValidateRoot(RootPath, out var error))
        {
            Message = error;
            return;
        }

        IsBusy = true;
        try
        {
            var directory = ServerLayout.SteamCmdDirectory(Path.GetFullPath(RootPath.Trim()));
            AppendLog("SteamCMD folder: " + directory);
            await SteamCmdSetup.EnsureAsync(directory, _downloader, CancellationToken.None).ConfigureAwait(true);
            AppendLog(File.Exists(ServerLayout.SteamCmdExecutable(Path.GetFullPath(RootPath.Trim())))
                ? "steamcmd.exe is ready."
                : "steamcmd.exe was not found after the download.");
            Message = null;
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or InvalidOperationException)
        {
            Message = ex.Message;
            AppendLog(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InstallServerAsync()
    {
        if (!TryPaths(out var root, out var serverDirectory, out var settings))
        {
            return;
        }

        IsBusy = true;
        try
        {
            Directory.CreateDirectory(serverDirectory);
            var steamCmd = ServerLayout.SteamCmdExecutable(root);
            await SteamCmdSetup.EnsureAsync(ServerLayout.SteamCmdDirectory(root), _downloader, CancellationToken.None).ConfigureAwait(true);
            var arguments = SteamCmdArguments.DedicatedServerUpdate(serverDirectory, settings.Beta, settings.BetaPassword);
            AppendLog("steamcmd " + SteamCmdArguments.DedicatedServerUpdate(serverDirectory, settings.Beta, string.IsNullOrEmpty(settings.BetaPassword) ? null : "(password hidden)"));
            var code = await RunSteamCmdAsync(steamCmd, arguments).ConfigureAwait(true);
            if (code != 0)
            {
                AppendLog("SteamCMD exited with code " + code.ToString(CultureInfo.InvariantCulture) + ". Trying once more.");
                code = await RunSteamCmdAsync(steamCmd, arguments).ConfigureAwait(true);
            }

            if (!ServerLayout.DedicatedExecutableExists(serverDirectory))
            {
                Message = "Bin64_dedicated\\MiscreatedServer.exe was not found. SteamCMD exit code "
                    + code.ToString(CultureInfo.InvariantCulture) + ".";
                return;
            }

            Remember(settings);
            Message = "The dedicated server files are in " + serverDirectory + ". The app did not start the server.";
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or InvalidOperationException or SetupCanceledException)
        {
            Message = ex.Message;
            AppendLog(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DownloadModsAsync()
    {
        if (!TryPaths(out var root, out var serverDirectory, out _))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var session = HostingConfigSession.Load(_editor.BuildText());
            var read = WorkshopIds.FromSession(session);
            foreach (var note in read.Notes)
            {
                AppendLog(note);
            }

            if (read.Ids.Count == 0)
            {
                Message = "No numeric Workshop ids to download.";
                return;
            }

            var steamCmd = ServerLayout.SteamCmdExecutable(root);
            await SteamCmdSetup.EnsureAsync(ServerLayout.SteamCmdDirectory(root), _downloader, CancellationToken.None).ConfigureAwait(true);
            foreach (var id in read.Ids)
            {
                var arguments = SteamCmdArguments.WorkshopDownload(id);
                AppendLog("steamcmd " + arguments);
                var code = await RunSteamCmdAsync(steamCmd, arguments).ConfigureAwait(true);
                if (code != 0)
                {
                    AppendLog("Workshop item " + id + " failed. SteamCMD exit code " + code.ToString(CultureInfo.InvariantCulture)
                        + ". Anonymous download of app 299740 is unverified.");
                }
            }

            foreach (var error in WorkshopCopier.Copy(ServerLayout.SteamCmdDirectory(root), serverDirectory, read.Ids))
            {
                AppendLog(error);
            }

            Message = "Workshop copy finished. Check the log for any item that was not in the cache.";
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or InvalidOperationException)
        {
            Message = ex.Message;
            AppendLog(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InstallRuntimesAsync()
    {
        var missing = VcRuntimes.Missing(_registry);
        if (missing.Count == 0)
        {
            Message = "The Visual C++ 2012, 2013, and 2015 or later x64 runtimes are already installed.";
            return;
        }

        var notice = VcRuntimes.InstallNotice(missing);
        if (MessageBox.Show(notice, "Install Visual C++ runtimes", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK)
        {
            Message = "Runtime install was cancelled.";
            return;
        }

        IsBusy = true;
        try
        {
            var cache = Path.Combine(Path.GetTempPath(), "MiscreatedServerConfigurator");
            Directory.CreateDirectory(cache);
            foreach (var runtime in missing)
            {
                var installer = Path.Combine(cache, runtime.Id + ".exe");
                AppendLog("Downloading " + runtime.DisplayName + ".");
                await _downloader.DownloadAsync(new Uri(runtime.InstallerUrl), installer, CancellationToken.None).ConfigureAwait(true);
                AppendLog("Running " + runtime.DisplayName + " " + runtime.SilentArguments);
                var code = await _processes.RunAsync(
                    new ProcessRequest(installer, runtime.SilentArguments, cache, Elevate: true),
                    onLine: null,
                    CancellationToken.None).ConfigureAwait(true);
                AppendLog(runtime.DisplayName + " exited with code " + code.ToString(CultureInfo.InvariantCulture) + ".");
            }

            Message = "Runtime installers finished. A reboot may still be required before the server will start.";
        }
        catch (SetupCanceledException ex)
        {
            Message = ex.Message;
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or InvalidOperationException)
        {
            Message = ex.Message;
            AppendLog(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddFirewallAsync()
    {
        if (!TryPaths(out _, out _, out var settings))
        {
            return;
        }

        var notice = FirewallPlan.ElevationNotice(ServerName.Trim(), settings.GamePort);
        if (MessageBox.Show(notice, "Windows Firewall", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK)
        {
            Message = "Firewall rules were not changed.";
            return;
        }

        await RunFirewallScriptAsync(FirewallPlan.BuildAddScript(ServerName.Trim(), settings.GamePort), "Firewall rules were added.").ConfigureAwait(true);
    }

    private Task RemoveFirewallAsync()
    {
        if (!ServerLayout.TryValidateServerName(ServerName, out var error))
        {
            Message = error;
            return Task.CompletedTask;
        }

        return RunFirewallScriptAsync(FirewallPlan.BuildRemoveScript(ServerName.Trim()), "Firewall rules for this server were removed.");
    }

    private async Task RunFirewallScriptAsync(string script, string success)
    {
        IsBusy = true;
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "MiscreatedServerConfigurator", "firewall.bat");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, script).ConfigureAwait(true);
            var code = await _processes.RunAsync(new ProcessRequest("cmd.exe", "/c \"" + path + "\"", Path.GetDirectoryName(path)!, Elevate: true), null, CancellationToken.None).ConfigureAwait(true);
            Message = code == 0 ? success : "The firewall script exited with code " + code.ToString(CultureInfo.InvariantCulture) + ".";
        }
        catch (SetupCanceledException ex)
        {
            Message = ex.Message;
            AppendLog(ex.Message);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            Message = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task WriteScriptAsync()
    {
        if (!TryPaths(out var root, out var serverDirectory, out var settings))
        {
            return;
        }

        var scriptPath = Path.Combine(serverDirectory, ServerLayout.StartScriptFileName);
        if (File.Exists(scriptPath))
        {
            var existing = StartScript.Parse(await File.ReadAllTextAsync(scriptPath).ConfigureAwait(true));
            var warning = LaunchSettingsValidator.SensitiveChangeWarning(existing.Settings, settings);
            if (warning is not null
                && MessageBox.Show(warning, "Start script", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                Message = "The start script was not changed.";
                return;
            }
        }

        Directory.CreateDirectory(serverDirectory);
        var text = StartScript.Generate(serverDirectory, ServerLayout.SteamCmdExecutable(root), settings);
        await File.WriteAllTextAsync(scriptPath, text).ConfigureAwait(true);
        Remember(settings);
        AppendLog("Wrote " + scriptPath + ". Start the server by running that file. This app will not run it.");
        var database = GrantGuides.DatabasePath(serverDirectory);
        if (settings.GrantAllGuides || File.Exists(database))
        {
            var guides = settings.GrantAllGuides ? GrantGuides.Apply(database) : GrantGuides.Remove(database);
            AppendLog(guides.Message);
        }

        Message = "Start script written. The server was not started.";
    }

    private void WriteConfig()
    {
        if (!TryPaths(out _, out var serverDirectory, out _))
        {
            return;
        }

        Directory.CreateDirectory(serverDirectory);
        var path = Path.Combine(serverDirectory, ServerLayout.HostingConfigFileName);
        HostingConfigFile.Write(path, _editor.BuildText());
        Message = "Wrote the editor's hosting.cfg to " + path + ".";
    }

    private void ApplyGuidesNow()
    {
        if (!TryPaths(out _, out var serverDirectory, out _))
        {
            return;
        }

        var database = GrantGuides.DatabasePath(serverDirectory);
        var result = GrantAllGuides ? GrantGuides.Apply(database) : GrantGuides.Remove(database);
        Message = result.Message;
        AppendLog(result.Message);
    }

    private async Task<int> RunSteamCmdAsync(string steamCmd, string arguments)
    {
        return await _processes.RunAsync(
            new ProcessRequest(steamCmd, arguments, Path.GetDirectoryName(steamCmd)!, Elevate: false),
            AppendLog,
            CancellationToken.None).ConfigureAwait(true);
    }

    private bool TryPaths(out string root, out string serverDirectory, out LaunchSettings settings)
    {
        root = string.Empty;
        serverDirectory = string.Empty;
        settings = LaunchSettings.CreateDefault();
        if (!ServerLayout.TryValidateRoot(RootPath, out var rootError))
        {
            Message = rootError;
            return false;
        }

        if (!ServerLayout.TryValidateServerName(ServerName, out var nameError))
        {
            Message = nameError;
            return false;
        }

        if (!TryReadSettings(out settings, out var settingsError))
        {
            Message = settingsError;
            return false;
        }

        root = Path.GetFullPath(RootPath.Trim());
        serverDirectory = ServerLayout.ServerDirectory(root, ServerName.Trim());
        Message = null;
        return true;
    }

    private bool TryReadSettings(out LaunchSettings settings, out string? error)
    {
        settings = LaunchSettings.CreateDefault();
        if (!int.TryParse(GamePort.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            || !int.TryParse(MaxPlayers.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var players)
            || !int.TryParse(ServerId.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            error = "Game port, max players, and server id must be whole numbers.";
            return false;
        }

        settings.Map = Map.Trim();
        settings.GamePort = port;
        settings.MaxPlayers = players;
        settings.Whitelisted = Whitelisted;
        settings.MisGameServerId = id;
        settings.SvBind = SvBind.Trim();
        settings.GrantAllGuides = GrantAllGuides;
        settings.Beta = Beta.Trim();
        settings.BetaPassword = BetaPassword;
        var issues = LaunchSettingsValidator.Validate(settings);
        if (issues.Count > 0)
        {
            error = issues[0].Field + ": " + issues[0].Message;
            return false;
        }

        error = null;
        return true;
    }

    private void ApplyParsed(ParsedStartScript parsed)
    {
        Map = parsed.Settings.Map;
        GamePort = parsed.Settings.GamePort.ToString(CultureInfo.InvariantCulture);
        MaxPlayers = parsed.Settings.MaxPlayers.ToString(CultureInfo.InvariantCulture);
        Whitelisted = parsed.Settings.Whitelisted;
        ServerId = parsed.Settings.MisGameServerId.ToString(CultureInfo.InvariantCulture);
        SvBind = parsed.Settings.SvBind;
        Beta = parsed.Settings.Beta;
        BetaPassword = parsed.Settings.BetaPassword;
    }

    private void Remember(LaunchSettings settings)
    {
        _store.RootPath = Path.GetFullPath(RootPath.Trim());
        _store.Remember(ServerName.Trim(), settings);
        _store.Save(_storePath);
        Raise(nameof(KnownServers));
    }

    private static string? FindScript(string serverDirectory)
    {
        var ours = Path.Combine(serverDirectory, ServerLayout.StartScriptFileName);
        if (File.Exists(ours))
        {
            return ours;
        }

        return Directory.Exists(serverDirectory)
            ? Directory.GetFiles(serverDirectory, "*.bat").FirstOrDefault()
            : null;
    }

    private void AppendLog(string line)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => AppendLog(line));
            return;
        }

        Log = string.IsNullOrEmpty(Log) ? line : Log + Environment.NewLine + line;
    }
}
