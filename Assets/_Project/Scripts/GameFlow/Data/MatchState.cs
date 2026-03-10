using System;
using System.Collections.Generic;

namespace LaoqiuParty.GameFlow.Data
{
    [Serializable]
    public class MatchState
    {
        public MatchPhase currentPhase = MatchPhase.Boot;
        public int currentRound = 1;
        public int currentTurnIndex;
        public int maxRounds = 15;
        public List<PlayerRuntimeState> players = new();

        public PlayerRuntimeState GetActivePlayer()
        {
            if (players.Count == 0 || currentTurnIndex < 0 || currentTurnIndex >= players.Count)
            {
                return null;
            }

            return players[currentTurnIndex];
        }

        public void AdvanceTurn()
        {
            if (players.Count == 0)
            {
                return;
            }

            currentTurnIndex++;
            if (currentTurnIndex < players.Count)
            {
                return;
            }

            currentTurnIndex = 0;
            currentRound++;
        }
    }
}
