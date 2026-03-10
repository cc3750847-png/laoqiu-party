using System;

namespace LaoqiuParty.GameFlow.Data
{
    [Serializable]
    public class PlayerRuntimeState
    {
        public int playerId;
        public string displayName = "Player";
        public bool isAi;
        public int coins;
        public int keys;
        public int boardPosition;
        public int score;
        public InventoryState inventory = new();
    }
}
