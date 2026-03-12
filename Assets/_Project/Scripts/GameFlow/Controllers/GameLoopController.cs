using System.Collections;
using System.Collections.Generic;
using LaoqiuParty.Agents.Runtime;
using LaoqiuParty.Board.Data;
using LaoqiuParty.Board.Runtime;
using LaoqiuParty.Director.Runtime;
using LaoqiuParty.GameFlow.Actions;
using LaoqiuParty.GameFlow.Data;
using LaoqiuParty.Rules.Dice;
using LaoqiuParty.Rules.Tiles;
using UnityEngine;

namespace LaoqiuParty.GameFlow.Controllers
{
    public class GameLoopController : MonoBehaviour
    {
        [SerializeField] private int maxRounds = 15;
        [SerializeField] private BoardGraph boardGraph;
        [SerializeField] private BoardMovementSystem boardMovementSystem;
        [SerializeField] private DirectorSystem directorSystem;
        [SerializeField] private DiceRoller diceRoller;
        [SerializeField] private TileEffectResolver tileEffectResolver;
        [SerializeField] private List<PlayerPawn> playerPawns = new();
        [SerializeField] private List<AgentBrain> aiBrains = new();
        [SerializeField] private List<PlayerRuntimeState> startingPlayers = new();
        [SerializeField] private float turnDelaySeconds = 0.5f;
        [SerializeField] private bool autoPlayAllPlayers = true;

        private readonly TurnController turnController = new();
        private readonly ActionExecutor actionExecutor = new();
        private readonly List<GameActionRequest> pendingActions = new();

        private MatchState matchState;
        private bool isTurnRunning;
        private bool waitingForHumanRoll;
        private bool waitingForPathChoice;
        private bool waitingForShopChoice;
        private bool waitingForItemChoice;
        private bool waitingForPaidRouteChoice;
        private BoardTile pendingChoiceTile;
        private readonly List<BoardTile> highlightedRouteTiles = new();
        private string choiceOptionALabel = string.Empty;
        private string choiceOptionBLabel = string.Empty;

        public MatchState MatchState => matchState;
        public bool IsWaitingForHumanRoll => waitingForHumanRoll;
        public bool IsWaitingForPathChoice => waitingForPathChoice;
        public bool IsWaitingForShopChoice => waitingForShopChoice;
        public bool IsWaitingForItemChoice => waitingForItemChoice;
        public bool IsWaitingForPaidRouteChoice => waitingForPaidRouteChoice;
        public string ChoiceOptionA => choiceOptionALabel;
        public string ChoiceOptionB => choiceOptionBLabel;

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
            if (startingPlayers.Count > 0)
            {
                StartMatch();
            }
        }

        private void Update()
        {
            if (waitingForHumanRoll && Input.GetKeyDown(KeyCode.Space))
            {
                SubmitHumanRoll();
            }

            if ((waitingForPathChoice || waitingForShopChoice || waitingForItemChoice || waitingForPaidRouteChoice)
                && Input.GetKeyDown(KeyCode.Alpha1))
            {
                SubmitHumanChoice(0);
            }

            if ((waitingForPathChoice || waitingForShopChoice || waitingForItemChoice || waitingForPaidRouteChoice)
                && Input.GetKeyDown(KeyCode.Alpha2))
            {
                SubmitHumanChoice(1);
            }
        }

        public void Configure(
            BoardGraph graph,
            BoardMovementSystem movementSystem,
            DirectorSystem director,
            DiceRoller roller,
            TileEffectResolver tileResolver,
            List<PlayerPawn> pawns,
            List<AgentBrain> brains,
            List<PlayerRuntimeState> players,
            bool autoPlayPlayers = true,
            int rounds = 15)
        {
            boardGraph = graph;
            boardMovementSystem = movementSystem;
            directorSystem = director;
            diceRoller = roller;
            tileEffectResolver = tileResolver;
            playerPawns = pawns ?? new List<PlayerPawn>();
            aiBrains = brains ?? new List<AgentBrain>();
            startingPlayers = players ?? new List<PlayerRuntimeState>();
            autoPlayAllPlayers = autoPlayPlayers;
            maxRounds = rounds;

            matchState = new MatchState
            {
                currentPhase = MatchPhase.MatchSetup,
                maxRounds = maxRounds,
                players = new List<PlayerRuntimeState>(startingPlayers)
            };
        }

