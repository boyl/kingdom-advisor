# Kingdom Advisor 0.6.7-r1

In-game advisor for Kingdom Two Crowns. The plugin version remains 0.6.7; r1 removes action-result notifications and adds two installation choices.

## Download and install

[GitHub release](https://github.com/boyl/kingdom-advisor/releases/tag/v0.6.7-r1) · [Existing Nexus Mods page](https://www.nexusmods.com/kingdomtwocrowns/mods/43?tab=files)

Nexus uses 0.6.7-r2 manual packages without scripts or a PowerShell requirement: [manual guide](distribution/nexus/README.en.md) and [placement diagram](distribution/nexus/INSTALLATION.png). The table and one-click steps below apply to GitHub 0.6.7-r1 assets only.

Choose one archive, not GitHub's automatically generated Source code archives:

| Archive | Contents and intended use | Guide |
|---|---|---|
| `KingdomAdvisor-0.6.7-r1-ModOnly.zip` | Mod only; requires an existing working BepInEx 6 Unity IL2CPP Windows x64 installation | [English](distribution/README.ModOnly.en.md) · [中文](distribution/README.ModOnly.md) |
| `KingdomAdvisor-0.6.7-r1-Complete.zip` | Mod, complete #788 runtime and parser patch; for fresh installs or the AndroidManager / RawPropertyType startup error | [English](distribution/README.Complete.en.md) · [中文](distribution/README.Complete.md) |

The Complete bundle is a **modified** official BepInEx 6.0.0-be.788 distribution. Only LibCpp2IL.dll and Cpp2IL.Core.dll are patched; corresponding sources, patch script and licenses are included. Stock #738 and #788 failed fresh interop generation locally. See the [compatibility record](docs/acceptance/bepinex-738-compatibility.md).

### Install PowerShell 7 for the GitHub one-click installer

Windows 10/11's built-in Windows PowerShell 5.1 cannot run this installer. Windows Terminal does not imply PS7 is installed. Nexus manual packages do not need this step.

1. **Microsoft's official Windows x64 download:** [PowerShell-7.6.6-win-x64.msi](https://github.com/PowerShell/PowerShell/releases/download/v7.6.6/PowerShell-7.6.6-win-x64.msi), the stable release verified on October 10, 2026.
2. Alternative: [official release page](https://github.com/PowerShell/PowerShell/releases/latest) and [Microsoft Windows installation guide](https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell-on-windows). Choose a stable `PowerShell-7.x.x-win-x64.msi` asset, not ARM64, x86, Preview or Source code.
3. Run the MSI and complete the wizard, retaining the default installation directory and “Add PowerShell to Path” option. PS7 installs alongside Windows PowerShell 5.1.
4. Close the previous missing-PS7 window, return to the extracted mod folder and double-click **INSTALL.cmd** again. Do not double-click install.ps1 or run inside the ZIP.
5. If PS7 is still missing, check that Start contains “PowerShell 7”, or that `C:\Program Files\PowerShell\7\pwsh.exe` exists. The launcher checks PATH and the default directory. Include the error message when reporting problems.

1. Quit the game and extract the chosen archive to a normal folder.
2. Install [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/latest), which is required by the installer and is not bundled.
3. Double-click `INSTALL.cmd` and select the folder containing `KingdomTwoCrowns.exe` (Steam → Manage → Browse local files).
4. The installer backs up, copies and verifies files. Windows may request administrator permission for protected folders. Complete replaces old core/runtime files and moves old generated caches into backup, preserving settings, saves and other plugins.
5. Launch normally. Initial assembly generation may take several minutes. Press F6 for settings or F7 for the catalog.

For manual ModOnly installation, copy its `payload/BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll` to the location below. Use the installer for Complete to avoid mixing runtime components.

```text
Kingdom Two Crowns/                  ← Game root
├─ KingdomTwoCrowns.exe
├─ KingdomTwoCrowns_Data/
├─ winhttp.dll                      ← Runtime
├─ doorstop_config.ini              ← Runtime
├─ dotnet/                          ← Runtime
├─ .install-backups/                ← Installer backups
└─ BepInEx/
   ├─ core/                         ← Runtime
   ├─ plugins/
   │  ├─ KingdomAdvisor/
   │  │  └─ KingdomAdvisor.dll      ← Required plugin location
   │  └─ OtherPlugins/              ← Preserved
   ├─ config/                       ← Generated; preserved on upgrade
   └─ interop/                      ← Generated locally; not distributed
```

Do not copy the payload folder itself or create BepInEx/BepInEx. Both archives include Chinese and English placement diagrams, command-line instructions and rollback instructions.

## Verified scope

Windows x64, game 2.4.2, Unity 6000.0.66f2, IL2CPP. Patched #788 passed fresh generation and gameplay after restart; the user accepted it and the notification removal. Isolated installer tests covered installation, hashes, failure recovery and rollback. Live UAC interaction, other game versions, all DLC and multiplayer combinations have not been individually verified. Other plugins are preserved; their runtime compatibility depends on those plugins.

## Controls

|Input|Action|
|---|---|
|F4|Show/hide HUD|
|F6 / F7|Settings / Catalog|
|Left stick click|Tap to open/close panel; hold 0.8 s to show/hide HUD. Configurable|
|Left stick|Character movement, or menu navigation|
|Right stick|Cursor movement|
|LB / RB|Switch tabs|
|LT / RT|List/detail pages, or settings pages|
|D-pad / A / B|Navigate / Confirm / Back|
|Map click|Tap to inspect; hold 0.8 s to teleport when enabled|

Panels can be dragged with the mouse. Release before the teleport timer completes to cancel.

## Compatibility and diagnostics

The 0.6.7 release is based on released 0.6.6. It fixes input release after closing the panel: keyboard/mouse input no longer waits for a gamepad to return to neutral, and release protection clears when the gamepad is released or becomes unavailable. Automated checks, compilation and installation checks passed. The user confirmed normal operation on October 10, 2026; individual close paths and gamepad-specific scenarios have not all been tested in-game. Custom mounts are not included. The Complete bundle includes the documented Cpp2IL compatibility patch.

Single-player was accepted by the user in 0.5.8. The bilingual changes in 0.6.0 include language-resolution tests, complete authored-text coverage checks and in-game screenshots in both languages. Multiplayer remains available, but two-client synchronization and the complete campaign/DLC matrix have not been verified. Other mods may alter costs, purse behavior or input mappings.

The island journal and diagnostics are stored in BepInEx/config/KingdomAdvisor/. An initial gameplay screenshot is saved; F9 requests an additional screenshot. Automated preview pages are disabled by default.

Source: https://github.com/boyl/kingdom-advisor

Downloads and updates: [GitHub Releases](https://github.com/boyl/kingdom-advisor/releases). Nexus Mods updates use the official upload API.

0.6.6 passed final user acceptance. See [acceptance records](docs/acceptance/building-details-audit.md).

## Catalog and locations

All 104 entries have explicit identity-based pixel icons: object on the left, purpose or trait on the right. Complete patterns differ between entries. Find matching island locations from the catalog; locations show direction, distance, purpose and current interaction state, with links to the catalog and map. Separate buildings keep separate locations. The ridden mount is excluded from purchase locations. Map focus does not teleport.
