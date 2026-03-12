using LaoqiuParty.GameFlow.Controllers;
using LaoqiuParty.GameFlow.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LaoqiuParty.UI.Hud
{
    public class HudController : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private GameLoopController gameLoopController;
        [SerializeField] private Text headerText;
        [SerializeField] private Text playersText;
        [SerializeField] private Text eventText;

        [Header("Top Bar")]
        [SerializeField] private Text topRoundValueText;
        [SerializeField] private Text topPhaseValueText;
        [SerializeField] private Text topActiveValueText;

        [Header("Player Cards")]
        [SerializeField] private Image[] playerCardBackgrounds;
        [SerializeField] private Text[] playerCardNameTexts;
        [SerializeField] private Text[] playerCardStatsTexts;
        [SerializeField] private Color playerCardColor = new Color(0.10f, 0.15f, 0.24f, 0.88f);
        [SerializeField] private Color playerCardActiveColor = new Color(0.24f, 0.33f, 0.16f, 0.94f);

        [Header("Banner")]
        [SerializeField] private Image bannerBackground;
        [SerializeField] private Text bannerHeadlineText;
        [SerializeField] private Text bannerBodyText;

        [Header("Command Bar")]
        [SerializeField] private Text commandHintText;
        [SerializeField] private Button rollButton;
        [SerializeField] private Button choiceButtonA;
        [SerializeField] private Button choiceButtonB;
        [SerializeField] private Text choiceButtonAText;
        [SerializeField] private Text choiceButtonBText;

        private int lastSeenMessageVersion = -1;
        private float bannerVisibleUntil;

        public void Configure(
            GameLoopController loopController,
            Text header,
            Text players,
            Text events,
            Image bannerBackgroundImage,
            Text bannerHeadline,
            Text bannerBody,
            Text commandHint,
            Button roll,
            Button routeA,
            Button routeB)
        {
            gameLoopController = loopController;
            headerText = header;
            playersText = players;
            eventText = events;
            bannerBackground = bannerBackgroundImage;
            bannerHeadlineText = bannerHeadline;
            bannerBodyText = bannerBody;
            commandHintText = commandHint;
            rollButton = roll;
            choiceButtonA = routeA;
            choiceButtonB = routeB;

            choiceButtonAText = choiceButtonA != null ? choiceButtonA.GetComponentInChildren<Text>() : null;
            choiceButtonBText = choiceButtonB != null ? choiceButtonB.GetComponentInChildren<Text>() : null;

            BindButtons();
        }

        public void ConfigureAdvanced(
            Text roundValueText,
            Text phaseValueText,
            Text activeValueText,
            Image[] cardBackgrounds,
            Text[] cardNameTexts,
            Text[] cardStatsTexts)
        {
            topRoundValueText = roundValueText;
            topPhaseValueText = phaseValueText;
            topActiveValueText = activeValueText;
            playerCardBackgrounds = cardBackgrounds;
            playerCardNameTexts = cardNameTexts;
            playerCardStatsTexts = cardStatsTexts;
        }

        private void BindButtons()
        {
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

            UpdateTopBar(matchState, activePlayer);
            UpdatePlayerPanel(matchState, activePlayer);
            UpdateEventLog(matchState);
            UpdateBanner(matchState);
            UpdateInputButtons();
        }

        private void UpdateTopBar(MatchState matchState, PlayerRuntimeState activePlayer)
        {
            if (topRoundValueText != null)
            {
                topRoundValueText.text = $"{matchState.currentRound:00}/{matchState.maxRounds:00}";
            }

            if (topPhaseValueText != null)
            {
                topPhaseValueText.text = GetPhaseLabel(matchState.currentPhase);
            }

            if (topActiveValueText != null)
            {
                topActiveValueText.text = activePlayer != null ? activePlayer.displayName : "\u65e0";
            }

            if (headerText != null)
            {
                headerText.text = BuildFallbackHeader(matchState, activePlayer);
            }
        }

        private void UpdatePlayerPanel(MatchState matchState, PlayerRuntimeState activePlayer)
        {
            if (playerCardBackgrounds == null || playerCardBackgrounds.Length == 0)
            {
                if (playersText != null)
                {
                    playersText.text = BuildFallbackPlayers(matchState, activePlayer);
                }

                return;
            }

            for (var i = 0; i < playerCardBackgrounds.Length; i++)
            {
                var hasPlayer = i < matchState.players.Count;
                var card = playerCardBackgrounds[i];
                if (card == null)
                {
                    continue;
                }

                card.gameObject.SetActive(hasPlayer);
                if (!hasPlayer)
                {
                    continue;
                }

                var player = matchState.players[i];
                var isActive = activePlayer != null && player.playerId == activePlayer.playerId;
                card.color = isActive ? playerCardActiveColor : playerCardColor;

                if (playerCardNameTexts != null && i < playerCardNameTexts.Length && playerCardNameTexts[i] != null)
                {
                    playerCardNameTexts[i].text = player.displayName;
                    playerCardNameTexts[i].color = GetPlayerColor(player.playerId);
                }

                if (playerCardStatsTexts != null && i < playerCardStatsTexts.Length && playerCardStatsTexts[i] != null)
                {
                    playerCardStatsTexts[i].text =
                        $"\u4f4d\u7f6e {player.boardPosition:00}   " +
                        $"\u91d1\u5e01 {player.coins:00}   " +
                        $"\u5206\u6570 {player.score:00}   " +
                        $"\u9053\u5177 {player.inventory?.itemIds.Count ?? 0:00}";
                }
            }
        }

        private void UpdateEventLog(MatchState matchState)
        {
            if (eventText != null)
            {
                eventText.text = $"\u6700\u8fd1\u4e8b\u4ef6\uff1a{matchState.latestMessage}";
            }
        }

        private void UpdateInputButtons()
        {
            if (gameLoopController == null)
            {
                return;
            }

            var waitingRoll = gameLoopController.IsWaitingForHumanRoll;
            var waitingChoice =
                gameLoopController.IsWaitingForPathChoice
                || gameLoopController.IsWaitingForShopChoice
                || gameLoopController.IsWaitingForItemChoice
                || gameLoopController.IsWaitingForPaidRouteChoice;

            if (rollButton != null)
            {
                rollButton.gameObject.SetActive(waitingRoll);
            }

            if (choiceButtonA != null)
            {
                choiceButtonA.gameObject.SetActive(waitingChoice);
            }

            if (choiceButtonB != null)
            {
                choiceButtonB.gameObject.SetActive(waitingChoice);
            }

            if (choiceButtonAText != null)
            {
                choiceButtonAText.text = gameLoopController.ChoiceOptionA;
            }

            if (choiceButtonBText != null)
            {
                choiceButtonBText.text = gameLoopController.ChoiceOptionB;
            }

            if (commandHintText != null)
            {
                if (waitingRoll)
                {
                    commandHintText.text = "\u6309 <b>\u7a7a\u683c</b> \u6216\u70b9\u51fb <b>\u63b7\u9ab0</b>";
                }
                else if (gameLoopController.IsWaitingForItemChoice)
                {
                    commandHintText.text = "\u9009\u62e9\uff1a<b>1</b> \u4f7f\u7528\u9053\u5177   <b>2</b> \u4fdd\u7559\u9053\u5177";
                }
                else if (gameLoopController.IsWaitingForPaidRouteChoice)
                {
                    commandHintText.text = "\u9009\u62e9\uff1a<b>1</b> \u4ed8\u8d39\u6362\u9053   <b>2</b> \u8d70\u5b89\u5168\u8def\u7ebf";
                }
                else if (gameLoopController.IsWaitingForShopChoice)
                {
                    commandHintText.text = "\u9009\u62e9\uff1a<b>1</b> \u8d2d\u4e70   <b>2</b> \u8df3\u8fc7";
                }
                else if (gameLoopController.IsWaitingForPathChoice)
                {
                    commandHintText.text = "\u9009\u8def\u7ebf\uff1a<b>1</b> \u5de6\u4fa7   <b>2</b> \u53f3\u4fa7";
                }
                else
                {
                    commandHintText.text = "\u7b49\u5f85\u6d41\u7a0b\u63a8\u8fdb";
                }
            }

            UpdateButtonVisuals();
        }

        private void UpdateButtonVisuals()
        {
            var waitingRoll = gameLoopController != null && gameLoopController.IsWaitingForHumanRoll;
            TintButton(rollButton, waitingRoll ? new Color(0.24f, 0.57f, 0.94f, 0.98f) : new Color(0.17f, 0.32f, 0.55f, 0.82f));

            var optionA = choiceButtonAText != null ? choiceButtonAText.text : string.Empty;
            var optionB = choiceButtonBText != null ? choiceButtonBText.text : string.Empty;

            TintButton(choiceButtonA, ResolveOptionColor(optionA, true));
            TintButton(choiceButtonB, ResolveOptionColor(optionB, false));
        }

        private Color ResolveOptionColor(string optionText, bool isDefault)
        {
            if (string.IsNullOrEmpty(optionText))
            {
                return new Color(0.17f, 0.32f, 0.55f, 0.82f);
            }

            if (optionText.Contains("\u5371\u9669") || optionText.Contains("\u9677\u9631") || optionText.Contains("Trap"))
            {
                return new Color(0.88f, 0.27f, 0.22f, 0.98f);
            }

            if (optionText.Contains("\u5546\u5e97") || optionText.Contains("\u8d2d\u4e70") || optionText.Contains("Shop"))
            {
                return new Color(0.22f, 0.63f, 0.98f, 0.98f);
            }

            if (optionText.Contains("\u6362\u9053") || optionText.Contains("Shortcut"))
            {
                return new Color(0.98f, 0.70f, 0.22f, 0.98f);
            }

            return isDefault
                ? new Color(0.95f, 0.66f, 0.20f, 0.98f)
                : new Color(0.32f, 0.82f, 0.48f, 0.98f);
        }

        private void TintButton(Button button, Color color)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private void UpdateBanner(MatchState matchState)
        {
            if (matchState.latestMessageVersion != lastSeenMessageVersion)
            {
                lastSeenMessageVersion = matchState.latestMessageVersion;
                bannerVisibleUntil = Time.unscaledTime + (matchState.currentPhase == MatchPhase.MatchEnd ? 999f : 2.6f);

                if (bannerHeadlineText != null)
                {
                    bannerHeadlineText.text = string.IsNullOrWhiteSpace(matchState.latestHeadline)
                        ? "\u4e8b\u4ef6\u64ad\u62a5"
                        : matchState.latestHeadline;
                }

                if (bannerBodyText != null)
                {
                    bannerBodyText.text = matchState.latestMessage;
                }
            }

            var alpha = Mathf.Clamp01((bannerVisibleUntil - Time.unscaledTime) / 0.45f);
            if (Time.unscaledTime + 2.15f < bannerVisibleUntil)
            {
                alpha = 1f;
            }

            SetGraphicAlpha(bannerBackground, alpha * 0.92f);
            SetGraphicAlpha(bannerHeadlineText, alpha);
            SetGraphicAlpha(bannerBodyText, alpha);
        }

        private void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private string BuildFallbackHeader(MatchState matchState, PlayerRuntimeState activePlayer)
        {
            return
                $"<size=18><color=#8D99AE>\u56de\u5408</color></size>\n" +
                $"<size=42><b>{matchState.currentRound:00}</b></size><size=22><color=#8D99AE> / {matchState.maxRounds:00}</color></size>\n" +
                $"<size=18><color=#8D99AE>\u9636\u6bb5</color></size>  <size=22><b>{GetPhaseLabel(matchState.currentPhase)}</b></size>\n" +
                $"<size=18><color=#8D99AE>\u5f53\u524d\u73a9\u5bb6</color></size>  <size=24><b>{activePlayer?.displayName ?? "\u65e0"}</b></size>";
        }

        private string BuildFallbackPlayers(MatchState matchState, PlayerRuntimeState activePlayer)
        {
            var result = "<size=20><color=#8D99AE>\u5f53\u524d\u6392\u884c</color></size>\n\n";
            foreach (var player in matchState.players)
            {
                var colorHex = GetPlayerColorHex(player.playerId);
                var activeMarker = activePlayer != null && player.playerId == activePlayer.playerId
                    ? "  <color=#FFD166>\u884c\u52a8\u4e2d</color>"
                    : string.Empty;
                result +=
                    $"<size=24><b><color={colorHex}>{player.displayName}</color></b></size>{activeMarker}\n" +
                    $"<size=18><color=#E9EEF5>\u4f4d\u7f6e</color> <b>{player.boardPosition:00}</b>   " +
                    $"<color=#E9EEF5>\u91d1\u5e01</color> <b>{player.coins:00}</b>   " +
                    $"<color=#E9EEF5>\u5206\u6570</color> <b>{player.score:00}</b>   " +
                    $"<color=#E9EEF5>\u9053\u5177</color> <b>{player.inventory?.itemIds.Count ?? 0:00}</b></size>\n\n";
            }

            return result;
        }

        private Color GetPlayerColor(int playerId)
        {
            return playerId switch
            {
                0 => new Color(1f, 0.42f, 0.40f),
                1 => new Color(0.30f, 0.62f, 1f),
                2 => new Color(1f, 0.83f, 0.28f),
                _ => new Color(0.34f, 0.88f, 0.56f)
            };
        }

        private string GetPlayerColorHex(int playerId)
        {
            return playerId switch
            {
                0 => "#FF6B6B",
                1 => "#4D9DFF",
                2 => "#FFD166",
                _ => "#53D68A"
            };
        }

        private string GetPhaseLabel(MatchPhase phase)
        {
            return phase switch
            {
                MatchPhase.MatchSetup => "\u51c6\u5907",
                MatchPhase.TurnStart => "\u56de\u5408\u5f00\u59cb",
                MatchPhase.DiceRoll => "\u63b7\u9ab0",
                MatchPhase.Move => "\u79fb\u52a8",
                MatchPhase.TileResolve => "\u683c\u5b50\u7ed3\u7b97",
                MatchPhase.EventResolve => "\u4e8b\u4ef6\u7ed3\u7b97",
                MatchPhase.MinigamePrep => "\u5c0f\u6e38\u620f\u51c6\u5907",
                MatchPhase.MinigamePlay => "\u5c0f\u6e38\u620f",
                MatchPhase.Reward => "\u5956\u52b1",
                MatchPhase.RoundEnd => "\u56de\u5408\u7ed3\u675f",
                MatchPhase.MatchEnd => "\u7ed3\u675f",
                _ => phase.ToString()
            };
        }
    }
}

