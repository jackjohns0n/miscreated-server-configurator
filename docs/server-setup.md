# Server download and setup

Server → Download and setup installs a Miscreated dedicated server and writes `StartServer.bat`. The app does not run the server. You start it by running that batch file.

The behavior checklist (SteamCMD, Workshop files, Visual C++ runtimes, firewall ports, grant-all-guides, a start script) matches the kind of steps community setup scripts perform. Spafbi's scripts were that checklist. They are unlicensed and were not copied.

## Folders

You pick one root, for example `D:\MiscreatedServers`. The box starts empty. The app will not use your Documents folder or Program Files as that root.

```text
D:\Tools\steamcmd\          app-wide SteamCMD, chosen once, not under the server root
D:\MiscreatedServers\
  MyServer\
    Bin64_dedicated\        dedicated server files (app 302200)
    hosting.cfg             written from the editor, not generated separately
    StartServer.bat         generated start script
    steamapps\workshop\     Workshop content copied here
    miscreated.db           created by the server the first time it starts
```

Each server is `<root>\<server name>\`. SteamCMD is a separate folder you pick. It is also what later Workshop upload and download features will use. The box starts empty. The app will not use Documents or Program Files.

If `steamcmd.exe` is already in `<root>\steamcmd` from an older build, the window asks whether to use that folder. It does not move the files.

The server root, the SteamCMD folder, and the known servers are stored together in `%LOCALAPPDATA%\MiscreatedServerConfigurator\servers.json` (`steamCmdPath` next to `rootPath`). One settings file is enough; SteamCMD is not a per-server value. The theme choice is the separate file `theme.txt` in that same directory.

## What the buttons do

**Download SteamCMD** asks for the SteamCMD folder if it is not set, then downloads Valve's official `steamcmd.zip` (`https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip`) when `steamcmd.exe` is not already there. The zip is deleted after a successful extract. Install server, Download mods, and the start script all use this same folder. If it is unset, those actions ask for it first.

**Install server** runs SteamCMD as:

```text
+force_install_dir <server> +login anonymous +app_update 302200 [-beta <name> -betapassword <password>] validate +quit
```

If that exits with an error, it runs the same command once more. It then checks that `Bin64_dedicated\MiscreatedServer.exe` exists. Beta branch and password are the fields on this window. There is no `beta.ini`.

**Download mods** reads `steam_ugc` from the hosting.cfg currently open in the editor. Only numeric ids are used. Each id is downloaded with:

```text
+login anonymous +workshop_download_item 299740 <id> +quit
```

That command does not set `force_install_dir`, so the files land in the SteamCMD folder's cache (`steamapps\workshop` under the folder you chose). The app then copies each item and `appworkshop_299740.acf` into `<server>\steamapps\workshop` with its own file copy, not robocopy. Whether SteamCMD can download those items straight into the server folder was not confirmed, and this app does not depend on that. Anonymous download of app 299740 is also unverified. If an item is missing from the cache, the log says so.

**Write hosting.cfg** writes the editor's current document to `<server>\hosting.cfg`. The editor remains the only place that builds that file.

**Install runtimes** looks in the registry for the x64 Visual C++ 2012, 2013, and 2015-or-later runtimes (`VC\Runtimes\x64`, Installed = 1). If one is missing, the window tells you which official Microsoft installer it will download and why. 2012 and 2013 are run with `/quiet /norestart`. The 2015-or-later installer is run with `/install /quiet /norestart`, because the older installers do not accept `/install`. winget is not used. A declined administrator prompt leaves the machine unchanged.

**Add firewall rules** asks first, then asks Windows for administrator approval. It opens the base game port and the next four ports, UDP and TCP. Published notes disagree about TCP versus UDP, so both are opened. For port 64090 that is 64090–64094. The rules are named `Miscreated Server (<name>) TCP` and `Miscreated Server (<name>) UDP`. Running the command again deletes those names first, so it does not stack copies. **Remove firewall rules** deletes those two names and nothing else. A declined administrator prompt is reported and does not change the rules.

**Grant all guides** is optional. When it is on, every row in `ServerAccountData` is given `Guide00 = '-1'` and `Guide01 = '-1'`, and a trigger keeps doing that after each update to `Characters`. That applies to every account on purpose. The app copies `miscreated.db` to a `.bak-` file beside it before it changes anything. Turning the option off drops the trigger and leaves the stored guide values as they are. The quotes around `-1` are what the statement uses. Whether those columns are text or numbers in a real database was not checked. If `miscreated.db` is not there yet, the window says the server creates it on the first start. Use **Apply guide setting** after that.

**Write start script** writes `<server>\StartServer.bat` and does not run it. The script:

- stops when `stop.txt` is in the server folder
- runs `app_update 302200 validate` through the SteamCMD folder saved in `servers.json`, and retries that update once
- passes the beta arguments when a beta branch is set
- deletes and recreates the `Mods` folder
- starts `Bin64_dedicated\MiscreatedServer.exe` with `-sv_bind`, optional `-mis_whitelist`, `-sv_port`, `-mis_gameserverid` (default 100), `+sv_maxplayers`, `+map`, and `+http_startserver`
- after the server exits, waits 10 seconds: Q quits, C continues, and waiting restarts

Workshop downloads stay in the app so a failed anonymous download is visible in the log. The script only updates the dedicated server and launches it.

## Launch settings

| Field | Default | Notes |
| --- | --- | --- |
| Map | `islands` | Editor default. Not read from the server. |
| Game port | `64090` | Must leave room for four more ports. |
| Max players | `50` | Editor default, from 1 to 128. Not written into hosting.cfg. |
| Whitelisted | off | Adds `-mis_whitelist`. |
| Server id | `100` | `-mis_gameserverid`. |
| Bind IP | `0.0.0.0` | IPv4. |
| Grant all guides | off | Database trigger, not a start-script flag. |
| Beta / password | empty | Optional. The password is stored in the batch file. |

Max players and whitelist can be changed and saved with no extra prompt. If a start script is already in the folder, changing the map, port, server id, or bind IP asks first. Those changes can mean a different world, new port forwarding, a different server database, or a different listen address.

## Load server

**Load server** asks for an existing server folder.

- `hosting.cfg` in that folder is opened in the editor, after a prompt when the editor has unsaved edits. It is read with the same parser as File → Open.
- `StartServer.bat` is read in full when this app wrote it (the file contains `rem MSC-START 1`).
- Any other batch file in the folder is read for the flags it recognizes (`-sv_bind`, `-sv_port`, `-mis_gameserverid`, `-mis_whitelist`, `+sv_maxplayers`, `+map`, `-beta`, `-betapassword`). Lines that are not one of those, and are not ordinary batch structure, are listed in the output box.

## What still needs a Windows machine

These were not run from the build environment:

- An anonymous Workshop download of app 299740.
- The firewall script and a declined or accepted UAC prompt.
- The Visual C++ installers in silent mode.
- The guide trigger against a `miscreated.db` created by the real server.
- Starting the server from `StartServer.bat` and joining it.
