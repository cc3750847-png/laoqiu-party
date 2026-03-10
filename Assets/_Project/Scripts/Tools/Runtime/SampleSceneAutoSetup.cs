using System.Collections.Generic;
using LaoqiuParty.Agents.Runtime;
using LaoqiuParty.Board.Data;
using LaoqiuParty.Board.Movement;
using LaoqiuParty.Board.Runtime;
using LaoqiuParty.Director.Runtime;
using LaoqiuParty.GameFlow.Controllers;
using LaoqiuParty.GameFlow.Data;
using LaoqiuParty.Rules.Dice;
using LaoqiuParty.Rules.Tiles;
using LaoqiuParty.UI.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace LaoqiuParty.Tools.Runtime
{
    public class SampleSceneAutoSetup : MonoBehaviour
    {
        [SerializeField] private int tileCount = 12;
        [SerializeField] private float tileSpacing = 2.5f;
        [SerializeField] private int playerCount = 4;
        [SerializeField] private int maxRounds = 8;

        private void Start()
        {
            if (FindObjectOfType<GameLoopController>() != null)
            {
                return;
            }

            BuildSampleScene();
        }

        private void BuildSampleScene()
        {
            var gameRoot = new GameObject("GameRoot");
            var boardRoot = new GameObject("BoardRoot");

            var boardGraph = gameRoot.AddComponent<BoardGraph>();
            var movementSystem = gameRoot.AddComponent<BoardMovementSystem>();
            var diceRoller = gameRoot.AddComponent<DiceRoller>();
            var director = gameRoot.AddComponent<DirectorSystem>();
            var tileResolver = gameRoot.AddComponent<TileEffectResolver>();
            var loopController = gameRoot.AddComponent<GameLoopController>();
            var brains = BuildBrains(gameRoot);

            movementSystem.Configure(boardGraph);

            var tiles = BuildTiles(boardRoot.transform);
            boardGraph.SetTiles(tiles);

            var pawns = BuildPawns();
            var players = BuildPlayers();

            PositionCamera();
            BuildHud(loopController);
            loopController.Configure(
                boardGraph,
                movementSystem,
                director,
                diceRoller,
                tileResolver,
                pawns,
                brains,
                players,
                false,
                maxRounds);
            loopController.StartMatch();
        }

        private List<BoardTile> BuildTiles(Transform parent)
        {
            var tiles = new List<BoardTile>(tileCount);
            for (var i = 0; i < tileCount; i++)
            {
                var tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObject.name = $"Tile_{i:00}";
                tileObject.transform.SetParent(parent);
                tileObject.transform.position = GetTilePosition(i);
                tileObject.transform.localScale = new Vector3(1.5f, 0.25f, 1.5f);

                var tile = tileObject.AddComponent<BoardTile>();
                tile.Configure($"tile_{i:00}", PickTileType(i));
                tiles.Add(tile);
            }

            ConfigureBoardLinks(tiles);

            return tiles;
        }

        private List<PlayerPawn> BuildPawns()
        {
            var pawns = new List<PlayerPawn>(playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                var pawnObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                pawnObject.name = $"Pawn_{i}";
                pawnObject.transform.position = new Vector3(0f, 1f, i * 1.25f);
                pawnObject.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
                ApplyPawnColor(pawnObject, i);

                var motor = pawnObject.AddComponent<PawnMotor>();
                var pawn = pawnObject.AddComponent<PlayerPawn>();
                pawn.Configure(i, motor);
                pawns.Add(pawn);
            }

            return pawns;
        }

        private List<PlayerRuntimeState> BuildPlayers()
        {
            var players = new List<PlayerRuntimeState>(playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                players.Add(new PlayerRuntimeState
                {
                    playerId = i,
                    displayName = $"Player {i + 1}",
                    isAi = i != 0,
                    coins = 0,
                    keys = 0,
                    boardPosition = 0,
                    score = 0
                });
            }

            return players;
        }

        private List<AgentBrain> BuildBrains(GameObject gameRoot)
        {
            var archetypes = new[]
            {
                AgentBrain.AgentArchetype.Greedy,
                AgentBrain.AgentArchetype.Scorer,
                AgentBrain.AgentArchetype.Cautious,
                AgentBrain.AgentArchetype.Chaotic
            };

            var brains = new List<AgentBrain>(playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                var brain = gameRoot.AddComponent<AgentBrain>();
                brain.Configure(i, archetypes[i % archetypes.Length]);
                brains.Add(brain);
            }

            return brains;
        }

        private BoardTileType PickTileType(int index)
        {
            if (index == 2 || index == 4)
            {
                return BoardTileType.Reward;
            }

            if (index == 8 || index == 9)
            {
                return BoardTileType.Trap;
            }

            if (index == 10 || index == 6)
            {
                return BoardTileType.Shop;
            }

            if (index == 3)
            {
                return BoardTileType.DirectorEvent;
            }

            return BoardTileType.Normal;
        }

        private void ApplyPawnColor(GameObject pawnObject, int index)
        {
            var renderer = pawnObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.material.color = index switch
            {
                0 => new Color(0.9f, 0.3f, 0.3f),
                1 => new Color(0.25f, 0.55f, 0.95f),
                2 => new Color(0.95f, 0.8f, 0.25f),
                _ => new Color(0.35f, 0.85f, 0.45f)
            };
        }

        private void PositionCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.transform.position = new Vector3(8f, 14f, -12f);
            camera.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        }

        private Vector3 GetTilePosition(int index)
        {
            return index switch
            {
                0 => new Vector3(0f, 0f, 0f),
                1 => new Vector3(tileSpacing, 0f, 0f),
                2 => new Vector3(tileSpacing * 2f, 0f, 0f),
                3 => new Vector3(tileSpacing * 3f, 0f, 0f),
                4 => new Vector3(tileSpacing * 4f, 0f, -1.6f),
                5 => new Vector3(tileSpacing * 5f, 0f, -1.6f),
                6 => new Vector3(tileSpacing * 6f, 0f, -1.6f),
                7 => new Vector3(tileSpacing * 7f, 0f, 0f),
                8 => new Vector3(tileSpacing * 4f, 0f, 1.8f),
                9 => new Vector3(tileSpacing * 5.5f, 0f, 1.8f),
                10 => new Vector3(tileSpacing * 8f, 0f, 0f),
                11 => new Vector3(tileSpacing * 9f, 0f, 0f),
                _ => new Vector3(index * tileSpacing, 0f, 0f)
            };
        }

        private void ConfigureBoardLinks(List<BoardTile> tiles)
        {
            tiles[0].Configure(tiles[0].TileId, tiles[0].TileType, new List<BoardTile> { tiles[1] });
            tiles[1].Configure(tiles[1].TileId, tiles[1].TileType, new List<BoardTile> { tiles[2] });
            tiles[2].Configure(tiles[2].TileId, tiles[2].TileType, new List<BoardTile> { tiles[3] });
            tiles[3].Configure(tiles[3].TileId, tiles[3].TileType, new List<BoardTile> { tiles[4], tiles[8] });
            tiles[4].Configure(tiles[4].TileId, tiles[4].TileType, new List<BoardTile> { tiles[5] });
            tiles[5].Configure(tiles[5].TileId, tiles[5].TileType, new List<BoardTile> { tiles[6] });
            tiles[6].Configure(tiles[6].TileId, tiles[6].TileType, new List<BoardTile> { tiles[7] });
            tiles[7].Configure(tiles[7].TileId, tiles[7].TileType, new List<BoardTile> { tiles[10] });
            tiles[8].Configure(tiles[8].TileId, tiles[8].TileType, new List<BoardTile> { tiles[9] });
            tiles[9].Configure(tiles[9].TileId, tiles[9].TileType, new List<BoardTile> { tiles[10] });
            tiles[10].Configure(tiles[10].TileId, tiles[10].TileType, new List<BoardTile> { tiles[11] });
            tiles[11].Configure(tiles[11].TileId, tiles[11].TileType, new List<BoardTile> { tiles[0] });
        }

        private void BuildHud(GameLoopController loopController)
        {
            var canvasObject = new GameObject("HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var header = CreateText("HeaderText", canvasObject.transform, new Vector2(20f, -20f), 26, TextAnchor.UpperLeft);
            var players = CreateText("PlayersText", canvasObject.transform, new Vector2(20f, -120f), 22, TextAnchor.UpperLeft);
            var events = CreateText("EventText", canvasObject.transform, new Vector2(20f, -380f), 20, TextAnchor.UpperLeft);
            var rollButton = CreateButton("RollButton", canvasObject.transform, new Vector2(20f, -460f), new Vector2(220f, 70f), "Roll (Space)");
            var routeAButton = CreateButton("RouteAButton", canvasObject.transform, new Vector2(260f, -460f), new Vector2(320f, 70f), "Route A (1)");
            var routeBButton = CreateButton("RouteBButton", canvasObject.transform, new Vector2(600f, -460f), new Vector2(320f, 70f), "Route B (2)");

            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.sizeDelta = new Vector2(520f, 100f);

            var playersRect = players.rectTransform;
            playersRect.anchorMin = new Vector2(0f, 1f);
            playersRect.anchorMax = new Vector2(0f, 1f);
            playersRect.pivot = new Vector2(0f, 1f);
            playersRect.sizeDelta = new Vector2(700f, 300f);

            var hud = canvasObject.AddComponent<HudController>();
            var eventRect = events.rectTransform;
            eventRect.anchorMin = new Vector2(0f, 1f);
            eventRect.anchorMax = new Vector2(0f, 1f);
            eventRect.pivot = new Vector2(0f, 1f);
            eventRect.sizeDelta = new Vector2(1100f, 80f);

            hud.Configure(loopController, header, players, events, rollButton, routeAButton, routeBButton);
        }

        private Text CreateText(string objectName, Transform parent, Vector2 anchoredPosition, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;

            var rectTransform = text.rectTransform;
            rectTransform.anchoredPosition = anchoredPosition;

            return text;
        }

        private Button CreateButton(
            string objectName,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            string label)
        {
            var buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.28f, 0.48f, 0.95f);

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            button.colors = colors;

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;

            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            buttonObject.SetActive(false);
            return button;
        }
    }
}
