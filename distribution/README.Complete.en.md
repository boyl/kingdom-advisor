# Kingdom Advisor — Complete loader + mod (0.6.7-r1)

Contains **BepInEx 6.0.0-be.788 + 5b766a3, Il2CppInterop 1.5.3, two patched Cpp2IL assemblies, and Kingdom Advisor 0.6.7-r1**. The in-game mod version remains 0.6.7. This is a modified compatibility distribution, not stock official #788. Changes are identified in `THIRD-PARTY-NOTICES.md`.

Tested on Windows x64, game 2.4.2 / Unity 6000.0.66f2: fresh interop generation, restart, mod loading, catalog and notification removal. Other versions, operating systems, multiplayer and all other-plugin combinations are not fully verified.

## One-click installation

1. Save and exit the game. Back up saves separately if desired; the installer never accesses saves.
2. Extract the entire archive to a normal directory. Do not run inside the ZIP or manually merge `payload` into an old loader first.
3. Double-click **INSTALL.cmd**. Requires PowerShell 7; if absent, install it using the link shown by the launcher, then retry. Windows PowerShell 5.1 is not used.
4. Steam libraries are detected automatically. If multiple or none are found, enter the directory containing `KingdomTwoCrowns.exe`. UAC is requested only if access is denied; cancellation fails the install.
5. Wait for “Installation verified”. First game launch downloads matching Unity libraries and generates interop. This can take several minutes. A delayed window does not mean a crash.
6. F7 opens the catalog, F6 opens settings. Preserve logs and roll back if startup fails.

The installer verifies every payload hash, backs up old files, replaces the complete `BepInEx/core` and `dotnet` directories, and moves old `interop` and `cache` into backup for regeneration. It preserves saves, settings, unrelated game files and other plugins; replaces the mod DLL and root loader files; and restores on failure. Incomplete recovery is explicitly reported.

```text
Extracted archive/
├─ INSTALL.cmd                       ← run this
├─ install.ps1 / manifest.json
├─ README.md / README.en.md
├─ licenses/ / sources/              (not installed into game)
└─ payload/
   ├─ winhttp.dll / doorstop_config.ini / .doorstop_version / changelog.txt
   ├─ dotnet/
   └─ BepInEx/
      ├─ core/
      └─ plugins/KingdomAdvisor/KingdomAdvisor.dll
```

```text
Installed game directory/
├─ KingdomTwoCrowns.exe
├─ KingdomTwoCrowns_Data/             (unchanged)
├─ winhttp.dll / doorstop_config.ini / .doorstop_version
├─ dotnet/
├─ .install-backups/                  (old environment and install record)
└─ BepInEx/
   ├─ core/                          (#788 + two parser patches)
   ├─ config/                        (preserved or created at first launch)
   ├─ interop/                       (generated; not bundled)
   ├─ unity-libs/                    (downloaded at first launch; not bundled)
   ├─ LogOutput.log
   └─ plugins/
      ├─ other plugins/              (preserved; compatibility unverified)
      └─ KingdomAdvisor/KingdomAdvisor.dll
```

## Explicit path, rollback and removal

```powershell
pwsh -NoProfile -File .\install.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Kingdom Two Crowns'
```

Exit the game and use the backup path printed by the installer:

```powershell
pwsh -NoProfile -File .\install.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Kingdom Two Crowns' -Rollback 'D:\SteamLibrary\steamapps\common\Kingdom Two Crowns\.install-backups\KingdomAdvisor-date-id'
```

Rollback verifies current files and stops if later updates modified them. It restores the previous core, runtime, caches, loader and mod; newly generated directories are moved to the backup's `after` folder. Saves remain untouched. Keep required backups. To remove only the mod, move `BepInEx/plugins/KingdomAdvisor` out of the game. Use rollback to undo the entire installation.

For issues, provide `BepInEx/LogOutput.log`, game version/source and archive name. `sources/patch-788-stage.ps1` reproduces parser changes and is not the install entry point.
