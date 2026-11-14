# hosting.cfg editor

This is the first draft of the user guide for Phase 1. It covers opening and saving a file, required keys, unused CVARs, factions, and `steam_ugc`. The day/night calculator is not in this version.

The editor's catalog comes from the reference template shipped as `samples/hosting.cfg` (recopied 13 November 2026). That file is the source of truth for which CVARs exist and for the commented values it shows. Official help at <https://servers.miscreatedgame.com/help> is partly outdated. Where the two disagree, the editor shows a note and leaves the template value alone.

## Create or open a file

**New from template** loads the reference template. The only live lines are `http_password=CHANGE_ME` and an empty `sv_servername`. `CHANGE_ME` is a placeholder. Replace it before you start the server. The template does not contain a real password, API key, or account name.

**Open** reads a UTF-8 `hosting.cfg`. A file that is not valid UTF-8 is left untouched.

**Save** and **Save as** write UTF-8 without a byte-order mark. The editor does not keep a separate copy of the file, and it does not send the file anywhere. Passwords and `sv_steamWebAPIKey` are masked in the window until you choose Show.

Changes apply on the dedicated server after a restart. Editing the file does not restart a server.

## How a line is written

A line that starts with `-- ` is a comment. The server ignores it.

A setting the server reads is a single line:

```text
key=value
```

Do not put a comment after the value. The template's own header says a trailing comment breaks server parsing. When this editor turns a commented CVAR into a live line, it writes `key=value` only.

A value cannot contain a line break; the editor strips any newline you paste. For `sv_servername`, type the name only. The editor writes the quotes (`sv_servername="My Server"`). The `""` in the reference template is not an example name. It only shows where the quotes go. Other template values that are empty, or only a pair of quotes, are not offered as a reference value.

Comments that are not CVAR lines, and any CVAR you have not changed, are written back as they were. A key that is in your file but not in the catalog is kept under **Other keys in this file** so a save does not drop it. `schedule_shutdown_utc` is one of those: it is not in the reference template, so it is not offered as a catalog CVAR, and it is preserved if your file already has it.

## Unused CVARs and factory defaults

If you do not set a CVAR, the server uses its factory default. In the editor, clear **Use this value** to leave a CVAR unused.

The toolbar chooses what an unused CVAR looks like in the file:

- **Comment them out** writes `-- key=value`, or leaves the original comment alone when you have not edited that line. This matches the reference template.
- **Omit them** removes that CVAR line. Prose comments stay.

Neither choice writes a live setting, so both mean "factory default." The numbers shown as **Reference template** are the values in the reference file. That file's header calls them the defaults. Some of them disagree with official help or with `miscreatedcvars.txt`. The note on the field says so. Use reference value copies the template text into the field; it does not silently prefer the other number.

## Required keys

The template says two settings are required for a uniquely named server that can be administered over RCON:

| Key | Role |
| --- | --- |
| `http_password` | RCON password. Do not use the same value as `sv_password`. Not listed on the official help page. |
| `sv_servername` | Name in the server browser. Type the name. The editor adds the quotes. |

The editor warns, and still lets you save, when the RCON password is missing or still `CHANGE_ME`, when the server name is empty, or when the two passwords match. The warning does not repeat a password you typed.

`sv_password` is the join password. The template says players also need it in `user.cfg`, and not to set it if the server should stay public. It is not listed on the official help page.

## Sections

The list on the left follows the template:

- **Core** — the required keys, plus server name, passwords, messages, uptime, prone, and `sv_maxplayers`.
- **Logging**
- **Ping** — includes `g_idleKickTime`, which official help says does not work. It is not treated as reliable, and no default is recorded for it.
- **Steam** — inventory flag, `steam_ugc`, and the Web API key.
- **Weather** — time scale, patterns, and `wm_startPattern` (official help; not an assignment line in the template).
- **Game Rules & Player** — includes `g_respawnAtBaseTime` from `miscreatedcvars.txt` (default 30 there). It is not an assignment line in the template.
- **AI**
- **Factions**
- **Faction damage matrix**

`sv_maxplayers` is a hosting.cfg CVAR. `miscreatedcvars.txt` names it and does not describe it or give a default, and the reference template has no line for it. Leave it unused to keep the factory default. The same is true of any field whose description is blank: the sources used here do not say what it means.

## Factions 0–15

Official help documents built-in factions 0–2 and custom factions 3–6. That page is outdated. Factions 7–15 are real and affect the server. The editor follows the template:

| Keys | Factions |
| --- | --- |
| Enable | 3–15. Factions 0, 1, and 2 have no enable line. |
| Name, equipment, spawn position | 0 and 3–15 |
| Steam ids (semicolon-separated) | 3–15 |

Names in the template are the placeholder `factionnamehere`. The template says letters only, no spaces.

Equipment: the template says set the value to `""` for the usual random items, put clothing first, and that order matters. Items are given only when there is inventory space. The faction 0 example is the lawmen list and includes the two tokens `flexcap_police` and `frontback_blue`. The faction 3 example is the outlaw list. Factions 4–15 have an empty equipment line and no example list.

