# Kingdom Advisor 0.6.5

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

Requires **BepInEx 6 · Unity IL2CPP · Windows x64**. This mod was tested with **6.0.0-be.738** and Kingdom Two Crowns **2.4.2** on Windows x64. BepInEx is not bundled and must be installed separately. Do not select Mono, x86, or BepInEx 5.

### 1. Install BepInEx

1. Open the [official BepInEx builds page](https://builds.bepinex.dev/projects/bepinex_be), find **#738**, and download `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.738+af0cba7.zip`.
2. In Steam, right-click the game → Manage → Browse local files. Locate the directory containing `KingdomTwoCrowns.exe`.
3. Extract **all contents** of the BepInEx archive into this directory, including its root files and `BepInEx` folder. Do not add an extra enclosing folder.
4. Launch the game once, wait for initialization to finish, then exit normally. The first launch may take longer and generates folders such as `BepInEx/config`.

Skip this step if the appropriate BepInEx version is already installed. See the [official installation guide](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html).

### 2. Install Kingdom Advisor

1. Download **`KingdomAdvisor-0.6.5.zip`** from the [0.6.5 release](https://github.com/boyl/kingdom-advisor/releases/tag/v0.6.5), rather than the source archive.
2. With the game closed, extract the mod and merge its `BepInEx` folder into the game directory.
3. Check that the plugin is at `BepInEx/plugins/KingdomAdvisor/KingdomAdvisor.dll`.
4. Launch the game normally. The mod loads automatically: **F6** opens settings, **F7** opens the catalog, and a short press of **left stick (L3)** opens the controller interface. No separate launcher is needed.

### Update and uninstall

To update, save and quit, replace the DLL above, and keep your configuration.

To uninstall, save and quit, then delete `BepInEx/plugins/KingdomAdvisor`. Configuration may be retained; do not remove BepInEx if other mods still use it.

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

Downloads and updates: [GitHub Releases](https://github.com/boyl/kingdom-advisor/releases). Nexus Mods updates use the official upload API.

0.6.5 passed final user acceptance. See [acceptance records](docs/acceptance/building-details-audit.md).
