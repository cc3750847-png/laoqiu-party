using System.Collections.Generic;
using System.Linq;
using LaoqiuParty.Board.Runtime;
using LaoqiuParty.GameFlow.Data;

namespace LaoqiuParty.Director.Runtime
{
    public class DirectorContext
    {
        public MatchState MatchState { get; }
        public BoardGraph BoardGraph { get; }
        public PlayerRuntimeState Leader { get; }
        public PlayerRuntimeState LastPlace { get; }

        public DirectorContext(MatchState matchState, BoardGraph boardGraph)
        {
            MatchState = matchState;
            BoardGraph = boardGraph;

            var orderedPlayers = matchState?.players?
                .OrderByDescending(player => player.score)
                .ThenByDescending(player => player.coins)
                .ToList() ?? new List<PlayerRuntimeState>();

            Leader = orderedPlayers.FirstOrDefault();
            LastPlace = orderedPlayers.LastOrDefault();
        }
    }
}
