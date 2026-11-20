# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Server download and setup (Windows). Server → Download and setup installs SteamCMD once under a root you choose, installs dedicated server app 302200, copies Workshop items from the editor's `steam_ugc`, checks the Visual C++ 2012, 2013, and 2015 or later x64 runtimes, and can add inbound firewall rules. It writes `StartServer.bat` and does not start the server. Grant-all-guides is a SQLite trigger on `miscreated.db`. See `docs/server-setup.md`. Spafbi's unlicensed setup scripts were a behavior checklist only and were not copied.
- Day/night calculator on the Weather page. Desired real hours become `wm_timeScale` and `wm_timeScaleNight` with the formula from [Miscalculator](https://github.com/jackjohns0n/Day_Night_Calculator) (`main.js` lines 77–83). Those two CVARs are edited only in the panel, including each one's on/off toggle, and either side updates the other. The panel explains the formula. See `docs/day-night-calculator.md`.

### Changed

- Theme setting: Dark, Light, or Follow system. Follow system is the default and updates when the Windows app theme changes. The choice is saved for the next launch.

### Fixed

- Clearing or invalidating a day or night length no longer leaves the previous `wm_timeScale` and `wm_timeScaleNight` numbers on screen. The pending scales clear, the line under the boxes says what is missing, and Apply stays off. The file is unchanged until Apply, and the saved values stay on their own line. The same rules apply when a scale box is cleared.
- Page titles, group headings, and monospace CVAR names use the theme text color. Those styles had been replacing the themed text style and falling back to black.
- Body text is 15pt. Page titles are 24pt and group headings 18pt, both semibold.
- The Theme and Unused CVARs boxes are 32px tall, with room for the full label, in line with the Find box and the toolbar buttons.
- Dark theme paints the page behind the editor cards. That area was the system window color, which stays white. Secondary text is darker in the light theme and lighter in the dark theme so helper lines meet WCAG AA on those backgrounds.
- The Theme and Unused CVARs boxes show their labels (Follow system, Light, Dark; Comment them out, Omit them) instead of the option type name.
- The damage-matrix explanation is shown only on the faction damage matrix page.
- Factions 3–15 are labeled "Faction 3" through "Faction 15", with no built-in role.
- The window no longer crashes on startup. The Save command was asked whether it could run while the window was still loading, before the editor model existed.

### Changed

- Validation text is shown only on the section that owns the CVAR. Other sections keep a warning count on the section list, and the status bar shows a short "1 warning" link instead of the full message.
- The Save button uses the same blue in Light and Dark, with white text, including hover and pressed.
- `sv_servername` is edited as the plain server name. The editor writes the quotes (`sv_servername="My Server"`). The template's `""` is not offered as a reference value, and neither is any other empty or quote-only template value.
- Light theme no longer uses pure white surfaces. The page is `#E9ECEF`, cards are `#F4F5F7`, and inputs are `#F1F3F5`, with softer borders. Text contrast stays at WCAG AA or better.
- An unexpected error is written to `%LOCALAPPDATA%\MiscreatedServerConfigurator\error.log` and shown in a dialog. The log is the exception text only, not the open hosting.cfg.
- Documented that `g_gameRules_faction2_dmg_f2` is intentionally absent. Faction 2 is bases and vehicles: a base cannot damage a base, and vehicle damage is handled elsewhere. The cell stays out of the catalog. Unspecified values use the server's stock value.

## [0.1.0-dev] - 2026-11-14

### Added

- Phase 1 hosting.cfg editor for Miscreated dedicated servers (Steam app 299740).
- Core library that parses and writes `hosting.cfg`: comments start with `-- `, live settings are `key=value` with no trailing comment, and unchanged lines keep their original text and order.
- CVAR catalog from the 13 November 2026 reference template, including factions 0–15 and the faction damage matrix. `sv_maxplayers`, `g_idleKickTime`, `wm_startPattern`, and `g_respawnAtBaseTime` are included because cited sources name them; they are not assignment lines in the template. `g_gameRules_faction2_dmg_f2` is intentionally absent and is not in the catalog.
- Choice for unused CVARs: comment them out, or omit them, so the server keeps its factory default.
- `steam_ugc` list editing: comma-separated Workshop file ids, no spaces, including ids pasted from a Workshop URL.
- WPF editor with the template's sections, masked secrets, and save / save as.
- xUnit coverage for round-trip of the reference template, the comment-or-omit choice, and `steam_ugc`.
- EditorConfig, analyzers, and `EnableWindowsTargeting` so the solution builds on Linux CI. The UI still runs on Windows.
- First draft of the hosting.cfg user guide (`docs/hosting-cfg.md`).
