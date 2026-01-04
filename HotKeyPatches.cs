using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;

namespace ValheimHotKeys
{
    [HarmonyPatch]
    public static class HotKeyPatches
    {
        [HarmonyPatch(typeof(Hud), "Update")]
        [HarmonyPostfix]
        public static void Hud_Update_Postfix(Hud __instance)
        {
            if (Plugin.ToggleHUDConfig.Value.IsDown())
            {
                if (__instance.m_rootObject != null)
                {
                    bool isActive = __instance.m_rootObject.activeSelf;
                    __instance.m_rootObject.SetActive(!isActive);
                }
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

            bool takeInput = (bool)AccessTools.Method(typeof(Player), "TakeInput").Invoke(__instance, null);
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

    public static class InputHelpers
    {
        /// <summary>
        /// A more permissive version of KeyboardShortcut.IsDown() that ignores extra keys being held (like 'W').
        /// </summary>
        public static bool IsDownPermissive(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None) return false;
            
            // The main key MUST be pressed THIS frame
            if (!Input.GetKeyDown(shortcut.MainKey)) return false;

            // All required modifiers MUST be held
            foreach (var mod in shortcut.Modifiers)
            {
                if (!Input.GetKey(mod)) return false;
            }

            // We explicitly DON'T check if other keys are held to allow usage while walking/running.
            return true;
        }
    }
}
