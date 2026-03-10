using LaoqiuParty.Board.Runtime;
using LaoqiuParty.GameFlow.Data;
using UnityEngine;

namespace LaoqiuParty.Director.Runtime
{
    public class DirectorSystem : MonoBehaviour
    {
        [SerializeField] private bool enableDebugLogs = true;

        private MatchState matchState;
        private BoardGraph boardGraph;

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
            if (!enableDebugLogs)
            {
                return;
            }

            Debug.Log(
                $"[Director] Round {matchState.currentRound}, Turn {matchState.currentTurnIndex}, " +
                $"Active={activePlayer.displayName}, Leader={context.Leader?.displayName}, Last={context.LastPlace?.displayName}");
        }
    }
}
