# Miscreated Server Configurator

A Windows desktop tool for Miscreated dedicated-server owners and modders. It edits `hosting.cfg` for Steam app 299740. Later versions will add a day/night calculator and publish Workshop mods through the Steam client. This repository does not wrap the old batch files or community publishers.

The current version is **0.1.0-dev**, Phase 1: the hosting.cfg editor.

## Status

| | |
| --- | --- |
| Version | 0.1.0-dev |
| Phase | 1 — hosting.cfg editor |
| Stack | C# / .NET 8, WPF for the UI, xUnit for the core tests |
| Steam | Not in this build. Steamworks.NET is planned for Phase 3. |

Phase 1 can load and save `hosting.cfg`, edit the CVARs from the reference template (factions 0–15 and the faction damage matrix included), edit the `steam_ugc` Workshop id list, and either comment out or omit unused CVARs.

Not built yet:

- Day/night calculator for `wm_timeScale` and `wm_timeScaleNight` (Phase 2).
- Workshop upload and update through the logged-in Steam client (Phase 3). No Steam code is in this tree.
- Installer, screenshots, and issue/pull-request templates (Phase 4).

## Features in this version

- Open a `hosting.cfg`, or start from the commented reference template in `samples/hosting.cfg`.
- Edit values by section: Core, Logging, Ping, Steam, Weather, Game Rules & Player, AI, Factions, and the faction damage matrix.
- `http_password` and `sv_servername` are marked required. Passwords and `sv_steamWebAPIKey` are masked.
- `steam_ugc` is edited as a list of Workshop file ids. Paste a numeric id or a Workshop URL. The file is written as comma-separated ids with no spaces.
- Unused CVARs can be commented out or left out of the file. Either way the server keeps its factory default.
- Comments and line order are kept when a setting has not changed. A live line the editor rewrites is `key=value` with no trailing comment.
- Keys that are in the open file but not in the catalog are kept, so a save does not drop them.

The catalog is built from `samples/hosting.cfg` (the 13 November 2026 reference template). Every assignment in that file is a real server CVAR. Official help at <https://servers.miscreatedgame.com/help> is partly outdated; where it disagrees with the template, the editor shows a note and does not rewrite the template value. Four keys are offered because a cited source names them even though the template has no assignment line: `sv_maxplayers`, `g_idleKickTime`, `wm_startPattern`, and `g_respawnAtBaseTime`. Nothing else was added. Descriptions and defaults that the sources do not give are left blank.

`samples/hosting.cfg` uses the placeholder `http_password=CHANGE_ME`. That is not a credential. The program does not contain passwords, API keys, or account names.

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

The core library (`src/MiscreatedServerConfigurator.Core`) has no WPF dependency. Its tests cover parsing, round-tripping the reference template, the comment-or-omit choice, and `steam_ugc`.

`dotnet format MiscreatedServerConfigurator.sln --verify-no-changes` checks the C# style in `.editorconfig`.

## Layout

| Path | What it is |
| --- | --- |
| `src/MiscreatedServerConfigurator.Core` | hosting.cfg parser, writer, and CVAR catalog |
| `src/MiscreatedServerConfigurator.App` | WPF editor |
| `tests/MiscreatedServerConfigurator.Core.Tests` | xUnit tests for the core library |
| `samples/hosting.cfg` | Reference template embedded in the core library |
| `docs/hosting-cfg.md` | User guide for the editor |

## Documentation

- [hosting.cfg user guide](docs/hosting-cfg.md)
- [Changelog](CHANGELOG.md)
- License: [MIT](LICENSE)
