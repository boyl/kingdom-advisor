# Kingdom Advisor 0.6.7-r2 — Nexus manual installation

Nexus packages use manual installation, contain no automatic installer and do not require PowerShell 7. The in-game plugin remains 0.6.7; accepted gameplay binaries are unchanged.

## Choose one package

- **ModOnly-Manual**: Advisor only. Requires a working BepInEx 6 Unity IL2CPP Windows x64 installation.
- **Complete-Manual**: Advisor plus a modified complete BepInEx #788 runtime. For fresh installs or the AndroidManager / RawPropertyType interop generation error. This is not stock official #788.

## Find the game folder

Steam library → right-click Kingdom Two Crowns → Manage → Browse local files. Use the folder containing `KingdomTwoCrowns.exe`, for example `D:\SteamLibrary\steamapps\common\Kingdom Two Crowns\`.

## ModOnly

1. Save and quit the game. Extract the archive to a normal folder.
2. Copy the package's **BepInEx folder** into the game root, merging with the existing BepInEx folder. Replace only `plugins/KingdomAdvisor/KingdomAdvisor.dll`.
3. Keep other plugins and settings, then launch normally.

Final path: `Game root/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`.

## Complete

1. Save and quit. Create a backup folder **outside the game root**.
2. If a loader is already installed, move the entire existing `BepInEx` and `dotnet` folders and root files `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt` into that backup. Skip missing items. Do not merge new core files into the old core directory.
3. Copy the archive's `BepInEx`, `dotnet` and those four root files into the folder containing the game EXE. The README, image, licenses and source information need not be copied into the game.
4. Copy back `BepInEx/config` from the backup to preserve settings and journals. Restore other plugins only if they support this runtime; retain the new Advisor DLL. Do not restore old core, interop, cache or dotnet folders.
5. Launch normally. First launch requires internet to download matching Unity libraries and may take several minutes to generate assemblies.
6. Press F7 for the catalog or F6 for settings. If startup fails, retain `BepInEx/LogOutput.log` and restore the backup as below.

The package contains no game files, generated assemblies, settings or saves. Preserve the original EXE, Data folder and other game files.

## Placement diagram

![Installation diagram](INSTALLATION.png)

```text
Complete archive:                   Game root:
BepInEx/                 ────────→  BepInEx/
dotnet/                  ────────→  dotnet/
winhttp.dll              ────────→  winhttp.dll
doorstop_config.ini      ────────→  doorstop_config.ini
.doorstop_version        ────────→  .doorstop_version
changelog.txt            ────────→  changelog.txt

Kingdom Two Crowns/
├─ KingdomTwoCrowns.exe            ← Locate installation folder
├─ KingdomTwoCrowns_Data/          ← Preserve original game content
├─ winhttp.dll
├─ doorstop_config.ini
├─ .doorstop_version
├─ changelog.txt
├─ dotnet/
└─ BepInEx/
   ├─ core/                       ← New runtime from Complete
   ├─ plugins/
   │  └─ KingdomAdvisor/
   │     └─ KingdomAdvisor.dll    ← Required plugin location
   ├─ config/                     ← Restore previous settings
   └─ interop/                    ← Generated on first launch
```

ModOnly supplies only the plugin path above. Do not add the archive's enclosing directory, create BepInEx/BepInEx or install inside KingdomTwoCrowns_Data.

## Update, uninstall and rollback

Update only Advisor: quit and replace the DLL, retaining settings and the loader.

Uninstall only Advisor: quit and move `BepInEx/plugins/KingdomAdvisor` out of the game folder, preserving the loader used by other plugins.

Undo Complete: quit, move the current BepInEx/dotnet folders and four installed root files into a separate backup, then restore the original backed-up items. For a fresh install without an old backup, move out the files added by this package. Preserve game files and saves.

## Compatibility and sources

Accepted on Windows x64, game 2.4.2, Unity 6000.0.66f2: fresh generation, restart and user gameplay. Only LibCpp2IL.dll and Cpp2IL.Core.dll are patched; remaining runtime files come from official #788. Other game versions, all DLC, multiplayer and all other-plugin combinations are not fully verified.

Corresponding source archives, patch script and reproduction instructions are available in the [public GitHub release](https://github.com/boyl/kingdom-advisor/releases/tag/v0.6.7-r1), whose Complete archive includes pinned third-party sources. Sources are not installed into the game. Nexus Complete retains license texts and component provenance.
