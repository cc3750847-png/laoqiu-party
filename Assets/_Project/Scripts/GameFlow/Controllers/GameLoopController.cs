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
        private BoardTile pendingChoiceTile;

        public MatchState MatchState => matchState;
        public bool IsWaitingForHumanRoll => waitingForHumanRoll;
        public bool IsWaitingForPathChoice => waitingForPathChoice;
        public bool IsWaitingForShopChoice => waitingForShopChoice;
        public string ChoiceOptionA => waitingForShopChoice
            ? $"Buy (-{tileEffectResolver?.ShopPrice ?? 5}, +{tileEffectResolver?.ShopScoreReward ?? 1} score)"
            : pendingChoiceTile != null && pendingChoiceTile.NextTiles.Count > 0
                ? pendingChoiceTile.NextTiles[0].TileType.ToString()
                : string.Empty;
        public string ChoiceOptionB => waitingForShopChoice
            ? "Skip"
            : pendingChoiceTile != null && pendingChoiceTile.NextTiles.Count > 1
                ? pendingChoiceTile.NextTiles[1].TileType.ToString()
                : string.Empty;

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

            if ((waitingForPathChoice || waitingForShopChoice) && Input.GetKeyDown(KeyCode.Alpha1))
            {
                SubmitHumanChoice(0);
            }

            if ((waitingForPathChoice || waitingForShopChoice) && Input.GetKeyDown(KeyCode.Alpha2))
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
            }
        }

        private IEnumerator RunAutomatedTurn(PlayerRuntimeState activePlayer)
        {
            isTurnRunning = true;

            matchState.currentPhase = MatchPhase.DiceRoll;
            yield return new WaitForSeconds(turnDelaySeconds);
            var rollAction = new GameActionRequest
            {
                actionType = GameActionType.Roll,
                playerId = activePlayer.playerId
            };
            var roll = actionExecutor.ResolveRoll(rollAction, diceRoller);

            var brainForTurn = GetBrain(activePlayer.playerId);
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
            var rollMessage = $"{activePlayer.displayName} rolled {roll} and stopped on tile {activePlayer.boardPosition}.";
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
            waitingForHumanRoll = true;
            matchState.SetMessage($"{activePlayer.displayName}, your turn. Click Roll or press Space.");

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
                matchState.SetMessage($"{activePlayer.displayName}, Shop: 1=Buy, 2=Skip.");

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
            var rollMessage = $"{activePlayer.displayName} rolled {roll} and stopped on tile {activePlayer.boardPosition}.";
            matchState.SetMessage(tileMessage ?? rollMessage);
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
                if (pendingChoiceTile != currentTile)
                {
                    pendingChoiceTile = currentTile;
                    waitingForPathChoice = true;
                    matchState.SetMessage($"{activePlayer.displayName}, choose your route (1/2).");
                }

                GameActionRequest humanChoice;
                if (!TryConsumeAction(GameActionType.ChoosePath, activePlayer.playerId, out humanChoice))
                {
                    return null;
                }

                waitingForPathChoice = false;
                pendingChoiceTile = null;
                var humanIndex = actionExecutor.ResolveChoiceIndex(humanChoice, currentTile.NextTiles.Count);
                var humanSelectedTile = currentTile.NextTiles[humanIndex];
                matchState.SetMessage($"{activePlayer.displayName} chose {humanSelectedTile.TileType} route.");
                return humanSelectedTile;
            }

            var aiChoice = brain?.BuildPathChoiceAction(matchState, activePlayer, currentTile);
            var aiIndex = actionExecutor.ResolveChoiceIndex(aiChoice, currentTile.NextTiles.Count);
            var selectedTile = currentTile.NextTiles[aiIndex];
            matchState.SetMessage(
                $"{activePlayer.displayName} chose the {selectedTile.TileType} route " +
                $"({brain?.Archetype.ToString() ?? "Default"} AI).");
            return selectedTile;
        }

        private void FinalizeMatch()
        {
            isTurnRunning = false;
            matchState.FinalizeRanking();
            var winner = matchState.finalRanking.Count > 0 ? matchState.finalRanking[0] : null;
            if (winner != null)
            {
                matchState.SetMessage($"Match over. Winner: {winner.displayName} with Score {winner.score} and Coins {winner.coins}.");
            }
            else
            {
                matchState.SetMessage("Match over.");
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
            pendingChoiceTile = null;
            pendingActions.Clear();
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
    }
}
