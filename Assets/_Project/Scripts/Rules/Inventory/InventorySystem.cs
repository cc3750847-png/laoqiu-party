using LaoqiuParty.GameFlow.Data;

namespace LaoqiuParty.Rules.Inventory
{
    public class InventorySystem
    {
        public void AddItem(PlayerRuntimeState player, string itemId)
        {
            if (player == null || string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            player.inventory.itemIds.Add(itemId);
        }

        public bool RemoveItem(PlayerRuntimeState player, string itemId)
        {
            return player != null
                && !string.IsNullOrWhiteSpace(itemId)
                && player.inventory.itemIds.Remove(itemId);
        }
    }
}
