using System.Linq;
using LaoqiuParty.Agents.Data;
using LaoqiuParty.Board.Data;
using LaoqiuParty.Board.Runtime;
using LaoqiuParty.GameFlow.Actions;
using LaoqiuParty.GameFlow.Data;
using UnityEngine;

namespace LaoqiuParty.Agents.Runtime
{
    public class AgentBrain : MonoBehaviour
    {
        public enum AgentArchetype
        {
            Greedy,
            Scorer,
            Cautious,
            Chaotic
        }

        [SerializeField] private int playerId;
        [SerializeField] private AgentPersonalityDefinition personality;
        [SerializeField] private AgentArchetype archetype;

        public int PlayerId => playerId;
        public AgentArchetype Archetype => archetype;

        public void Configure(int id, AgentArchetype type)
        {
            playerId = id;
            archetype = type;
        }

        public void TakeTurn(MatchState matchState)
        {
            if (matchState == null)
            {
                return;
            }

            var self = matchState.players.FirstOrDefault(player => player.playerId == playerId);
            if (self == null)
            {
                return;
            }

            var target = ChooseTarget(matchState, self);
            Debug.Log(
                $"[Agent] {self.displayName} evaluates turn. " +
                $"Target={target?.displayName ?? "None"}, Aggression={personality?.aggression ?? 0f:0.00}");
        }

        public BoardTile ChooseNextTile(MatchState matchState, PlayerRuntimeState self, BoardTile currentTile)
        {
            if (currentTile == null || currentTile.NextTiles.Count == 0)
            {
                return null;
            }

            if (currentTile.NextTiles.Count == 1)
            {
                return currentTile.NextTiles[0];
            }

            BoardTile bestTile = null;
            var bestScore = float.MinValue;
            foreach (var candidate in currentTile.NextTiles)
            {
                var score = ScoreTile(candidate, self);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestTile = candidate;
            }

            return bestTile ?? currentTile.NextTiles[0];
        }

        public GameActionRequest BuildPathChoiceAction(MatchState matchState, PlayerRuntimeState self, BoardTile currentTile)
        {
            if (currentTile == null || currentTile.NextTiles.Count == 0)
            {
                return null;
            }

            var bestIndex = 0;
            var bestScore = float.MinValue;
            for (var i = 0; i < currentTile.NextTiles.Count; i++)
            {
                var score = ScoreTile(currentTile.NextTiles[i], self);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestIndex = i;
            }

            return new GameActionRequest
            {
                actionType = GameActionType.ChoosePath,
                playerId = self.playerId,
                intValue = bestIndex
            };
        }

        public GameActionRequest BuildShopDecisionAction(PlayerRuntimeState self, int shopPrice)
        {
            var wantsToBuy = archetype switch
            {
                AgentArchetype.Scorer => self.coins >= shopPrice,
                AgentArchetype.Greedy => self.coins >= shopPrice + 1,
                AgentArchetype.Cautious => self.coins >= shopPrice + 2,
                AgentArchetype.Chaotic => Random.value > 0.45f && self.coins >= shopPrice,
                _ => self.coins >= shopPrice
            };

            return new GameActionRequest
            {
                actionType = GameActionType.ShopDecision,
                playerId = self.playerId,
                boolValue = wantsToBuy
            };
        }

        public GameActionRequest BuildPaidRouteAction(PlayerRuntimeState self, BoardTile currentTile)
        {
            var canPay = currentTile != null && self != null && self.coins >= currentTile.PaidChoiceCost;
            var wantsPaidRoute = canPay && archetype switch
            {
                AgentArchetype.Scorer => true,
                AgentArchetype.Greedy => currentTile.PaidChoiceCost <= 2,
                AgentArchetype.Cautious => false,
                AgentArchetype.Chaotic => Random.value > 0.35f,
                _ => false
            };

            return new GameActionRequest
            {
                actionType = GameActionType.PaidRouteDecision,
                playerId = self.playerId,
                boolValue = wantsPaidRoute
            };
        }

        public GameActionRequest BuildUseItemAction(MatchState matchState, PlayerRuntimeState self)
        {
            if (self == null || self.inventory == null || !self.inventory.HasItem("steal_coin"))
            {
                return null;
            }

            var shouldUseItem = archetype switch
            {
                AgentArchetype.Greedy => true,
                AgentArchetype.Scorer => matchState != null && self.coins < 5,
                AgentArchetype.Cautious => false,
                AgentArchetype.Chaotic => Random.value > 0.4f,
                _ => false
            };

            return shouldUseItem
                ? new GameActionRequest
                {
                    actionType = GameActionType.UseItem,
                    playerId = self.playerId,
                    boolValue = true
                }
                : null;
        }

        private PlayerRuntimeState ChooseTarget(MatchState matchState, PlayerRuntimeState self)
        {
            return matchState.players
                .Where(player => player.playerId != self.playerId)
                .OrderByDescending(player => player.score)
                .ThenByDescending(player => player.coins)
                .FirstOrDefault();
        }

        private float ScoreTile(BoardTile tile, PlayerRuntimeState self)
        {
            var score = tile.TileType switch
            {
                BoardTileType.Reward => 2f,
                BoardTileType.Shop => 1.5f + (self.coins >= 5 ? 2f : -0.5f),
                BoardTileType.Trap => -2f,
                _ => 0f
            };

            score += archetype switch
            {
                AgentArchetype.Greedy => tile.TileType switch
                {
                    BoardTileType.Reward => 2f,
                    BoardTileType.Shop => 1f,
                    BoardTileType.Trap => -1f,
                    _ => 0f
                },
                AgentArchetype.Scorer => tile.TileType switch
                {
                    BoardTileType.Shop => 3f,
                    BoardTileType.Reward => 0.5f,
                    _ => 0f
                },
                AgentArchetype.Cautious => tile.TileType switch
                {
                    BoardTileType.Trap => -3f,
                    BoardTileType.Reward => 1f,
                    _ => 0.25f
                },
                AgentArchetype.Chaotic => tile.TileType switch
                {
                    BoardTileType.Trap => 2.5f,
                    BoardTileType.DirectorEvent => 2f,
                    _ => Random.Range(-0.5f, 0.75f)
                },
                _ => 0f
            };

            return score;
        }
    }
}
