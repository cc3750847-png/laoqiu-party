using LaoqiuParty.Board.Runtime;
using LaoqiuParty.GameFlow.Data;
using UnityEngine;

namespace LaoqiuParty.Director.Runtime
{
    public class DirectorSystem : MonoBehaviour
    {
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private int leaderCoinPenalty = 2;
        [SerializeField] private int lastPlaceCoinBoost = 2;

        private MatchState matchState;
        private BoardGraph boardGraph;
        private int lastProcessedRound = -1;

        public void Initialize(MatchState state, BoardGraph graph)
        {
            matchState = state;
            boardGraph = graph;
        }

        public void EvaluateTurnStart(PlayerRuntimeState activePlayer)
        {
            if (matchState == null || activePlayer == null)
            {
                return;
            }

            var context = new DirectorContext(matchState, boardGraph);
            if (matchState.currentTurnIndex == 0 && matchState.currentRound != lastProcessedRound)
            {
                ApplyRoundEvent(context);
                lastProcessedRound = matchState.currentRound;
            }

            if (!enableDebugLogs)
            {
                return;
            }

            Debug.Log(
                $"[Director] 回合 {matchState.currentRound}, 玩家序 {matchState.currentTurnIndex}, " +
                $"当前={activePlayer.displayName}, 领先={context.Leader?.displayName}, 落后={context.LastPlace?.displayName}");
        }

        private void ApplyRoundEvent(DirectorContext context)
        {
            var leader = context.Leader;
            var lastPlace = context.LastPlace;
            if (leader == null || lastPlace == null)
            {
                return;
            }

            string message;
            string headline;
            if (leader.playerId != lastPlace.playerId && leader.coins >= leaderCoinPenalty)
            {
                leader.coins -= leaderCoinPenalty;
                lastPlace.coins += lastPlaceCoinBoost;
                headline = "导演裁定";
                message =
                    $"{leader.displayName} 被系统盯上，支付 {leaderCoinPenalty} 金币。{lastPlace.displayName} 获得 {lastPlaceCoinBoost} 金币补偿。";
            }
            else
            {
                lastPlace.coins += lastPlaceCoinBoost;
                headline = "补偿机制";
                message = $"{lastPlace.displayName} 获得导演补偿的 {lastPlaceCoinBoost} 金币。";
            }

            matchState.SetMessage(message, headline);
            Debug.Log($"[DirectorEvent] {message}");
        }
    }
}