Spawn positions are `X,Y,Z`. Z is height relative to the ground; use 0 if players should not spawn in the air.

## Faction damage matrix

The matrix labels are:

- f0 players
- f1 AI
- f2 bases and vehicles
- Faction 3 through Faction 15, with no built-in role
- 1.0 normal damage, 2.0 double damage, 0 no damage

Factions 3–15 have no built-in role. A server owner may use a custom faction for anything, including an admin group. That is their choice, not a name this editor assigns. Official help describes built-in factions differently (0 unaffiliated players, 1 environment, 2 bases). The template's meanings for 0, 1, and 2 are the ones shown here. The note on that page records the disagreement.

Each row is the faction dealing damage. Each column is the faction taking it. A cell is one CVAR, `g_gameRules_factionX_dmg_fY`. Uncheck a cell to leave it unused. The template comments `1` on every cell it contains. Those lines have no per-cell description beyond the header.

`g_gameRules_faction2_dmg_f2` is intentionally absent. Faction 2 is bases and vehicles: a base cannot damage a base, and vehicle damage is handled elsewhere. That cell stays out of the catalog and is shown as a dash. Leaving a cell unchecked, like any other unused CVAR, does not write a value, so the server uses its stock value. If a file already contains that key, the cell becomes editable and the line is preserved.

## steam_ugc

`steam_ugc` is the CVAR that lists the Workshop mods the server runs. It is not on the official help page. Both official Entrada server videos use it.

Ids are decimal Workshop file ids from the Workshop URL (`?id=123456789`). In the file they are comma-separated with no spaces:

```text
steam_ugc=111,222,333
```

Community host guides describe that order as load order. The editor writes the list in the order shown. Add accepts one id, one Workshop URL, or several separated by commas. Checking **Use this value** with an empty list writes `steam_ugc=` (an empty value). Leaving it unchecked does not write a live line.

`sv_steamWebAPIKey` is a secret for the Steam inventory kiosk, from the template's comment. The editor masks it. Get a key from Steam's API key page if you need one; do not put a key into the reference template or into source control.

`steam_inventory_enable` is a known disagreement. The template comments `1` and says 1 enabled / 0 disabled. `miscreatedcvars.txt` says the default is 0, that 0 means fully enabled, that 3 means admin disabled, and not to use 1 or 2. Official help has no 0/1 table. The editor does not pick a winner.

## Notes that stay informational

These are recorded so the editor does not quietly "fix" a template value. The template value stays what the file says.

| Key | Template comment | Other source |
| --- | --- | --- |
| `g_playerFoodDecay` | 0.208333 | `miscreatedcvars.txt` default 0.2777 |
| `g_playerFoodDecaySprinting` | 0.260416 | `miscreatedcvars.txt` default 0.34722 |
| `g_playerWaterDecay` | 0.32407 | `miscreatedcvars.txt` default 0.4861 |
| `g_playerWaterDecaySprinting` | 0.40509 | `miscreatedcvars.txt` default 0.607638 |
| `g_playerTemperatureEnvRate` | 0.00125 | Official help default 0.0005 |
| `asm_percent` | 50 (max 90) | Official help / `miscreatedcvars.txt` default 33 |
| `ism_maxCount` | 500 (range 0 to 5000) | Official help default 750 |
| `wm_forceTime` | commented value 1, text says default -1 | `miscreatedcvars.txt` and the same comment: default -1 |
| `wm_timeScaleWeather` | described as a multiplier on `wm_timeScale` | Official help: weather speed is independent of day and night |
| `g_pinglimit` | template spelling and commented 0 | `miscreatedcvars.txt` spells it `g_pingLimit` and says default 1000 |
| `sv_maxuptime` | commented 25.3588; server will not stay up more than 24 hours | `miscreatedcvars.txt`: default 24 whitelisted, 12 otherwise, cannot exceed 24 |
| `g_gameRules_bases` | 0 / 1 / 2 as in the template | `miscreatedcvars.txt` says 2 is whitelisted only. Official help warns that building anywhere can delete bases in exclusion zones. |

`g_pinglimitGraceTime`, `wm_disable`, `http_password`, `sv_password`, `steam_ugc`, and `sv_steamWebAPIKey` are in the template (or, for the last two, in the videos and the template) and are not listed on the official help page. They are still real.

No formula is known here for turning the four food and water decay CVARs into minutes until a player is hungry or thirsty. This version does not calculate one.

## Day and night length

The Weather page edits `wm_timeScale` and `wm_timeScaleNight` only in the day/night panel. Enter the real hours you want, or edit the two scales in that panel; either side updates the other. Each one can be turned off, and the server then uses its stock default for that CVAR. `wm_timeScaleWeather` stays in the list and is not part of the day or night length. The explanation and the formula are in [day-night-calculator.md](day-night-calculator.md).

## Not in this version

- Publishing or updating a Workshop mod. `steam_ugc` only stores ids.
- RCON, bans, whitelist commands, or restarting a live server. The template's header and MisServer-101 keep those off the config file: over RCON the separator is a space, and the change lasts until restart.
- Deploying the file to a host.
