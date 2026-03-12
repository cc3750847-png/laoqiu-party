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
    public class BoardPrototypeAutoSetup : MonoBehaviour
    {
        private static Font uiFont;
        private static Sprite uiSprite;

        [SerializeField] private BoardLayoutDefinition layoutDefinition;
        [SerializeField] private int playerCount = 4;
        [SerializeField] private int maxRounds = 10;
        [SerializeField] private bool autoStartMatch = true;

        private void Start()
        {
            if (FindObjectOfType<GameLoopController>() != null)
            {
                return;
            }

            BuildPrototypeScene();
        }

        private void BuildPrototypeScene()
        {
            var nodes = GetLayoutNodes();
            if (nodes.Count == 0)
            {
                Debug.LogError("[BoardPrototype] Empty layout.");
                return;
            }

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

            var tiles = BuildTiles(boardRoot.transform, nodes);
            boardGraph.SetTiles(tiles);

            BuildConnections(boardRoot.transform, nodes);
            BuildBranchMarkers(boardRoot.transform, nodes);
            BuildStageSet(boardRoot.transform, nodes);

            var pawns = BuildPawns();
            var players = BuildPlayers();

            PositionCamera(nodes);
            BuildHud(loopController);

            var pawnHighlightPresenter = gameRoot.AddComponent<ActivePawnHighlightPresenter>();
            pawnHighlightPresenter.Configure(loopController, pawns);

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

            if (autoStartMatch)
            {
                loopController.StartMatch();
            }
        }

        private List<BoardNodeDefinition> GetLayoutNodes()
        {
            if (layoutDefinition != null && layoutDefinition.nodes != null && layoutDefinition.nodes.Count > 0)
            {
                return layoutDefinition.nodes;
            }

            return BuildDefaultLayoutNodes();
        }

        private List<BoardTile> BuildTiles(Transform parent, List<BoardNodeDefinition> nodes)
        {
            var tiles = new List<BoardTile>(nodes.Count);
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObject.name = $"Tile_{i:00}";
                tileObject.transform.SetParent(parent);
                tileObject.transform.position = node.position;
                tileObject.transform.localScale = new Vector3(1.5f, 0.25f, 1.5f);

                var tile = tileObject.AddComponent<BoardTile>();
                tile.Configure(string.IsNullOrWhiteSpace(node.nodeId) ? $"tile_{i:00}" : node.nodeId, node.tileType);
                tiles.Add(tile);
            }

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var links = new List<BoardTile>();
                if (node.nextIndices != null)
                {
                    foreach (var nextIndex in node.nextIndices)
                    {
                        if (nextIndex >= 0 && nextIndex < tiles.Count)
                        {
                            links.Add(tiles[nextIndex]);
                        }
                    }
                }

                if (links.Count == 0)
                {
                    links.Add(tiles[(i + 1) % tiles.Count]);
                }

                tiles[i].Configure(tiles[i].TileId, tiles[i].TileType, links);
                tiles[i].ConfigurePaidChoice(
                    node.requiresPaidChoice,
                    node.paidChoiceCost,
                    node.paidChoiceLabel,
                    node.defaultChoiceLabel);
                tiles[i].ConfigureBranchLabels(node.branchOptionALabel, node.branchOptionBLabel);
            }

            return tiles;
        }

        private void BuildConnections(Transform parent, List<BoardNodeDefinition> nodes)
        {
            var root = new GameObject("Connections");
            root.transform.SetParent(parent, false);

            var created = new HashSet<string>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var from = nodes[i].position;
                if (nodes[i].nextIndices == null)
                {
                    continue;
                }

                foreach (var nextIndex in nodes[i].nextIndices)
                {
                    if (nextIndex < 0 || nextIndex >= nodes.Count)
                    {
                        continue;
                    }

                    var a = Mathf.Min(i, nextIndex);
                    var b = Mathf.Max(i, nextIndex);
                    var key = $"{a}-{b}";
                    if (!created.Add(key))
                    {
                        continue;
                    }

                    var to = nodes[nextIndex].position;
                    var direction = to - from;
                    var length = direction.magnitude - 1.05f;
                    if (length <= 0.35f)
                    {
                        continue;
                    }

                    var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    segment.name = $"Link_{i:00}_{nextIndex:00}";
                    segment.transform.SetParent(root.transform, false);
                    segment.transform.position = (from + to) * 0.5f + new Vector3(0f, 0.04f, 0f);
                    segment.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    segment.transform.localScale = new Vector3(0.34f, 0.07f, length);

                    var renderer = segment.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = new Color(0.15f, 0.20f, 0.30f);
                    }
                }
            }
        }

        private void BuildBranchMarkers(Transform parent, List<BoardNodeDefinition> nodes)
        {
            var root = new GameObject("BranchHints");
            root.transform.SetParent(parent, false);

            for (var i = 0; i < nodes.Count; i++)
            {
                var nextCount = nodes[i].nextIndices != null ? nodes[i].nextIndices.Count : 0;
                if (nextCount < 2)
                {
                    continue;
                }

                var marker = new GameObject($"Branch_{i:00}");
                marker.transform.SetParent(root.transform, false);
                marker.transform.position = nodes[i].position + new Vector3(0f, 0.46f, 0f);

                CreatePrimitivePart(
                    marker.transform,
                    "Stem",
                    PrimitiveType.Cylinder,
                    Vector3.zero,
                    new Vector3(0.08f, 0.20f, 0.08f),
                    new Color(0.95f, 0.66f, 0.20f));

                CreatePrimitivePart(
                    marker.transform,
                    "Head",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 0.32f, 0f),
                    new Vector3(0.22f, 0.22f, 0.22f),
                    new Color(1f, 0.80f, 0.26f));
            }
        }

        private List<PlayerPawn> BuildPawns()
        {
            var pawns = new List<PlayerPawn>(playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                var pawnObject = new GameObject($"Pawn_{i}");
                pawnObject.transform.position = new Vector3(0f, 1f, i * 1.2f);
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
                    displayName = $"\u73a9\u5bb6 {i + 1}",
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

        private void BuildStageSet(Transform parent, List<BoardNodeDefinition> nodes)
        {
            var bounds = GetBoardBounds(nodes);
            var center = bounds.center;
            var size = bounds.size;
            var width = Mathf.Max(28f, size.x + 9f);
            var depth = Mathf.Max(16f, size.z + 9f);

            CreateStagePart("ArenaBase", parent, PrimitiveType.Cube, new Vector3(center.x, -1.55f, center.z), new Vector3(width, 1.8f, depth), new Color(0.04f, 0.05f, 0.07f));
            CreateStagePart("ArenaDeck", parent, PrimitiveType.Cube, new Vector3(center.x, -0.72f, center.z), new Vector3(width - 2.6f, 0.16f, depth - 1.8f), new Color(0.08f, 0.10f, 0.14f));
            CreateStagePart("Backdrop", parent, PrimitiveType.Cube, new Vector3(center.x, 2.9f, center.z + depth * 0.5f + 1.3f), new Vector3(width + 1.2f, 6.2f, 0.30f), new Color(0.06f, 0.08f, 0.12f));

            CreateStagePart("ResourceZone", parent, PrimitiveType.Cube, new Vector3(center.x + width * 0.15f, -0.60f, center.z - depth * 0.26f), new Vector3(width * 0.25f, 0.05f, depth * 0.26f), new Color(0.09f, 0.18f, 0.13f));
            CreateStagePart("RiskZone", parent, PrimitiveType.Cube, new Vector3(center.x + width * 0.12f, -0.60f, center.z + depth * 0.30f), new Vector3(width * 0.27f, 0.05f, depth * 0.22f), new Color(0.19f, 0.09f, 0.10f));
            CreateStagePart("ShopZone", parent, PrimitiveType.Cube, new Vector3(center.x + width * 0.30f, -0.60f, center.z), new Vector3(width * 0.16f, 0.05f, depth * 0.24f), new Color(0.08f, 0.14f, 0.20f));
        }

        private void PositionCamera(List<BoardNodeDefinition> nodes)
        {
            var camera = FindObjectOfType<Camera>();
            if (camera == null)
            {
                return;
            }

            var bounds = GetBoardBounds(nodes);
            var center = bounds.center;
            var span = Mathf.Max(bounds.size.x, bounds.size.z) + 6.5f;

            camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
            camera.transform.position = center + new Vector3(0f, span * 0.68f, -span * 0.88f);
            camera.transform.rotation = Quaternion.Euler(36f, -1.5f, 0f);
        }

        private Bounds GetBoardBounds(List<BoardNodeDefinition> nodes)
        {
            var first = nodes[0].position;
            var min = first;
            var max = first;
            for (var i = 1; i < nodes.Count; i++)
            {
                min = Vector3.Min(min, nodes[i].position);
                max = Vector3.Max(max, nodes[i].position);
            }

            var center = (min + max) * 0.5f;
            var size = new Vector3(max.x - min.x, 0f, max.z - min.z);
            return new Bounds(center, size);
        }

        private void BuildPawnVisual(Transform parent, int index)
        {
            var accent = GetPawnColor(index);
            CreatePrimitivePart(parent, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.48f, 0.12f, 0.48f), new Color(0.08f, 0.10f, 0.14f));
            CreatePrimitivePart(parent, "Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.36f, 0.03f, 0.36f), accent);
            CreatePrimitivePart(parent, "Core", PrimitiveType.Cylinder, new Vector3(0f, 0.56f, 0f), new Vector3(0.22f, 0.34f, 0.22f), accent * 0.9f);
            CreatePrimitivePart(parent, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), new Vector3(0.34f, 0.34f, 0.34f), accent);
            CreatePrimitivePart(parent, "ActiveIndicator", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.66f, 0.015f, 0.66f), new Color(0.14f, 0.16f, 0.20f, 0.85f));
        }

        private void BuildHud(GameLoopController loopController)
        {
            var canvasObject = new GameObject("HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var safe = 28f;
            var panelColor = new Color(0.07f, 0.10f, 0.16f, 0.86f);
            var panelColorStrong = new Color(0.08f, 0.12f, 0.18f, 0.93f);

            var topBar = CreatePanel("TopStatusBar", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -safe), new Vector2(1120f, 90f), panelColorStrong);
            var rightPanel = CreatePanel("RightPlayerPanel", canvasObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-safe, -safe), new Vector2(430f, 500f), panelColor);
            var logPanel = CreatePanel("EventLogPanel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(safe, safe + 146f), new Vector2(720f, 120f), panelColor);
            var commandPanel = CreatePanel("CommandPanel", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, safe), new Vector2(980f, 132f), panelColorStrong);
            var bannerPanel = CreatePanel("BannerPanel", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -safe - 102f), new Vector2(980f, 90f), new Color(0.10f, 0.13f, 0.22f, 0.92f));

            var roundLabel = CreateText("RoundLabel", topBar.transform, new Vector2(26f, -18f), new Vector2(90f, 24f), 18, TextAnchor.UpperLeft);
            roundLabel.text = "\u56de\u5408";
            var phaseLabel = CreateText("PhaseLabel", topBar.transform, new Vector2(360f, -18f), new Vector2(90f, 24f), 18, TextAnchor.UpperLeft);
            phaseLabel.text = "\u9636\u6bb5";
            var activeLabel = CreateText("ActiveLabel", topBar.transform, new Vector2(690f, -18f), new Vector2(130f, 24f), 18, TextAnchor.UpperLeft);
            activeLabel.text = "\u5f53\u524d\u73a9\u5bb6";

            var topRoundValue = CreateText("RoundValue", topBar.transform, new Vector2(26f, -44f), new Vector2(240f, 36f), 32, TextAnchor.UpperLeft);
            var topPhaseValue = CreateText("PhaseValue", topBar.transform, new Vector2(360f, -44f), new Vector2(300f, 36f), 30, TextAnchor.UpperLeft);
            var topActiveValue = CreateText("ActiveValue", topBar.transform, new Vector2(690f, -44f), new Vector2(380f, 36f), 30, TextAnchor.UpperLeft);

            var playersTitle = CreateText("PlayersTitle", rightPanel.transform, new Vector2(24f, -18f), new Vector2(380f, 30f), 22, TextAnchor.UpperLeft);
            playersTitle.text = "\u73a9\u5bb6\u4fe1\u606f";

            var cardBackgrounds = new Image[playerCount];
            var cardNameTexts = new Text[playerCount];
            var cardStatsTexts = new Text[playerCount];

            for (var i = 0; i < playerCount; i++)
            {
                var y = -58f - i * 106f;
                var card = CreatePanel($"PlayerCard_{i}", rightPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, y), new Vector2(392f, 92f), new Color(0.10f, 0.15f, 0.24f, 0.88f));
                cardBackgrounds[i] = card;

                var nameText = CreateText($"PlayerCardName_{i}", card.transform, new Vector2(14f, -10f), new Vector2(360f, 30f), 24, TextAnchor.UpperLeft);
                var statsText = CreateText($"PlayerCardStats_{i}", card.transform, new Vector2(14f, -44f), new Vector2(360f, 32f), 18, TextAnchor.UpperLeft);
                cardNameTexts[i] = nameText;
                cardStatsTexts[i] = statsText;
            }

            var eventText = CreateText("EventText", logPanel.transform, new Vector2(20f, -18f), new Vector2(670f, 80f), 22, TextAnchor.UpperLeft);
            var commandHintText = CreateText("CommandHint", commandPanel.transform, new Vector2(24f, -16f), new Vector2(920f, 28f), 24, TextAnchor.UpperLeft);

            var rollButton = CreateButton("RollButton", commandPanel.transform, new Vector2(20f, -66f), new Vector2(260f, 54f), "\u63b7\u9ab0  [\u7a7a\u683c]");
            var choiceAButton = CreateButton("ChoiceAButton", commandPanel.transform, new Vector2(300f, -66f), new Vector2(320f, 54f), "\u9009\u9879\u4e00  [1]");
            var choiceBButton = CreateButton("ChoiceBButton", commandPanel.transform, new Vector2(640f, -66f), new Vector2(320f, 54f), "\u9009\u9879\u4e8c  [2]");

            var bannerHeadline = CreateText("BannerHeadline", bannerPanel.transform, new Vector2(22f, -14f), new Vector2(930f, 30f), 28, TextAnchor.UpperLeft);
            var bannerBody = CreateText("BannerBody", bannerPanel.transform, new Vector2(22f, -46f), new Vector2(930f, 30f), 20, TextAnchor.UpperLeft);
            bannerHeadline.color = new Color(1f, 0.86f, 0.55f, 1f);

            var hud = canvasObject.AddComponent<HudController>();
            hud.Configure(
                loopController,
                null,
                null,
                eventText,
                bannerPanel,
                bannerHeadline,
                bannerBody,
                commandHintText,
                rollButton,
                choiceAButton,
                choiceBButton);
            hud.ConfigureAdvanced(
                topRoundValue,
                topPhaseValue,
                topActiveValue,
                cardBackgrounds,
                cardNameTexts,
                cardStatsTexts);
        }

        private Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = GetUiFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0.95f, 0.97f, 1f, 1f);
            text.supportRichText = true;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        private Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.sprite = GetUiSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.18f, 0.32f, 0.56f, 0.98f);

            var button = go.AddComponent<Button>();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = CreateText("Label", go.transform, new Vector2(0f, 0f), Vector2.zero, 20, TextAnchor.MiddleCenter);
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.text = label;

            go.SetActive(false);
            return button;
        }

        private Image CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = GetUiSprite();
            image.type = Image.Type.Sliced;
            image.color = color;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return image;
        }

        private void CreateStagePart(string name, Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
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

        private GameObject CreatePrimitivePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
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

        private Color GetPawnColor(int index)
        {
            return index switch
            {
                0 => new Color(1f, 0.42f, 0.40f),
                1 => new Color(0.30f, 0.62f, 1f),
                2 => new Color(1f, 0.83f, 0.28f),
                _ => new Color(0.34f, 0.88f, 0.56f)
            };
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

        private Sprite GetUiSprite()
        {
            if (uiSprite != null)
            {
                return uiSprite;
            }

            uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            return uiSprite;
        }

        private List<BoardNodeDefinition> BuildDefaultLayoutNodes()
        {
            var nodes = new List<BoardNodeDefinition>();
            AddNode(nodes, "N0", BoardTileType.Normal, new Vector3(0f, 0f, -3.4f), 1);
            AddNode(nodes, "N1", BoardTileType.Normal, new Vector3(2.8f, 0f, -3.4f), 2);
            AddNode(nodes, "N2", BoardTileType.Reward, new Vector3(5.6f, 0f, -3.4f), 3);
            AddNode(nodes, "N3", BoardTileType.DirectorEvent, new Vector3(8.4f, 0f, -3.4f), 8, 4);
            AddNode(nodes, "N4", BoardTileType.Normal, new Vector3(11.2f, 0f, -4.8f), 5);
            AddNode(nodes, "N5", BoardTileType.Shop, new Vector3(14.2f, 0f, -5.2f), 6);
            AddNode(nodes, "N6", BoardTileType.Reward, new Vector3(17.3f, 0f, -5.2f), 7);
            AddNode(nodes, "N7", BoardTileType.Normal, new Vector3(20.4f, 0f, -4.2f), 8);
            AddNode(nodes, "N8", BoardTileType.Reward, new Vector3(23.4f, 0f, -3.1f), 9);
            AddNode(nodes, "N9", BoardTileType.Shop, new Vector3(26.2f, 0f, -1.0f), 10);
            AddNode(nodes, "N10", BoardTileType.Trap, new Vector3(25.4f, 0f, 2.0f), 11);
            AddNode(nodes, "N11", BoardTileType.DirectorEvent, new Vector3(22.8f, 0f, 4.6f), 15, 12);
            AddNode(nodes, "N12", BoardTileType.Trap, new Vector3(19.4f, 0f, 5.4f), 13);
            AddNode(nodes, "N13", BoardTileType.Normal, new Vector3(15.8f, 0f, 5.6f), 14);
            AddNode(nodes, "N14", BoardTileType.Trap, new Vector3(12.1f, 0f, 4.8f), 15);
            AddNode(nodes, "N15", BoardTileType.Shop, new Vector3(8.6f, 0f, 3.1f), 16);
            AddNode(nodes, "N16", BoardTileType.Reward, new Vector3(5.2f, 0f, 1.2f), 17);
            AddNode(nodes, "N17", BoardTileType.Normal, new Vector3(2.2f, 0f, -1.2f), 0);

            nodes[3].requiresPaidChoice = true;
            nodes[3].paidChoiceCost = 2;
            nodes[3].paidChoiceLabel = "\u4ed8\u8d39\u6362\u9053";
            nodes[3].defaultChoiceLabel = "\u8d44\u6e90\u8def\u7ebf";
            nodes[3].branchOptionALabel = "\u4ed8\u8d39\u6362\u9053";
            nodes[3].branchOptionBLabel = "\u8d44\u6e90\u8def\u7ebf";

            nodes[11].branchOptionALabel = "\u5546\u5e97\u8def\u7ebf";
            nodes[11].branchOptionBLabel = "\u5371\u9669\u8def\u7ebf";
            return nodes;
        }

        private void AddNode(List<BoardNodeDefinition> nodes, string id, BoardTileType tileType, Vector3 position, params int[] nextIndices)
        {
            var node = new BoardNodeDefinition
            {
                nodeId = id,
                tileType = tileType,
                position = position,
                nextIndices = new List<int>(nextIndices)
            };
            nodes.Add(node);
        }
    }
}

