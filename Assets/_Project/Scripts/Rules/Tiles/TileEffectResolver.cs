using LaoqiuParty.Board.Data;
using LaoqiuParty.Board.Runtime;
using LaoqiuParty.GameFlow.Data;
using UnityEngine;

namespace LaoqiuParty.Rules.Tiles
{
    public class TileEffectResolver : MonoBehaviour
    {
        [SerializeField] private int rewardCoins = 3;
        [SerializeField] private int trapCoins = 2;
        [SerializeField] private int shopPrice = 5;
        [SerializeField] private int shopScoreReward = 1;

        public int ShopPrice => shopPrice;
        public int ShopScoreReward => shopScoreReward;

        public bool CanAffordShop(PlayerRuntimeState player)
        {
            return player != null && player.coins >= shopPrice;
        }

        public string Resolve(PlayerRuntimeState player, BoardTile tile, bool? humanBuyDecision = null)
        {
            if (player == null || tile == null)
            {
                return null;
            }

            string message;
            switch (tile.TileType)
            {
                case BoardTileType.Reward:
                    player.coins += rewardCoins;
                    message = $"{player.displayName} landed on Reward and gained {rewardCoins} coins.";
                    break;
                case BoardTileType.Trap:
                    player.coins = Mathf.Max(0, player.coins - trapCoins);
                    message = $"{player.displayName} landed on Trap and lost {trapCoins} coins.";
                    break;
                case BoardTileType.Shop:
                    var shouldBuy = humanBuyDecision ?? player.coins >= shopPrice;
                    if (!CanAffordShop(player))
                    {
                        message = $"{player.displayName} reached Shop but lacked coins.";
                    }
                    else if (shouldBuy)
                    {
                        player.coins -= shopPrice;
                        player.score += shopScoreReward;
                        message = $"{player.displayName} bought {shopScoreReward} score at Shop for {shopPrice} coins.";
                    }
                    else
                    {
                        message = $"{player.displayName} skipped Shop.";
                    }
                    break;
                default:
                    message = $"{player.displayName} landed on {tile.TileType}.";
                    break;
            }

            Debug.Log($"[Tile] {message}");
            return message;
        }
    }
}
