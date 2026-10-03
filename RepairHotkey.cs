using malafein.Valheim.Shared;
using UnityEngine;

namespace ValheimHotKeys
{
    // Equips a build tool that has a repair piece and selects repair in one press.
    // A second press while repair is selected puts the tool away.
    public static class RepairHotkey
    {
        // Set when the hotkey has asked for the tool to be equipped but place mode
        // hasn't started yet. Player.ToggleEquipped queues the equip when the item has
        // an m_equipDuration, so the repair selection has to wait for it to land.
        private static ItemDrop.ItemData _pendingTool;

        /// <summary>Handles a press of the hotkey. Returns true if it acted.</summary>
        public static bool HandlePress(Player player)
        {
            Inventory inventory = player.GetInventory();
            ItemDrop.ItemData tool = FindRepairTool(player, inventory);
            if (tool == null)
            {
                Log.Debug("No build tool with a repair piece found in inventory.");
                return false;
            }

            if (player.IsItemEquiped(tool) && player.InPlaceMode())
            {
                Piece selected = player.GetSelectedPiece();
                if (selected != null && selected.m_repairPiece)
                {
                    _pendingTool = null;
                    player.UseItem(inventory, tool, true);
                }
                else
                {
                    SelectRepair(player, tool.m_shared.m_buildPieces);
                }
                return true;
            }

            // Pressing again while the equip is still queued cancels it in vanilla
            // (QueueEquipAction removes an already-queued action), so the pending
            // selection is dropped by Update once nothing is queued any more.
            player.UseItem(inventory, tool, true);
            _pendingTool = tool;
            Update(player);
            return true;
        }

        /// <summary>Completes a pending repair selection once the tool is in hand.</summary>
        public static void Update(Player player)
        {
            if (_pendingTool == null) return;

            if (player.IsItemEquiped(_pendingTool) && player.InPlaceMode())
            {
                SelectRepair(player, _pendingTool.m_shared.m_buildPieces);
                _pendingTool = null;
            }
            else if (!player.IsItemEquiped(_pendingTool) && !player.IsEquipActionQueued(_pendingTool))
            {
                // Equip was cancelled, or refused outright (ToggleEquipped does nothing
                // mid-attack), or the tool left the inventory.
                _pendingTool = null;
            }
        }

        // Matches by capability rather than prefab name so modded hammers work. An
        // equipped tool wins over one sitting in the inventory.
        private static ItemDrop.ItemData FindRepairTool(Player player, Inventory inventory)
        {
            ItemDrop.ItemData found = null;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (FindRepairPiece(item.m_shared.m_buildPieces) == null) continue;
                if (player.IsItemEquiped(item)) return item;
                if (found == null) found = item;
            }
            return found;
        }

        private static Piece FindRepairPiece(PieceTable table)
        {
            if (table == null) return null;

            foreach (GameObject prefab in table.m_pieces)
            {
                if (prefab == null) continue;
                Piece piece = prefab.GetComponent<Piece>();
                if (piece != null && piece.m_repairPiece) return piece;
            }
            return null;
        }

        // Player.SetSelectedPiece(Piece) switches the build menu to the piece's own
        // category. The repair piece is normally category All, which PieceTable lists
        // in every tab, so select it inside the current tab instead to keep the menu
        // where the player left it. Fall back to the vanilla lookup otherwise.
        private static void SelectRepair(Player player, PieceTable table)
        {
            var piecesInTab = table.GetPiecesInSelectedCategory();
            for (int i = 0; i < piecesInTab.Count; i++)
            {
                if (piecesInTab[i].m_repairPiece)
                {
                    player.SetSelectedPiece(new Vector2Int(i % PieceTable.m_gridWidth, i / PieceTable.m_gridWidth));
                    return;
                }
            }

            Piece repair = FindRepairPiece(table);
            if (repair == null || !player.SetSelectedPiece(repair))
            {
                Log.Warn("Could not select the repair piece in the equipped build tool.");
            }
        }
    }
}
