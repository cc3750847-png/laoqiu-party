using LaoqiuParty.GameFlow.Data;

namespace LaoqiuParty.GameFlow.Controllers
{
    public class TurnController
    {
        public PlayerRuntimeState BeginTurn(MatchState matchState)
        {
            matchState.currentPhase = MatchPhase.TurnStart;
            return matchState.GetActivePlayer();
        }

        public void EndTurn(MatchState matchState)
        {
            matchState.AdvanceTurn();
            matchState.currentPhase = matchState.currentRound > matchState.maxRounds
                ? MatchPhase.MatchEnd
                : MatchPhase.TurnStart;
        }
    }
}
