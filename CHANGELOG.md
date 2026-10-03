# Changelog

## Unreleased
* Hotbar slot bindings are back, for key combinations with Shift, Ctrl, or Alt, which Valheim's Controls menu can't bind. Settings from before 1.3.0 are picked up again.
* Custom item bindings are now named Item 1-8 instead of Slot 1-8, so they aren't mistaken for hotbar slots. Existing bindings carry over.
* Binding a custom item key while holding the right Shift, Ctrl, or Alt now saves the right one. It used to save the left one, so the binding then only worked with the left key.
* The log warns when a binding shares a key with one of the game's own bindings.

## 1.3.0
* Removed the hotbar slot bindings. Valheim now has a rebindable alternate key for each hotbar slot in its own Controls menu. Any leftover [Hotbar] settings in your config file are ignored.
* A custom item binding whose item is not in your inventory no longer logs a warning.

## 1.2.0
* Added a Repair Hammer hotkey (unbound by default): one press equips your hammer with repair selected, a second press puts it away.

## 1.1.1
* Corrected documentation.

## 1.1.0
* Updated for the Valheim 1.0 release, which moved the game to Unity 6.
* Mouse bindings are now limited to Mouse0 through Mouse4, the buttons Valheim's input system supports.

## 1.0.0
* Initial release.
* Configurable hotkeys for HUD toggling and hotbar slots.
* Custom item bindings with partial matching and item cycling.
* Support for localized and internal item IDs in custom bindings.
