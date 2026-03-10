using System.Text;
using LaoqiuParty.GameFlow.Controllers;
using LaoqiuParty.GameFlow.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LaoqiuParty.UI.Hud
{
    public class HudController : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoopController;
        [SerializeField] private Text headerText;
        [SerializeField] private Text playersText;
        [SerializeField] private Text eventText;
        [SerializeField] private Button rollButton;
        [SerializeField] private Button choiceButtonA;
        [SerializeField] private Button choiceButtonB;
        [SerializeField] private Text choiceButtonAText;
        [SerializeField] private Text choiceButtonBText;

        public void Configure(
            GameLoopController loopController,
            Text header,
            Text players,
            Text events,
            Button roll,
            Button routeA,
            Button routeB)
        {
            gameLoopController = loopController;
            headerText = header;
            playersText = players;
            eventText = events;
            rollButton = roll;
            choiceButtonA = routeA;
            choiceButtonB = routeB;

            choiceButtonAText = choiceButtonA != null ? choiceButtonA.GetComponentInChildren<Text>() : null;
            choiceButtonBText = choiceButtonB != null ? choiceButtonB.GetComponentInChildren<Text>() : null;

            if (rollButton != null)
            {
                rollButton.onClick.RemoveAllListeners();
                rollButton.onClick.AddListener(() => gameLoopController?.SubmitHumanRoll());
            }

            if (choiceButtonA != null)
            {
                choiceButtonA.onClick.RemoveAllListeners();
                choiceButtonA.onClick.AddListener(() => gameLoopController?.SubmitHumanChoice(0));
            }

            if (choiceButtonB != null)
            {
                choiceButtonB.onClick.RemoveAllListeners();
                choiceButtonB.onClick.AddListener(() => gameLoopController?.SubmitHumanChoice(1));
            }
        }

        private void Update()
        {
            if (gameLoopController == null || gameLoopController.MatchState == null)
            {
                return;
            }

            var matchState = gameLoopController.MatchState;
            var activePlayer = matchState.GetActivePlayer();

            if (headerText != null)
            {
                headerText.text = matchState.currentPhase == MatchPhase.MatchEnd
                    ? BuildMatchEndHeader(matchState)
                    : $"Round {matchState.currentRound}/{matchState.maxRounds}\n" +
                      $"Phase: {matchState.currentPhase}\n" +
                      $"Active: {activePlayer?.displayName ?? "None"}";
            }

            if (playersText != null)
            {
                playersText.text = matchState.currentPhase == MatchPhase.MatchEnd
                    ? BuildFinalRanking(matchState)
                    : BuildLivePlayersText(matchState, activePlayer);
            }

            if (eventText != null)
            {
                eventText.text = $"Latest: {matchState.latestMessage}";
            }

            UpdateInputButtons();
        }

        private string BuildLivePlayersText(MatchState matchState, PlayerRuntimeState activePlayer)
        {
            var builder = new StringBuilder();
            foreach (var player in matchState.players)
            {
                builder.Append(player.displayName);
                builder.Append("  Pos:");
                builder.Append(player.boardPosition);
                builder.Append("  Coins:");
                builder.Append(player.coins);
                builder.Append("  Score:");
                builder.Append(player.score);

                if (activePlayer != null && player.playerId == activePlayer.playerId)
                {
                    builder.Append("  <");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private string BuildMatchEndHeader(MatchState matchState)
        {
            var winner = matchState.finalRanking.Count > 0 ? matchState.finalRanking[0] : null;
            return winner == null
                ? "Match Over"
                : $"Match Over\nWinner: {winner.displayName}\nScore {winner.score}  Coins {winner.coins}";
        }

        private string BuildFinalRanking(MatchState matchState)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < matchState.finalRanking.Count; i++)
            {
                var player = matchState.finalRanking[i];
                builder.Append('#');
                builder.Append(i + 1);
                builder.Append(' ');
                builder.Append(player.displayName);
                builder.Append("  Score:");
                builder.Append(player.score);
                builder.Append("  Coins:");
                builder.Append(player.coins);
                builder.Append("  Pos:");
                builder.Append(player.boardPosition);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private void UpdateInputButtons()
        {
            if (gameLoopController == null)
            {
                return;
            }

            if (rollButton != null)
            {
                rollButton.gameObject.SetActive(gameLoopController.IsWaitingForHumanRoll);
            }

            var showChoice = gameLoopController.IsWaitingForPathChoice || gameLoopController.IsWaitingForShopChoice;
            if (choiceButtonA != null)
            {
                choiceButtonA.gameObject.SetActive(showChoice);
            }

            if (choiceButtonB != null)
            {
                choiceButtonB.gameObject.SetActive(showChoice);
            }

            if (choiceButtonAText != null)
            {
                choiceButtonAText.text = gameLoopController.ChoiceOptionA;
            }

            if (choiceButtonBText != null)
            {
                choiceButtonBText.text = gameLoopController.ChoiceOptionB;
            }
        }
    }
}
