# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- The window no longer crashes on startup. The Save command was asked whether it could run while the window was still loading, before the editor model existed.

### Changed

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
