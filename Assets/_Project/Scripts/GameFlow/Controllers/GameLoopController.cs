using System.Collections.Generic;
using LaoqiuParty.Agents.Runtime;
using LaoqiuParty.Board.Runtime;
using LaoqiuParty.Director.Runtime;
using LaoqiuParty.GameFlow.Data;
using UnityEngine;

namespace LaoqiuParty.GameFlow.Controllers
{
    public class GameLoopController : MonoBehaviour
    {
        [SerializeField] private int maxRounds = 15;
        [SerializeField] private BoardGraph boardGraph;
        [SerializeField] private DirectorSystem directorSystem;
        [SerializeField] private List<AgentBrain> aiBrains = new();
        [SerializeField] private List<PlayerRuntimeState> startingPlayers = new();

        private readonly TurnController turnController = new();
        private MatchState matchState;

        public MatchState MatchState => matchState;

        private void Awake()
        {
            matchState = new MatchState
            {
                currentPhase = MatchPhase.MatchSetup,
                maxRounds = maxRounds,
                players = new List<PlayerRuntimeState>(startingPlayers)
            };
        }

        private void Start()
        {
            StartMatch();
        }

        public void StartMatch()
        {
            matchState.currentPhase = MatchPhase.TurnStart;
            directorSystem?.Initialize(matchState, boardGraph);
            BeginTurn();
        }

        public void BeginTurn()
        {
            var activePlayer = turnController.BeginTurn(matchState);
            if (activePlayer == null)
            {
                matchState.currentPhase = MatchPhase.MatchEnd;
                return;
            }

            directorSystem?.EvaluateTurnStart(activePlayer);

            if (!activePlayer.isAi)
            {
                return;
            }

            foreach (var brain in aiBrains)
            {
                if (brain != null && brain.PlayerId == activePlayer.playerId)
                {
                    brain.TakeTurn(matchState);
                    break;
                }
            }
        }

        public void EndTurn()
        {
            turnController.EndTurn(matchState);
            if (matchState.currentPhase != MatchPhase.MatchEnd)
            {
                BeginTurn();
            }
        }
    }
}