        public void StartMatch()
        {
            if (matchState == null)
            {
                matchState = new MatchState
                {
                    currentPhase = MatchPhase.MatchSetup,
                    maxRounds = maxRounds,
                    players = new List<PlayerRuntimeState>(startingPlayers)
                };
            }

            matchState.currentPhase = MatchPhase.TurnStart;
            directorSystem?.Initialize(matchState, boardGraph);
            SnapPawnsToBoard();
            BeginTurn();
        }

        public void BeginTurn()
        {
            if (isTurnRunning)
            {
                return;
            }

            ResetInputState();
            var activePlayer = turnController.BeginTurn(matchState);
            if (activePlayer == null)
            {
                matchState.currentPhase = MatchPhase.MatchEnd;
                return;
            }

            directorSystem?.EvaluateTurnStart(activePlayer);

            if (!activePlayer.isAi && !autoPlayAllPlayers)
            {
                StartCoroutine(RunHumanTurn(activePlayer));
                return;
            }

            StartCoroutine(RunAutomatedTurn(activePlayer));
        }

        public void EndTurn()
        {
            turnController.EndTurn(matchState);
            if (matchState.currentPhase == MatchPhase.MatchEnd)
            {
                FinalizeMatch();
                return;
            }

            BeginTurn();
        }

        public void SubmitHumanRoll()
        {
            var activePlayer = matchState?.GetActivePlayer();
            if (!waitingForHumanRoll || activePlayer == null)
            {
                return;
            }

            SubmitAction(new GameActionRequest
            {
                actionType = GameActionType.Roll,
                playerId = activePlayer.playerId
            });
        }

        public void SubmitHumanChoice(int optionIndex)
        {
            var activePlayer = matchState?.GetActivePlayer();
            if (activePlayer == null)
            {
                return;
            }

            if (waitingForPathChoice)
            {
                SubmitAction(new GameActionRequest
                {
                    actionType = GameActionType.ChoosePath,
                    playerId = activePlayer.playerId,
                    intValue = optionIndex
                });
                return;
            }

            if (waitingForShopChoice)
            {
                SubmitAction(new GameActionRequest
                {
                    actionType = GameActionType.ShopDecision,
                    playerId = activePlayer.playerId,
                    boolValue = optionIndex == 0
                });
                return;
            }

            if (waitingForItemChoice)
            {
                SubmitAction(new GameActionRequest
                {
                    actionType = GameActionType.UseItem,
                    playerId = activePlayer.playerId,
                    boolValue = optionIndex == 0
                });
                return;
            }

            if (waitingForPaidRouteChoice)
            {
                SubmitAction(new GameActionRequest
                {
                    actionType = GameActionType.PaidRouteDecision,
                    playerId = activePlayer.playerId,
                    boolValue = optionIndex == 0
                });
            }
        }

        private IEnumerator RunAutomatedTurn(PlayerRuntimeState activePlayer)
        {
            isTurnRunning = true;

            matchState.currentPhase = MatchPhase.DiceRoll;
            yield return new WaitForSeconds(turnDelaySeconds);
            var brainForTurn = GetBrain(activePlayer.playerId);
            TryResolveAutomatedItem(activePlayer, brainForTurn);

            var rollAction = new GameActionRequest
            {
                actionType = GameActionType.Roll,
                playerId = activePlayer.playerId
            };
            var roll = actionExecutor.ResolveRoll(rollAction, diceRoller);
            brainForTurn?.TakeTurn(matchState);

            matchState.currentPhase = MatchPhase.Move;
            var pawn = GetPawn(activePlayer.playerId);
            if (boardMovementSystem != null && pawn != null)
            {
                yield return boardMovementSystem.MovePlayer(
                    activePlayer,
                    pawn,
                    roll,
                    currentTile => SelectNextTile(activePlayer, brainForTurn, currentTile));
            }

            matchState.currentPhase = MatchPhase.TileResolve;
            var landedTile = boardGraph != null ? boardGraph.GetTile(activePlayer.boardPosition) : null;
            var tileMessage = ResolveTileOutcome(activePlayer, landedTile, brainForTurn);
            var rollMessage = $"{activePlayer.displayName} 掷出了 {roll} 点，停在了 {activePlayer.boardPosition} 号格。";
            matchState.SetMessage(tileMessage ?? rollMessage);
            Debug.Log($"[GameLoop] {rollMessage}");
            yield return new WaitForSeconds(turnDelaySeconds);

            isTurnRunning = false;
            EndTurn();
        }

