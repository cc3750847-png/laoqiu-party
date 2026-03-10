using System.Linq;
using LaoqiuParty.Agents.Data;
using LaoqiuParty.GameFlow.Data;
using UnityEngine;

namespace LaoqiuParty.Agents.Runtime
{
    public class AgentBrain : MonoBehaviour
    {
        [SerializeField] private int playerId;
        [SerializeField] private AgentPersonalityDefinition personality;

        public int PlayerId => playerId;

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

        private PlayerRuntimeState ChooseTarget(MatchState matchState, PlayerRuntimeState self)
        {
            return matchState.players
                .Where(player => player.playerId != self.playerId)
                .OrderByDescending(player => player.score)
                .ThenByDescending(player => player.coins)
                .FirstOrDefault();
        }
    }
}
