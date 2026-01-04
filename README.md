# Valheim HotKeys

A Valheim mod to add configurable hotkeys for various actions.

## Installation

1. Ensure you have BepInEx Pack for Valheim installed.
2. Download the latest release.
3. Extract the contents into your `Valheim/BepInEx/plugins` folder.

## Features
- **Toggle HUD**: Quickly hide/show the in-game HUD.
- **Hotbar Slot Bindings**: Assign custom keys to any of the 8 hotbar slots.
- **Custom Item Bindings**:
    - Bind hotkeys to items by name (e.g., "Healing Mead", "Arrow"). This can be either the localized name or the internal item ID.
    - **Partial Matching**: "Arrow" will match "Fire Arrow", "Wood Arrow", etc.
    - **Item Cycling**: Press the hotkey multiple times to cycle through all items in your inventory that match the search name.

## Configuration
 
The configuration file will be generated in `Valheim/BepInEx/config/com.malafein.valheimhotkeys.cfg` after the first run.
 
**Recommendation**: Use a configuration manager like [shudnal's Configuration Manager](https://github.com/shudnal/ConfigurationManager) to easily edit bindings and settings in-game.
