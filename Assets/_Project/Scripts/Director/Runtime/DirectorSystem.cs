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
                $"[Director] Round {matchState.currentRound}, Turn {matchState.currentTurnIndex}, " +
                $"Active={activePlayer.displayName}, Leader={context.Leader?.displayName}, Last={context.LastPlace?.displayName}");
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
            if (leader.playerId != lastPlace.playerId && leader.coins >= leaderCoinPenalty)
            {
                leader.coins -= leaderCoinPenalty;
                lastPlace.coins += lastPlaceCoinBoost;
                message =
                    $"Director event: {leader.displayName} is taxed {leaderCoinPenalty} coins. " +
                    $"{lastPlace.displayName} gets {lastPlaceCoinBoost} comeback coins.";
            }
            else
            {
                lastPlace.coins += lastPlaceCoinBoost;
                message = $"Director event: {lastPlace.displayName} gets {lastPlaceCoinBoost} comeback coins.";
            }

            matchState.SetMessage(message);
            Debug.Log($"[DirectorEvent] {message}");
        }
    }
}
