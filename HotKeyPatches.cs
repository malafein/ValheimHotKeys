using HarmonyLib;
using malafein.Valheim.Shared;

namespace ValheimHotKeys
{
    [HarmonyPatch]
    public static class HotKeyPatches
    {
        // Valheim 1.0 drives HUD visibility through Hud.m_userHidden, which Hud.Update
        // feeds into SetVisible(); that moves m_rootObject off-screen instead of
        // deactivating it. Flipping the flag from a prefix lets the vanilla Update apply
        // it the same frame and keeps this hotkey in sync with the game's own Ctrl+F3
        // toggle and with Hud.IsVisible().
        [HarmonyPatch(typeof(Hud), "Update")]
        [HarmonyPrefix]
        public static void Hud_Update_Prefix(Hud __instance)
        {
            if (Keybinds.IsDown(Plugin.ToggleHUDConfig.Value))
            {
                __instance.m_userHidden = !__instance.m_userHidden;
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        [HarmonyPostfix]
        public static void Player_Update_Postfix(Player __instance)
        {
            // Only handle input if we are the local player.
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            RepairHotkey.Update(__instance);

            if (!Keybinds.CanTakeInput(__instance))
            {
                return;
            }

            if (Keybinds.IsDown(Plugin.RepairHammerConfig.Value, ModifierMatch.AtLeast) && RepairHotkey.HandlePress(__instance))
            {
                return; // Only use one thing per frame
            }

            for (int i = 0; i < Plugin.HotbarConfigs.Length; i++)
            {
                if (Keybinds.IsDown(Plugin.HotbarConfigs[i].Value, ModifierMatch.AtLeast))
                {
                    __instance.UseHotbarItem(i + 1);
                    return; // Only use one thing per frame
                }
            }

            // Custom Item Hotkeys
            for (int i = 0; i < Plugin.CustomItemBindings.Count; i++)
            {
                var binding = Plugin.CustomItemBindings[i].Value;
                if (Keybinds.IsDown(binding.Shortcut, ModifierMatch.AtLeast))
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
                        Log.Debug($"No item matching '{itemName}' found in inventory.");
                    }
                }
            }
        }
    }
}
