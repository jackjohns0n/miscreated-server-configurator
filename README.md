# Miscreated Server Configurator

A Windows desktop tool for Miscreated dedicated-server owners and modders. It edits `hosting.cfg` for Steam app 299740, calculates day and night length, and can download dedicated server app 302200. Workshop publishing through the Steam client is still later work. This repository does not wrap old batch files or community setup scripts.

The current version is **0.1.0-dev**. It edits `hosting.cfg`, calculates day and night length, and can download a dedicated server. It does not start the server.

## Status

| | |
| --- | --- |
| Version | 0.1.0-dev |
| Phase | hosting.cfg editor, day/night calculator, server download and setup |
| Stack | C# / .NET 8, WPF for the UI, xUnit for the core tests |
| Steam | SteamCMD installs dedicated server app 302200. Steamworks.NET is still planned for Workshop publishing. |

Phase 1 can load and save `hosting.cfg`, edit the CVARs from the reference template (factions 0–15 and the faction damage matrix included), edit the `steam_ugc` Workshop id list, and either comment out or omit unused CVARs. The Weather page also calculates `wm_timeScale` and `wm_timeScaleNight` from a desired day and night length.

Not built yet:

- Workshop upload and update through the logged-in Steam client. SteamCMD install is included. Steamworks.NET is not.
- Installer, screenshots, and issue/pull-request templates (Phase 4).

## Features in this version

- Open a `hosting.cfg`, or start from the commented reference template in `samples/hosting.cfg`.
- Edit values by section: Core, Logging, Ping, Steam, Weather, Game Rules & Player, AI, Factions, and the faction damage matrix.
- On Weather, the day/night panel is the only place to edit `wm_timeScale` and `wm_timeScaleNight`. Enter real hours, or edit the scales; each side updates the other. Apply writes them when both are valid. Clearing a box does not change the file. See `docs/day-night-calculator.md`.
- Server → Download and setup asks for an app-wide SteamCMD folder (not under the server root), installs dedicated server app 302200, copies Workshop mods from `steam_ugc`, checks the Visual C++ x64 runtimes, and can add firewall rules. It writes `StartServer.bat` and does not launch the server. See `docs/server-setup.md`.
- `http_password` and `sv_servername` are marked required. Passwords and `sv_steamWebAPIKey` are masked.
- `steam_ugc` is edited as a list of Workshop file ids. Paste a numeric id or a Workshop URL. The file is written as comma-separated ids with no spaces.
- Unused CVARs can be commented out or left out of the file. Either way the server keeps its stock value.
- Theme: Dark, Light, or Follow system (the default). Follow system switches when the Windows app theme changes. The choice is saved under `%LOCALAPPDATA%\MiscreatedServerConfigurator\theme.txt`.
- Comments and line order are kept when a setting has not changed. A live line the editor rewrites is `key=value` with no trailing comment.
- Keys that are in the open file but not in the catalog are kept, so a save does not drop them.

The catalog is built from `samples/hosting.cfg` (the 13 November 2026 reference template). Every assignment in that file is a real server CVAR. Official help at <https://servers.miscreatedgame.com/help> is partly outdated; where it disagrees with the template, the editor shows a note and does not rewrite the template value. Four keys are offered because a cited source names them even though the template has no assignment line: `sv_maxplayers`, `g_idleKickTime`, `wm_startPattern`, and `g_respawnAtBaseTime`. Nothing else was added. Descriptions and defaults that the sources do not give are left blank.

`samples/hosting.cfg` uses the placeholder `http_password=CHANGE_ME`. That is not a credential. The program does not contain passwords, API keys, or account names. A beta password you type is written only into that server's `StartServer.bat`.

## Server download and setup

Server → Download and setup installs a dedicated server and writes a start script. The steps follow the same kind of checklist community setup scripts use (SteamCMD app 302200, Workshop files copied in beside the server, the Visual C++ x64 runtimes, firewall ports, a grant-all-guides database trigger, and a generated start script). Spafbi's setup scripts were the behavior checklist for that list. They are unlicensed and were not copied. The script and the C# that drives SteamCMD are original to this project.

The server root, the SteamCMD folder, and known servers are stored in `%LOCALAPPDATA%\MiscreatedServerConfigurator\servers.json`. SteamCMD is app-wide, so its path sits next to the server root in that file rather than in a second settings file. The theme choice remains `%LOCALAPPDATA%\MiscreatedServerConfigurator\theme.txt`. The app never fills in Documents or Program Files for either folder.

The full walkthrough, including Load server and the ports that get opened, is in [docs/server-setup.md](docs/server-setup.md).

## Build

Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). From the repository root:

```bash
dotnet restore MiscreatedServerConfigurator.sln
dotnet test MiscreatedServerConfigurator.sln
dotnet build MiscreatedServerConfigurator.sln --configuration Release
```

`EnableWindowsTargeting` is set so the WPF project compiles on Linux as well as Windows. That is what CI does. The window itself runs on Windows:

```bash
dotnet run --project src/MiscreatedServerConfigurator.App
```

The core library (`src/MiscreatedServerConfigurator.Core`) has no WPF dependency. Its tests cover parsing, round-tripping the reference template, the comment-or-omit choice, `steam_ugc`, the day/night formula, and the server-setup command, script, firewall, and guide-grant logic.

`dotnet format MiscreatedServerConfigurator.sln --verify-no-changes` checks the C# style in `.editorconfig`.

## Layout

| Path | What it is |
| --- | --- |
| `src/MiscreatedServerConfigurator.Core` | hosting.cfg parser, writer, CVAR catalog, and server-setup logic |
| `src/MiscreatedServerConfigurator.App` | WPF editor |
| `tests/MiscreatedServerConfigurator.Core.Tests` | xUnit tests for the core library |
| `samples/hosting.cfg` | Reference template embedded in the core library |
| `docs/hosting-cfg.md` | User guide for the editor |
| `docs/server-setup.md` | User guide for download, setup, and Load server |

## Documentation

- [hosting.cfg user guide](docs/hosting-cfg.md)
- [Day/night calculator](docs/day-night-calculator.md)
- [Server download and setup](docs/server-setup.md)
- [Changelog](CHANGELOG.md)
- License: [MIT](LICENSE)
