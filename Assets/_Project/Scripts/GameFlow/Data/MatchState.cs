using System;
using System.Collections.Generic;
using System.Linq;

namespace LaoqiuParty.GameFlow.Data
{
    [Serializable]
    public class MatchState
    {
        public MatchPhase currentPhase = MatchPhase.Boot;
        public int currentRound = 1;
        public int currentTurnIndex;
        public int maxRounds = 15;
        public string latestHeadline = "Match Ready";
        public string latestMessage = "Match ready.";
        public int latestMessageVersion;
        public List<PlayerRuntimeState> players = new();
        public List<PlayerRuntimeState> finalRanking = new();

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

        public void SetMessage(string message, string headline = null)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                latestMessage = message;
                latestHeadline = string.IsNullOrWhiteSpace(headline) ? latestHeadline : headline;
                latestMessageVersion++;
            }
        }

        public void FinalizeRanking()
        {
            finalRanking = players
                .OrderByDescending(player => player.score)
                .ThenByDescending(player => player.coins)
                .ThenByDescending(player => player.boardPosition)
                .ToList();
        }
    }
}