        private IEnumerator RunHumanTurn(PlayerRuntimeState activePlayer)
        {
            isTurnRunning = true;
            matchState.currentPhase = MatchPhase.DiceRoll;
            var hadStealCoinAtTurnStart = activePlayer?.inventory != null && activePlayer.inventory.HasItem("steal_coin");
            yield return TryResolveHumanItem(activePlayer);
            waitingForHumanRoll = true;
            if (hadStealCoinAtTurnStart && (activePlayer?.inventory == null || !activePlayer.inventory.HasItem("steal_coin")))
            {
                matchState.SetMessage($"{matchState.latestMessage} 按空格或点击掷骰继续。", matchState.latestHeadline);
            }
            else
            {
                matchState.SetMessage($"{activePlayer.displayName}，轮到你了。点击掷骰或按空格。", "你的回合");
            }

            GameActionRequest rollAction;
            while (!TryConsumeAction(GameActionType.Roll, activePlayer.playerId, out rollAction))
            {
                yield return null;
            }

            waitingForHumanRoll = false;
            var roll = actionExecutor.ResolveRoll(rollAction, diceRoller);

            matchState.currentPhase = MatchPhase.Move;
            var pawn = GetPawn(activePlayer.playerId);
            if (boardMovementSystem != null && pawn != null)
            {
                yield return boardMovementSystem.MovePlayer(
                    activePlayer,
                    pawn,
                    roll,
                    currentTile => SelectNextTile(activePlayer, null, currentTile));
            }

            matchState.currentPhase = MatchPhase.TileResolve;
            var landedTile = boardGraph != null ? boardGraph.GetTile(activePlayer.boardPosition) : null;
            string tileMessage;
            if (landedTile != null
                && landedTile.TileType == BoardTileType.Shop
                && tileEffectResolver != null
                && tileEffectResolver.CanAffordShop(activePlayer))
            {
                waitingForShopChoice = true;
                choiceOptionALabel = $"购买（-{tileEffectResolver.ShopPrice} 金币，+{tileEffectResolver.ShopScoreReward} 分）";
                choiceOptionBLabel = "跳过";
                matchState.SetMessage($"{activePlayer.displayName} 到达商店：1 购买，2 跳过。", "商店选择");

                GameActionRequest shopAction;
                while (!TryConsumeAction(GameActionType.ShopDecision, activePlayer.playerId, out shopAction))
                {
                    yield return null;
                }

                waitingForShopChoice = false;
                var buy = actionExecutor.ResolveShopDecision(shopAction, false);
                tileMessage = tileEffectResolver.Resolve(activePlayer, landedTile, buy);
            }
            else
            {
                tileMessage = tileEffectResolver?.Resolve(activePlayer, landedTile, null);
            }
            var rollMessage = $"{activePlayer.displayName} 掷出了 {roll} 点，停在了 {activePlayer.boardPosition} 号格。";
            matchState.SetMessage(tileMessage ?? rollMessage, "回合结果");
            Debug.Log($"[GameLoop] {rollMessage}");
            yield return new WaitForSeconds(turnDelaySeconds);

            isTurnRunning = false;
            EndTurn();
        }

