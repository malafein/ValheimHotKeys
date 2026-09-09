using System.Reflection;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;

namespace ValheimHotKeys
{
    [HarmonyPatch]
    public static class HotKeyPatches
    {
        // Player.TakeInput() is protected, so it has to be reached by reflection.
        // Resolved once instead of per-frame inside the Player.Update postfix.
        private static readonly MethodInfo TakeInputMethod = AccessTools.Method(typeof(Player), "TakeInput");

        // Valheim 1.0 drives HUD visibility through Hud.m_userHidden, which Hud.Update
        // feeds into SetVisible(); that moves m_rootObject off-screen instead of
        // deactivating it. Flipping the flag from a prefix lets the vanilla Update apply
        // it the same frame and keeps this hotkey in sync with the game's own Ctrl+F3
        // toggle and with Hud.IsVisible().
        [HarmonyPatch(typeof(Hud), "Update")]
        [HarmonyPrefix]
        public static void Hud_Update_Prefix(Hud __instance)
        {
            if (InputHelpers.IsDownExact(Plugin.ToggleHUDConfig.Value))
            {
                __instance.m_userHidden = !__instance.m_userHidden;
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        [HarmonyPostfix]
        public static void Player_Update_Postfix(Player __instance)
        {
            // Only handle input if we are the local player.
            // Using reflection for TakeInput since it may be private/protected
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            bool takeInput = (bool)TakeInputMethod.Invoke(__instance, null);
            if (!takeInput)
            {
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                if (InputHelpers.IsDownPermissive(Plugin.HotbarConfigs[i].Value))
                {
                    __instance.UseHotbarItem(i + 1);
                    return; // Only use one thing per frame
                }
            }

            // Custom Item Hotkeys
            for (int i = 0; i < Plugin.CustomItemBindings.Count; i++)
            {
                var binding = Plugin.CustomItemBindings[i].Value;
                if (InputHelpers.IsDownPermissive(binding.Shortcut))
                {
                    string itemName = binding.ItemName;
                    if (string.IsNullOrEmpty(itemName)) continue;

                    Inventory inventory = __instance.GetInventory();
                    var allItems = inventory.GetAllItems();
                    string searchName = itemName.ToLower();

                    // Find all items matching the name (allows for cycling matches like "Arrow" -> "Fire Arrow", "Wood Arrow")
                    var matches = allItems.FindAll(iData => 
                        Localization.instance.Localize(iData.m_shared.m_name).ToLower().Contains(searchName) || 
                        iData.m_shared.m_name.ToLower().Contains(searchName)
                    );

                    if (matches.Count > 0)
                    {
                        ItemDrop.ItemData itemToUse = matches[0];

                        // If we have multiple matches, try to find the "next" one if one is already equipped
                        if (matches.Count > 1)
                        {
                            int equippedIndex = matches.FindIndex(m => m.m_equipped);
                            if (equippedIndex != -1)
                            {
                                // Move to the next item, wrapping around
                                itemToUse = matches[(equippedIndex + 1) % matches.Count];
                            }
                        }

                        __instance.UseItem(inventory, itemToUse, true);
                        return; // Only use one thing per frame
                    }
                    else
                    {
                        ZLog.LogWarning($"[ValheimHotKeys] No item matching '{itemName}' found in inventory.");
                    }
                }
            }
        }
    }

    // Valheim 1.0 runs on Unity 6 with the new Input System package, so the legacy
    // UnityEngine.Input API no longer reports key state -- and neither does BepInEx's
    // KeyboardShortcut.IsDown(), which is built on it. ZInput exposes KeyCode-based
    // equivalents backed by the new system, so every key read goes through it.
    public static class InputHelpers
    {
        private static readonly KeyCode[] ModifierKeys =
        {
            KeyCode.LeftControl,
            KeyCode.RightControl,
            KeyCode.LeftShift,
            KeyCode.RightShift,
            KeyCode.LeftAlt,
            KeyCode.RightAlt
        };

        /// <summary>
        /// A more permissive version of KeyboardShortcut.IsDown() that ignores extra keys being held (like 'W').
        /// </summary>
        public static bool IsDownPermissive(KeyboardShortcut shortcut)
        {
            if (!IsMainKeyDown(shortcut)) return false;

            // All required modifiers MUST be held
            foreach (var mod in shortcut.Modifiers)
            {
                if (!ZInput.GetKey(mod, false)) return false;
            }

            // We explicitly DON'T check if other keys are held to allow usage while walking/running.
            return true;
        }

        /// <summary>
        /// Requires an exact modifier match: every listed modifier held and no other one.
        /// Used where a permissive match would also fire on a vanilla binding that shares
        /// the same main key -- Valheim 1.0 toggles the HUD on Ctrl+F3, so a plain F3
        /// binding must not respond to it as well.
        /// </summary>
        public static bool IsDownExact(KeyboardShortcut shortcut)
        {
            if (!IsMainKeyDown(shortcut)) return false;

            foreach (var mod in ModifierKeys)
            {
                bool required = false;
                foreach (var wanted in shortcut.Modifiers)
                {
                    if (wanted == mod)
                    {
                        required = true;
                        break;
                    }
                }

                if (ZInput.GetKey(mod, false) != required) return false;
            }

            return true;
        }

        private static bool IsMainKeyDown(KeyboardShortcut shortcut)
        {
            KeyCode mainKey = shortcut.MainKey;

            // ZInput rejects Mouse5/Mouse6 and anything above JoystickButton19.
            if (mainKey == KeyCode.None || !ZInput.IsKeyCodeValid(mainKey)) return false;

            // The main key MUST be pressed THIS frame
            return ZInput.GetKeyDown(mainKey, false);
        }
    }
}
