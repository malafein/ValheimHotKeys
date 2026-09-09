# Valheim HotKeys

A Valheim mod to add configurable hotkeys for various actions.

## Features

- **Toggle HUD**: Quickly hide/show the in-game HUD (default `F3`).
- **Hotbar Slot Bindings**: Assign custom keys to any of the 8 hotbar slots.
- **Custom Item Bindings**:
    - Bind hotkeys to items by name (e.g., "Healing Mead", "Arrow"). This can be either the localized name or the internal item ID.
    - **Partial Matching**: "Arrow" will match "Fire Arrow", "Wood Arrow", etc.
    - **Item Cycling**: Press the hotkey multiple times to cycle through all items in your inventory that match the search name.
    - Bindings fire while you are moving, so you can drink or swap ammo on the run.

## Installation

### Thunderstore / r2modman (Recommended)
- Install via Thunderstore Mod Manager or r2modman.  
-or-  
- Download the mod from [Thunderstore](https://thunderstore.io/c/valheim/p/malafein/ValheimHotKeys/), and follow the Manual Installation instructions below.

### Nexus Mods / Vortex
- Install via Vortex Mod Manager.  
-or-  
- Download the mod from [Nexus Mods](https://www.nexusmods.com/valheim/mods/3218), and follow the Manual Installation instructions below.

### Manual Installation
1. Install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Download the latest release of ValheimHotKeys from [GitHub](https://github.com/malafein/ValheimHotKeys/releases).
3. Extract the `ValheimHotKeys.dll` file into your `<Valheim Install Folder>\BepInEx\plugins` directory.

> **Compatibility**: version 1.1.0 and later require Valheim 1.0 or newer. Use 1.0.0 for earlier game versions.

## Configuration

The `com.malafein.valheimhotkeys.cfg` file will be generated in your `BepInEx/config` folder after the first run.  
There you can set the HUD toggle key, the eight hotbar slot bindings, and the eight custom item bindings.

**Recommendation**: Use a configuration manager like [shudnal's Configuration Manager](https://github.com/shudnal/ConfigurationManager) to easily edit bindings and settings in-game.

Mouse buttons may be bound, but only `Mouse0` through `Mouse4` — Valheim does not resolve higher mouse buttons.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Bugs & Known Issues

Feel free to log any issues you encounter while using this mod on [GitHub](https://github.com/malafein/ValheimHotKeys/issues).