        private string ResolveTileOutcome(PlayerRuntimeState activePlayer, BoardTile landedTile, AgentBrain brainForTurn)
        {
            if (tileEffectResolver == null)
            {
                return null;
            }

            if (landedTile != null
                && landedTile.TileType == BoardTileType.Shop
                && tileEffectResolver.CanAffordShop(activePlayer))
            {
                var shopAction = brainForTurn?.BuildShopDecisionAction(activePlayer, tileEffectResolver.ShopPrice);
                var buy = actionExecutor.ResolveShopDecision(shopAction, true);
                return tileEffectResolver.Resolve(activePlayer, landedTile, buy);
            }

            return tileEffectResolver.Resolve(activePlayer, landedTile, null);
        }

        private BoardTile SelectNextTile(PlayerRuntimeState activePlayer, AgentBrain brain, BoardTile currentTile)
        {
            if (currentTile == null || currentTile.NextTiles.Count <= 1)
            {
                return currentTile != null && currentTile.NextTiles.Count == 1
                    ? currentTile.NextTiles[0]
                    : null;
            }

            if (!activePlayer.isAi && !autoPlayAllPlayers)
            {
                if (currentTile.RequiresPaidChoice)
                {
                    if (pendingChoiceTile != currentTile)
                    {
                        pendingChoiceTile = currentTile;
                        waitingForPaidRouteChoice = true;
                        choiceOptionALabel = $"{currentTile.PaidChoiceLabel} (-{currentTile.PaidChoiceCost})";
                        choiceOptionBLabel = currentTile.DefaultChoiceLabel;
                        SetRouteHighlights(currentTile, 0);
                        matchState.SetMessage(
                            $"{activePlayer.displayName} 可以花 {currentTile.PaidChoiceCost} 金币走捷径，或者走普通路线。",
                            "付费捷径");
                    }

                    GameActionRequest paidChoice;
                    if (!TryConsumeAction(GameActionType.PaidRouteDecision, activePlayer.playerId, out paidChoice))
                    {
                        return null;
                    }

                    waitingForPaidRouteChoice = false;
                    pendingChoiceTile = null;
                    var usePaidRoute = actionExecutor.ResolveBooleanDecision(paidChoice, false)
                        && activePlayer.coins >= currentTile.PaidChoiceCost;
                    SetRouteHighlights(currentTile, usePaidRoute ? 0 : Mathf.Min(1, currentTile.NextTiles.Count - 1));

                    if (usePaidRoute)
                    {
                        activePlayer.coins -= currentTile.PaidChoiceCost;
                        matchState.SetMessage(
                            $"{activePlayer.displayName} 花了 {currentTile.PaidChoiceCost} 金币，走了{currentTile.PaidChoiceLabel}。",
                            "已走捷径");
                        return currentTile.NextTiles[0];
                    }

                    matchState.SetMessage(
                        $"{activePlayer.displayName} 保留金币，走了{currentTile.DefaultChoiceLabel}。",
                        "普通路线");
                    return currentTile.NextTiles[Mathf.Min(1, currentTile.NextTiles.Count - 1)];
                }

                if (pendingChoiceTile != currentTile)
                {
                    pendingChoiceTile = currentTile;
                    waitingForPathChoice = true;
                    choiceOptionALabel = pendingChoiceTile.NextTiles.Count > 0
                        ? GetBranchOptionLabel(pendingChoiceTile, 0)
                        : string.Empty;
                    choiceOptionBLabel = pendingChoiceTile.NextTiles.Count > 1
                        ? GetBranchOptionLabel(pendingChoiceTile, 1)
                        : string.Empty;
                    SetRouteHighlights(currentTile, -1);
                    matchState.SetMessage($"{activePlayer.displayName} 请选择路线：1 / 2。", "选择路线");
                }

                GameActionRequest humanChoice;
                if (!TryConsumeAction(GameActionType.ChoosePath, activePlayer.playerId, out humanChoice))
                {
                    return null;
                }

                waitingForPathChoice = false;
                pendingChoiceTile = null;
                var humanIndex = actionExecutor.ResolveChoiceIndex(humanChoice, currentTile.NextTiles.Count);
                SetRouteHighlights(currentTile, humanIndex);
                var humanSelectedTile = currentTile.NextTiles[humanIndex];
                matchState.SetMessage($"{activePlayer.displayName} 选择了{GetBranchOptionLabel(currentTile, humanIndex)}。", "路线已锁定");
                return humanSelectedTile;
            }

            if (currentTile.RequiresPaidChoice)
            {
                var paidAction = brain?.BuildPaidRouteAction(activePlayer, currentTile);
                var usePaidLane = actionExecutor.ResolveBooleanDecision(paidAction, false)
                    && activePlayer.coins >= currentTile.PaidChoiceCost;
                SetRouteHighlights(currentTile, usePaidLane ? 0 : Mathf.Min(1, currentTile.NextTiles.Count - 1));

                if (usePaidLane)
                {
                    activePlayer.coins -= currentTile.PaidChoiceCost;
                    matchState.SetMessage(
                        $"{activePlayer.displayName} 花了 {currentTile.PaidChoiceCost} 金币，走了{currentTile.PaidChoiceLabel}。",
                        "AI 走捷径");
                    return currentTile.NextTiles[0];
                }

                matchState.SetMessage(
                    $"{activePlayer.displayName} 没付费，走了{currentTile.DefaultChoiceLabel}。",
                    "AI 选路线");
                return currentTile.NextTiles[Mathf.Min(1, currentTile.NextTiles.Count - 1)];
            }

            var aiChoice = brain?.BuildPathChoiceAction(matchState, activePlayer, currentTile);
            var aiIndex = actionExecutor.ResolveChoiceIndex(aiChoice, currentTile.NextTiles.Count);
            SetRouteHighlights(currentTile, aiIndex);
            var selectedTile = currentTile.NextTiles[aiIndex];
            matchState.SetMessage(
                $"{activePlayer.displayName} 选择了{GetBranchOptionLabel(currentTile, aiIndex)}。",
                "AI 决策");
            return selectedTile;
        }

