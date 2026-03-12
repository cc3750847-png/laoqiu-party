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
        private static Font uiFont;

        [SerializeField] private float tileSpacing = 2.5f;
        [SerializeField] private int playerCount = 4;
        [SerializeField] private int maxRounds = 10;
        [SerializeField] private bool useBoardPrototypeBuilder = true;

        private void Start()
        {
            if (FindObjectOfType<GameLoopController>() != null)
            {
                return;
            }

            if (useBoardPrototypeBuilder)
            {
                var prototypeSetup = GetComponent<BoardPrototypeAutoSetup>();
                if (prototypeSetup == null)
                {
                    prototypeSetup = gameObject.AddComponent<BoardPrototypeAutoSetup>();
                }

                prototypeSetup.enabled = true;
                enabled = false;
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

            BuildStageSet(boardRoot.transform);
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
            var tileCount = GetBoardNodeCount();
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

        private int GetBoardNodeCount()
        {
            return 18;
        }

        private List<PlayerPawn> BuildPawns()
        {
            var pawns = new List<PlayerPawn>(playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                var pawnObject = new GameObject($"Pawn_{i}");
                pawnObject.name = $"Pawn_{i}";
                pawnObject.transform.position = new Vector3(0f, 1f, i * 1.25f);
                BuildPawnVisual(pawnObject.transform, i);

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
                var player = new PlayerRuntimeState
                {
                    playerId = i,
                    displayName = $"Player {i + 1}",
                    isAi = i != 0,
                    coins = 2,
                    keys = 0,
                    boardPosition = 0,
                    score = 0
                };

                if (i == 0 || i == 1 || i == 3)
                {
                    player.inventory.AddItem("steal_coin");
                }

                players.Add(player);
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
            if (index == 2 || index == 6 || index == 8 || index == 16)
            {
                return BoardTileType.Reward;
            }

            if (index == 10 || index == 12 || index == 14)
            {
                return BoardTileType.Trap;
            }

            if (index == 5 || index == 9 || index == 15)
            {
                return BoardTileType.Shop;
            }

            if (index == 3 || index == 11)
            {
                return BoardTileType.DirectorEvent;
            }

            return BoardTileType.Normal;
        }

        private void BuildPawnVisual(Transform parent, int index)
        {
            var accent = GetPawnColor(index);

            CreatePawnPart("Base", parent, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.48f, 0.12f, 0.48f), new Color(0.08f, 0.10f, 0.14f));
            CreatePawnPart("Ring", parent, PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.36f, 0.03f, 0.36f), accent);
            CreatePawnPart("Core", parent, PrimitiveType.Cylinder, new Vector3(0f, 0.56f, 0f), new Vector3(0.22f, 0.34f, 0.22f), accent * 0.9f);
            CreatePawnPart("Head", parent, PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), new Vector3(0.34f, 0.34f, 0.34f), accent);
            CreatePawnPart("Beacon", parent, PrimitiveType.Cylinder, new Vector3(0f, 1.38f, 0f), new Vector3(0.08f, 0.10f, 0.08f), Lighten(accent, 0.22f));
            CreatePawnPart("ShoulderLeft", parent, PrimitiveType.Cube, new Vector3(-0.22f, 0.72f, 0f), new Vector3(0.10f, 0.08f, 0.18f), new Color(0.12f, 0.14f, 0.18f));
            CreatePawnPart("ShoulderRight", parent, PrimitiveType.Cube, new Vector3(0.22f, 0.72f, 0f), new Vector3(0.10f, 0.08f, 0.18f), new Color(0.12f, 0.14f, 0.18f));

            var plate = CreatePawnPart("NumberPlate", parent, PrimitiveType.Cube, new Vector3(0f, 0.58f, -0.22f), new Vector3(0.24f, 0.12f, 0.04f), new Color(0.96f, 0.96f, 0.98f));
            plate.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            CreatePawnNumber(parent, index);
        }

        private GameObject CreatePawnPart(
            string name,
            Transform parent,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            var part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            return part;
        }

        private void CreatePawnNumber(Transform parent, int index)
        {
            var labelObject = new GameObject("NumberText");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.58f, -0.255f);
            labelObject.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = (index + 1).ToString();
            textMesh.characterSize = 0.1f;
            textMesh.fontSize = 56;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = new Color(0.05f, 0.06f, 0.08f);
        }

        private Color GetPawnColor(int index)
        {
            return index switch
            {
                0 => new Color(0.92f, 0.36f, 0.34f),
                1 => new Color(0.28f, 0.62f, 1f),
                2 => new Color(1f, 0.84f, 0.30f),
                _ => new Color(0.35f, 0.90f, 0.55f)
            };
        }

        private Color Lighten(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                1f);
        }

        private void BuildStageSet(Transform parent)
        {
            var center = new Vector3(13f, 0f, 0.1f);
            CreateStagePart("ArenaBase", parent, PrimitiveType.Cube, new Vector3(center.x, -1.55f, center.z), new Vector3(36f, 1.8f, 16f), new Color(0.04f, 0.05f, 0.07f));
            CreateStagePart("ArenaDeck", parent, PrimitiveType.Cube, new Vector3(center.x, -0.72f, center.z), new Vector3(33f, 0.16f, 13.4f), new Color(0.08f, 0.10f, 0.14f));
            CreateStagePart("Backdrop", parent, PrimitiveType.Cube, new Vector3(center.x, 3.2f, 8.4f), new Vector3(37f, 7f, 0.35f), new Color(0.06f, 0.08f, 0.12f));
            CreateStagePart("FrontRail", parent, PrimitiveType.Cube, new Vector3(center.x, 0.35f, -6.4f), new Vector3(36f, 0.18f, 0.18f), new Color(0.18f, 0.24f, 0.34f));
            CreateStagePart("BackRail", parent, PrimitiveType.Cube, new Vector3(center.x, 0.35f, 6.8f), new Vector3(36f, 0.18f, 0.18f), new Color(0.18f, 0.24f, 0.34f));
            CreateStagePart("LeftRail", parent, PrimitiveType.Cube, new Vector3(-4.6f, 0.35f, center.z), new Vector3(0.18f, 0.18f, 13.2f), new Color(0.18f, 0.24f, 0.34f));
            CreateStagePart("RightRail", parent, PrimitiveType.Cube, new Vector3(30.6f, 0.35f, center.z), new Vector3(0.18f, 0.18f, 13.2f), new Color(0.18f, 0.24f, 0.34f));
            CreateStagePart("ZonePlateResources", parent, PrimitiveType.Cube, new Vector3(17.6f, -0.60f, -4.2f), new Vector3(8.2f, 0.05f, 4.8f), new Color(0.10f, 0.20f, 0.14f));
            CreateStagePart("ZonePlateRisk", parent, PrimitiveType.Cube, new Vector3(16.4f, -0.60f, 4.3f), new Vector3(10.8f, 0.05f, 4.4f), new Color(0.18f, 0.09f, 0.10f));
            CreateStagePart("ZonePlateMarket", parent, PrimitiveType.Cube, new Vector3(24.1f, -0.60f, 0.2f), new Vector3(6.0f, 0.05f, 4.2f), new Color(0.08f, 0.14f, 0.20f));
            CreateTower(parent, "TowerNW", new Vector3(-3.8f, 1.7f, 6f), new Color(1f, 0.36f, 0.24f));
            CreateTower(parent, "TowerNE", new Vector3(29.8f, 1.7f, 6f), new Color(0.28f, 0.72f, 1f));
            CreateTower(parent, "TowerSW", new Vector3(-3.8f, 1.7f, -5.8f), new Color(1f, 0.82f, 0.28f));
            CreateTower(parent, "TowerSE", new Vector3(29.8f, 1.7f, -5.8f), new Color(0.38f, 0.94f, 0.56f));
        }

        private void PositionCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
            camera.transform.position = new Vector3(13.2f, 16.4f, -16.5f);
            camera.transform.rotation = Quaternion.Euler(38f, -2f, 0f);
        }

        private Vector3 GetTilePosition(int index)
        {
            return index switch
            {
                0 => new Vector3(0f, 0f, -3.4f),
                1 => new Vector3(2.8f, 0f, -3.4f),
                2 => new Vector3(5.6f, 0f, -3.4f),
                3 => new Vector3(8.4f, 0f, -3.4f),
                4 => new Vector3(11.2f, 0f, -4.8f),
                5 => new Vector3(14.2f, 0f, -5.2f),
                6 => new Vector3(17.3f, 0f, -5.2f),
                7 => new Vector3(20.4f, 0f, -4.2f),
                8 => new Vector3(23.4f, 0f, -3.1f),
                9 => new Vector3(26.2f, 0f, -1.0f),
                10 => new Vector3(25.4f, 0f, 2.0f),
                11 => new Vector3(22.8f, 0f, 4.6f),
                12 => new Vector3(19.4f, 0f, 5.4f),
                13 => new Vector3(15.8f, 0f, 5.6f),
                14 => new Vector3(12.1f, 0f, 4.8f),
                15 => new Vector3(8.6f, 0f, 3.1f),
                16 => new Vector3(5.2f, 0f, 1.2f),
                17 => new Vector3(2.2f, 0f, -1.2f),
                _ => new Vector3(index * tileSpacing, 0f, 0f)
            };
        }

        private void ConfigureBoardLinks(List<BoardTile> tiles)
        {
            tiles[0].Configure(tiles[0].TileId, tiles[0].TileType, new List<BoardTile> { tiles[1] });
            tiles[1].Configure(tiles[1].TileId, tiles[1].TileType, new List<BoardTile> { tiles[2] });
            tiles[2].Configure(tiles[2].TileId, tiles[2].TileType, new List<BoardTile> { tiles[3] });
            tiles[3].Configure(tiles[3].TileId, tiles[3].TileType, new List<BoardTile> { tiles[8], tiles[4] });
            tiles[3].ConfigurePaidChoice(true, 2, "付费捷径", "资源路线");
            tiles[3].ConfigureBranchLabels("付费捷径", "资源路线");
            tiles[4].Configure(tiles[4].TileId, tiles[4].TileType, new List<BoardTile> { tiles[5] });
            tiles[5].Configure(tiles[5].TileId, tiles[5].TileType, new List<BoardTile> { tiles[6] });
            tiles[6].Configure(tiles[6].TileId, tiles[6].TileType, new List<BoardTile> { tiles[7] });
            tiles[7].Configure(tiles[7].TileId, tiles[7].TileType, new List<BoardTile> { tiles[8] });
            tiles[8].Configure(tiles[8].TileId, tiles[8].TileType, new List<BoardTile> { tiles[9] });
            tiles[9].Configure(tiles[9].TileId, tiles[9].TileType, new List<BoardTile> { tiles[10] });
            tiles[10].Configure(tiles[10].TileId, tiles[10].TileType, new List<BoardTile> { tiles[11] });
            tiles[11].Configure(tiles[11].TileId, tiles[11].TileType, new List<BoardTile> { tiles[15], tiles[12] });
            tiles[11].ConfigureBranchLabels("商店路线", "危险路线");
            tiles[12].Configure(tiles[12].TileId, tiles[12].TileType, new List<BoardTile> { tiles[13] });
            tiles[13].Configure(tiles[13].TileId, tiles[13].TileType, new List<BoardTile> { tiles[14] });
            tiles[14].Configure(tiles[14].TileId, tiles[14].TileType, new List<BoardTile> { tiles[15] });
            tiles[15].Configure(tiles[15].TileId, tiles[15].TileType, new List<BoardTile> { tiles[16] });
            tiles[16].Configure(tiles[16].TileId, tiles[16].TileType, new List<BoardTile> { tiles[17] });
            tiles[17].Configure(tiles[17].TileId, tiles[17].TileType, new List<BoardTile> { tiles[0] });
            tiles[11].ConfigurePaidChoice(false, 0, string.Empty, string.Empty);
        }

        private void BuildHud(GameLoopController loopController)
        {
            var canvasObject = new GameObject("HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var headerPanel = CreatePanel("HeaderPanel", canvasObject.transform, new Vector2(24f, -24f), new Vector2(320f, 190f), new Color(0.06f, 0.09f, 0.14f, 0.88f));
            var rosterPanel = CreatePanel("RosterPanel", canvasObject.transform, new Vector2(1376f, -24f), new Vector2(500f, 360f), new Color(0.06f, 0.09f, 0.14f, 0.88f));
            var eventPanel = CreatePanel("EventPanel", canvasObject.transform, new Vector2(24f, -610f), new Vector2(960f, 84f), new Color(0.05f, 0.08f, 0.12f, 0.84f));
            var commandPanel = CreatePanel("CommandPanel", canvasObject.transform, new Vector2(430f, -900f), new Vector2(1060f, 132f), new Color(0.08f, 0.12f, 0.18f, 0.96f));
            var bannerBackground = CreatePanel("BannerPanel", canvasObject.transform, new Vector2(416f, -24f), new Vector2(920f, 128f), new Color(0.09f, 0.12f, 0.2f, 0.94f));

            var header = CreateText("HeaderText", headerPanel.transform, new Vector2(22f, -20f), 26, TextAnchor.UpperLeft);
            var players = CreateText("PlayersText", rosterPanel.transform, new Vector2(24f, -20f), 22, TextAnchor.UpperLeft);
            var events = CreateText("EventText", eventPanel.transform, new Vector2(24f, -18f), 18, TextAnchor.UpperLeft);
            var bannerHeadline = CreateText("BannerHeadline", bannerBackground.transform, new Vector2(26f, -18f), 36, TextAnchor.UpperLeft);
            var bannerBody = CreateText("BannerBody", bannerBackground.transform, new Vector2(26f, -70f), 24, TextAnchor.UpperLeft);
            var commandHint = CreateText("CommandHint", commandPanel.transform, new Vector2(28f, -18f), 24, TextAnchor.UpperLeft);
            var rollButton = CreateButton("RollButton", commandPanel.transform, new Vector2(24f, -62f), new Vector2(280f, 52f), "掷骰  [空格]");
            var routeAButton = CreateButton("RouteAButton", commandPanel.transform, new Vector2(322f, -62f), new Vector2(340f, 52f), "选项一  [1]");
            var routeBButton = CreateButton("RouteBButton", commandPanel.transform, new Vector2(680f, -62f), new Vector2(340f, 52f), "选项二  [2]");

            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.sizeDelta = new Vector2(270f, 150f);

            var playersRect = players.rectTransform;
            playersRect.anchorMin = new Vector2(0f, 1f);
            playersRect.anchorMax = new Vector2(0f, 1f);
            playersRect.pivot = new Vector2(0f, 1f);
            playersRect.sizeDelta = new Vector2(450f, 320f);

            var hud = canvasObject.AddComponent<HudController>();
            var eventRect = events.rectTransform;
            eventRect.anchorMin = new Vector2(0f, 1f);
            eventRect.anchorMax = new Vector2(0f, 1f);
            eventRect.pivot = new Vector2(0f, 1f);
            eventRect.sizeDelta = new Vector2(910f, 48f);

            var bannerHeadlineRect = bannerHeadline.rectTransform;
            bannerHeadlineRect.anchorMin = new Vector2(0f, 1f);
            bannerHeadlineRect.anchorMax = new Vector2(0f, 1f);
            bannerHeadlineRect.pivot = new Vector2(0f, 1f);
            bannerHeadlineRect.sizeDelta = new Vector2(860f, 40f);

            var bannerBodyRect = bannerBody.rectTransform;
            bannerBodyRect.anchorMin = new Vector2(0f, 1f);
            bannerBodyRect.anchorMax = new Vector2(0f, 1f);
            bannerBodyRect.pivot = new Vector2(0f, 1f);
            bannerBodyRect.sizeDelta = new Vector2(860f, 46f);

            bannerHeadline.color = new Color(1f, 0.88f, 0.55f, 1f);
            bannerBody.color = new Color(1f, 1f, 1f, 1f);
            events.color = new Color(0.88f, 0.92f, 0.98f, 1f);
            header.color = new Color(0.95f, 0.97f, 1f, 1f);
            players.color = new Color(0.95f, 0.97f, 1f, 1f);
            commandHint.color = new Color(0.98f, 0.98f, 1f, 1f);

            var commandHintRect = commandHint.rectTransform;
            commandHintRect.anchorMin = new Vector2(0f, 1f);
            commandHintRect.anchorMax = new Vector2(0f, 1f);
            commandHintRect.pivot = new Vector2(0f, 1f);
            commandHintRect.sizeDelta = new Vector2(980f, 34f);

            hud.Configure(
                loopController,
                header,
                players,
                events,
                bannerBackground,
                bannerHeadline,
                bannerBody,
                commandHint,
                rollButton,
                routeAButton,
                routeBButton);
        }

        private Text CreateText(string objectName, Transform parent, Vector2 anchoredPosition, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.font = GetUiFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            text.supportRichText = true;

            var outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

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
            image.color = new Color(0.17f, 0.32f, 0.55f, 0.96f);

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
            text.font = GetUiFont();
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.supportRichText = true;

            var outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            buttonObject.SetActive(false);
            return button;
        }

        private Image CreatePanel(
            string objectName,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var panelObject = new GameObject(objectName);
            panelObject.transform.SetParent(parent, false);

            var image = panelObject.AddComponent<Image>();
            image.color = color;

            var outline = panelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.46f, 0.64f, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);

            var rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            return image;
        }

        private void CreateStagePart(
            string name,
            Transform parent,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            var part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private void CreateTower(Transform parent, string name, Vector3 localPosition, Color lightColor)
        {
            var tower = new GameObject(name);
            tower.transform.SetParent(parent, false);
            tower.transform.localPosition = localPosition;

            CreateStagePart("Pole", tower.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.14f, 1.5f, 0.14f), new Color(0.14f, 0.16f, 0.21f));
            CreateStagePart("Lamp", tower.transform, PrimitiveType.Sphere, new Vector3(0f, 1.8f, 0f), new Vector3(0.35f, 0.35f, 0.35f), lightColor);
        }

        private Font GetUiFont()
        {
            if (uiFont != null)
            {
                return uiFont;
            }

            var candidates = new[]
            {
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "SimHei",
                "SimSun",
                "Noto Sans CJK SC",
                "Arial Unicode MS"
            };

            try
            {
                uiFont = Font.CreateDynamicFontFromOSFont(candidates, 24);
            }
            catch
            {
                uiFont = null;
            }

            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return uiFont;
        }

    }
}
