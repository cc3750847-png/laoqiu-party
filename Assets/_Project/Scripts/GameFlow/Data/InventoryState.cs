using System;
using System.Collections.Generic;

namespace LaoqiuParty.GameFlow.Data
{
    [Serializable]
    public class InventoryState
    {
        public List<string> itemIds = new();

        public bool HasItem(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && itemIds.Contains(itemId);
        }

        public void AddItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            itemIds.Add(itemId);
        }

        public bool RemoveItem(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && itemIds.Remove(itemId);
        }
    }
}