        private void FinalizeMatch()
        {
            isTurnRunning = false;
            matchState.FinalizeRanking();
            var winner = matchState.finalRanking.Count > 0 ? matchState.finalRanking[0] : null;
            if (winner != null)
            {
                matchState.SetMessage(
                    $"对局结束。赢家是 {winner.displayName}，分数 {winner.score}，金币 {winner.coins}。",
                    "最终结果");
            }
            else
            {
                matchState.SetMessage("对局结束。", "最终结果");
            }

            Debug.Log($"[MatchEnd] {matchState.latestMessage}");
        }

        private void SubmitAction(GameActionRequest actionRequest)
        {
            if (actionRequest != null)
            {
                pendingActions.Add(actionRequest);
            }
        }

        private bool TryConsumeAction(GameActionType actionType, int playerId, out GameActionRequest actionRequest)
        {
            for (var i = 0; i < pendingActions.Count; i++)
            {
                var candidate = pendingActions[i];
                if (candidate.actionType != actionType || candidate.playerId != playerId)
                {
                    continue;
                }

                actionRequest = candidate;
                pendingActions.RemoveAt(i);
                return true;
            }

            actionRequest = null;
            return false;
        }

        private void ResetInputState()
        {
            waitingForHumanRoll = false;
            waitingForPathChoice = false;
            waitingForShopChoice = false;
            waitingForItemChoice = false;
            waitingForPaidRouteChoice = false;
            pendingChoiceTile = null;
            choiceOptionALabel = string.Empty;
            choiceOptionBLabel = string.Empty;
            ClearRouteHighlights();
            pendingActions.Clear();
        }

        private IEnumerator TryResolveHumanItem(PlayerRuntimeState activePlayer)
        {
            if (activePlayer?.inventory == null || !activePlayer.inventory.HasItem("steal_coin"))
            {
                yield break;
            }

            waitingForItemChoice = true;
            choiceOptionALabel = "使用偷金币";
            choiceOptionALabel = "使用偷金币";
            choiceOptionBLabel = "先留着";
            matchState.SetMessage($"{activePlayer.displayName} 现在要使用偷金币吗？（1/2）", "道具可用");

            GameActionRequest itemAction;
            while (!TryConsumeAction(GameActionType.UseItem, activePlayer.playerId, out itemAction))
            {
                yield return null;
            }

            waitingForItemChoice = false;
            if (actionExecutor.ResolveBooleanDecision(itemAction, false))
            {
                ExecuteStealCoin(activePlayer);
                yield return new WaitForSeconds(turnDelaySeconds * 0.8f);
            }
        }

