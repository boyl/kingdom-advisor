# Kingdom Advisor 0.6.0

An in-game information HUD and optional convenience mod for Kingdom Two Crowns. Loaded automatically by BepInEx; no separate launcher.

## Languages

The default Auto mode follows the game's selected language. Chinese variants use Simplified Chinese. English and all other languages use English. If the game language is not available yet, the system language is used; unknown languages fall back to English.

You can override the language in F6 → Catalog and journal → Language: Auto, Simplified Chinese, or English. Changes apply immediately. Existing configuration keys and saved journal data are preserved. The catalog accepts Chinese names and English names, descriptions, or internal keywords in either display language.

## Features

- Stable island minimap with building names, counts, camps and moving enemy groups. Focus a location for levels, locks, stock, health and construction progress.
- Draggable kingdom advisor with resource, population, technology and mount icons.
- Catalog, categorized search, points of interest and island observation journal.
- Per-panel transparent or panel backgrounds. The minimap and advisor default to transparent; the main interface defaults to a panel.
- Large high-contrast mouse and controller cursors. The right stick controls the cursor independently of character movement.
- Optional unlimited purse capacity, manual resource addition and clearing, hold-to-teleport, unlimited stamina and game speed controls.

Gameplay assists default to off, speed to 1x. Disabling unlimited capacity restores the game's native purse, pickup and overflow behavior. Resource clearing executes directly without a second confirmation.

## Requirements and installation

Verified runtime: Windows x64, Kingdom Two Crowns 2.4.2, Unity 6000.0.66f2, IL2CPP, BepInEx 6.0.0-be.738. Install the appropriate BepInEx IL2CPP version separately; this package includes no game assemblies or BepInEx runtime.

Save and quit from the game menu. Copy the ZIP's BepInEx folder into the game directory. For updates, replace BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll; existing settings are preserved. Launch the game normally afterward.

To uninstall, save and quit, then remove that DLL. Configuration remains at BepInEx/config/local.kingdom.advisor.cfg.

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

Single-player was accepted by the user in 0.5.8. The bilingual changes in 0.6.0 include language-resolution tests, complete authored-text coverage checks and in-game screenshots in both languages. Multiplayer remains available, but two-client synchronization and the complete campaign/DLC matrix have not been verified. Other mods may alter costs, purse behavior or input mappings.

The island journal and diagnostics are stored in BepInEx/config/KingdomAdvisor/. An initial gameplay screenshot is saved; F9 requests an additional screenshot. Automated preview pages are disabled by default.

Source: https://github.com/boyl/kingdom-advisor

Downloads and updates: [GitHub Releases](https://github.com/boyl/kingdom-advisor/releases). Nexus Mods uploads are handled by the author.
