using LaoqiuParty.GameFlow.Data;

namespace LaoqiuParty.Rules.Economy
{
    public class EconomySystem
    {
        public void AddCoins(PlayerRuntimeState player, int amount)
        {
            if (player == null || amount <= 0)
            {
                return;
            }

            player.coins += amount;
        }

        public bool TrySpendCoins(PlayerRuntimeState player, int amount)
        {
            if (player == null || amount < 0 || player.coins < amount)
            {
                return false;
            }

            player.coins -= amount;
            return true;
        }
    }
}
