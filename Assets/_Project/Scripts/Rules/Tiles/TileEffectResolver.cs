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
                    message = $"{player.displayName} 落在奖励格，获得 {rewardCoins} 金币。";
                    break;
                case BoardTileType.Trap:
                    player.coins = Mathf.Max(0, player.coins - trapCoins);
                    message = $"{player.displayName} 落在陷阱格，失去 {trapCoins} 金币。";
                    break;
                case BoardTileType.Shop:
                    var shouldBuy = humanBuyDecision ?? player.coins >= shopPrice;
                    if (!CanAffordShop(player))
                    {
                        message = $"{player.displayName} 到了商店，但金币不够。";
                    }
                    else if (shouldBuy)
                    {
                        player.coins -= shopPrice;
                        player.score += shopScoreReward;
                        message = $"{player.displayName} 在商店花了 {shopPrice} 金币，购买了 {shopScoreReward} 分。";
                    }
                    else
                    {
                        message = $"{player.displayName} 跳过了商店。";
                    }
                    break;
                default:
                    message = $"{player.displayName} 落在普通格。";
                    break;
            }

            Debug.Log($"[Tile] {message}");
            return message;
        }
    }
}