        private void TryResolveAutomatedItem(PlayerRuntimeState activePlayer, AgentBrain brainForTurn)
        {
            if (activePlayer?.inventory == null || brainForTurn == null)
            {
                return;
            }

            var itemAction = brainForTurn.BuildUseItemAction(matchState, activePlayer);
            if (actionExecutor.ResolveBooleanDecision(itemAction, false))
            {
                ExecuteStealCoin(activePlayer);
            }
        }

        private void ExecuteStealCoin(PlayerRuntimeState user)
        {
            if (user?.inventory == null || !user.inventory.RemoveItem("steal_coin"))
            {
                return;
            }

            var target = FindStealTarget(user.playerId);
            if (target == null)
            {
                matchState.SetMessage($"{user.displayName} 使用了偷金币，但没人有金币可偷。", "道具落空");
                return;
            }

            var stolenCoins = Mathf.Min(2, target.coins);
            target.coins -= stolenCoins;
            user.coins += stolenCoins;
            matchState.SetMessage($"{user.displayName} 从 {target.displayName} 身上偷了 {stolenCoins} 金币。", "已使用道具");
        }

        private PlayerRuntimeState FindStealTarget(int userPlayerId)
        {
            PlayerRuntimeState bestTarget = null;
            foreach (var player in matchState.players)
            {
                if (player.playerId == userPlayerId || player.coins <= 0)
                {
                    continue;
                }

                if (bestTarget == null
                    || player.coins > bestTarget.coins
                    || (player.coins == bestTarget.coins && player.score > bestTarget.score))
                {
                    bestTarget = player;
                }
            }

            return bestTarget;
        }

        private PlayerPawn GetPawn(int playerId)
        {
            foreach (var pawn in playerPawns)
            {
                if (pawn != null && pawn.PlayerId == playerId)
                {
                    return pawn;
                }
            }

            return null;
        }

        private AgentBrain GetBrain(int playerId)
        {
            foreach (var brain in aiBrains)
            {
                if (brain != null && brain.PlayerId == playerId)
                {
                    return brain;
                }
            }

            return null;
        }

        private void SnapPawnsToBoard()
        {
            if (boardMovementSystem == null)
            {
                return;
            }

            foreach (var player in matchState.players)
            {
                var pawn = GetPawn(player.playerId);
                if (pawn != null)
                {
                    boardMovementSystem.SnapPlayerToBoard(player, pawn);
                }
            }
        }

        private void SetRouteHighlights(BoardTile currentTile, int selectedIndex)
        {
            ClearRouteHighlights();
            if (currentTile == null)
            {
                return;
            }

            currentTile.SetRouteHighlight(true, false);
            highlightedRouteTiles.Add(currentTile);

            for (var i = 0; i < currentTile.NextTiles.Count; i++)
            {
                var nextTile = currentTile.NextTiles[i];
                if (nextTile == null)
                {
                    continue;
                }

                var highlight = selectedIndex < 0 || i == selectedIndex;
                var dim = selectedIndex >= 0 && i != selectedIndex;
                nextTile.SetRouteHighlight(highlight, dim);
                highlightedRouteTiles.Add(nextTile);
            }
        }

        private void ClearRouteHighlights()
        {
            foreach (var tile in highlightedRouteTiles)
            {
                tile?.ClearRouteHighlight();
            }

            highlightedRouteTiles.Clear();
        }

        private string GetBranchOptionLabel(BoardTile tile, int optionIndex)
        {
            if (tile == null || optionIndex < 0 || optionIndex >= tile.NextTiles.Count)
            {
                return string.Empty;
            }

            if (optionIndex == 0 && !string.IsNullOrWhiteSpace(tile.BranchOptionALabel))
            {
                return tile.BranchOptionALabel;
            }

            if (optionIndex == 1 && !string.IsNullOrWhiteSpace(tile.BranchOptionBLabel))
            {
                return tile.BranchOptionBLabel;
            }

            return tile.NextTiles[optionIndex].TileType.ToString();
        }
    }
}
