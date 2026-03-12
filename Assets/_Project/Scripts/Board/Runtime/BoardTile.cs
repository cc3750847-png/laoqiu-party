using System.Collections.Generic;
using LaoqiuParty.Board.Data;
using UnityEngine;

namespace LaoqiuParty.Board.Runtime
{
    public class BoardTile : MonoBehaviour
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        [SerializeField] private string tileId;
        [SerializeField] private BoardTileType tileType;
        [SerializeField] private List<BoardTile> nextTiles = new();
        [SerializeField] private bool requiresPaidChoice;
        [SerializeField] private int paidChoiceCost;
        [SerializeField] private string paidChoiceLabel = "Fast Lane";
        [SerializeField] private string defaultChoiceLabel = "Safe Lane";
        [SerializeField] private string branchOptionALabel = string.Empty;
        [SerializeField] private string branchOptionBLabel = string.Empty;

        private Renderer rootRenderer;
        private bool isHighlighted;
        private bool isDimmed;
        private Color cachedTopColor;

        public string TileId => tileId;
        public BoardTileType TileType => tileType;
        public IReadOnlyList<BoardTile> NextTiles => nextTiles;
        public bool RequiresPaidChoice => requiresPaidChoice;
        public int PaidChoiceCost => paidChoiceCost;
        public string PaidChoiceLabel => paidChoiceLabel;
        public string DefaultChoiceLabel => defaultChoiceLabel;
        public string BranchOptionALabel => branchOptionALabel;
        public string BranchOptionBLabel => branchOptionBLabel;

        public void Configure(string id, BoardTileType type, List<BoardTile> links = null)
        {
            tileId = id;
            tileType = type;
            nextTiles = links ?? new List<BoardTile>();
            RefreshVisual();
        }

        public void ConfigurePaidChoice(bool requiresChoice, int cost, string paidLabel, string defaultLabel)
        {
            requiresPaidChoice = requiresChoice;
            paidChoiceCost = Mathf.Max(0, cost);
            paidChoiceLabel = string.IsNullOrWhiteSpace(paidLabel) ? "Fast Lane" : paidLabel;
            defaultChoiceLabel = string.IsNullOrWhiteSpace(defaultLabel) ? "Safe Lane" : defaultLabel;
            RefreshVisual();
        }

        public void ConfigureBranchLabels(string optionALabel, string optionBLabel)
        {
            branchOptionALabel = optionALabel ?? string.Empty;
            branchOptionBLabel = optionBLabel ?? string.Empty;
        }

        private void Awake()
        {
            rootRenderer = GetComponent<Renderer>();
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            var renderer = rootRenderer != null ? rootRenderer : GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            transform.localScale = new Vector3(1.9f, 0.22f, 1.9f);

            var topColor = tileType switch
            {
                BoardTileType.Reward => new Color(0.42f, 0.92f, 0.40f),
                BoardTileType.Trap => new Color(0.98f, 0.34f, 0.26f),
                BoardTileType.Shop => new Color(0.25f, 0.67f, 1f),
                BoardTileType.DirectorEvent => new Color(0.82f, 0.50f, 0.98f),
                BoardTileType.Duel => new Color(1f, 0.76f, 0.22f),
                BoardTileType.Teleport => new Color(0.33f, 0.96f, 0.88f),
                _ => new Color(0.24f, 0.27f, 0.33f)
            };
            cachedTopColor = topColor;

            var baseColor = new Color(0.07f, 0.09f, 0.12f);
            renderer.material.SetColor(ColorProperty, ResolveDisplayColor(topColor));

            EnsureVisualPart("Base", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(2.2f, 0.28f, 2.2f), baseColor);
            EnsureVisualPart("Deck", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(1.9f, 0.18f, 1.9f), ResolveDisplayColor(topColor));
            EnsureVisualPart("GlowStrip", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(1.45f, 0.03f, 1.45f), ResolveGlowColor(topColor));
            EnsureBeacon(ResolveDisplayColor(topColor));
            EnsureRouteMarker(ResolveGlowColor(topColor));
            EnsurePaidChoiceMarker();
        }

        private void Update()
        {
            if (!isHighlighted)
            {
                return;
            }

            RefreshVisual();
        }

        public void SetRouteHighlight(bool highlighted, bool dimmed)
        {
            isHighlighted = highlighted;
            isDimmed = dimmed;
            RefreshVisual();
        }

        public void ClearRouteHighlight()
        {
            if (!isHighlighted && !isDimmed)
            {
                return;
            }

            isHighlighted = false;
            isDimmed = false;
            RefreshVisual();
        }

        private void EnsureVisualPart(string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var child = transform.Find(name);
            GameObject childObject;
            if (child == null)
            {
                childObject = GameObject.CreatePrimitive(primitiveType);
                childObject.name = name;
                childObject.transform.SetParent(transform, false);
                DestroyColliderIfAny(childObject);
            }
            else
            {
                childObject = child.gameObject;
            }

            childObject.transform.localPosition = localPosition;
            childObject.transform.localRotation = Quaternion.identity;
            childObject.transform.localScale = localScale;

            var childRenderer = childObject.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.material.SetColor(ColorProperty, color);
            }
        }

        private void EnsureBeacon(Color color)
        {
            if (tileType == BoardTileType.Normal)
            {
                RemoveChildIfExists("Beacon");
                return;
            }

            EnsureVisualPart("Beacon", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 0.18f, 0.12f), Lighten(color, 0.18f));
        }

        private void EnsureRouteMarker(Color color)
        {
            if (nextTiles.Count <= 1)
            {
                RemoveChildIfExists("RouteMarker");
                return;
            }

            EnsureVisualPart("RouteMarker", PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(1.2f, 0.04f, 0.28f), Lighten(color, 0.28f));
            var routeMarker = transform.Find("RouteMarker");
            if (routeMarker != null)
            {
                routeMarker.localRotation = Quaternion.Euler(0f, 22f, 0f);
            }
        }

        private void EnsurePaidChoiceMarker()
        {
            if (!requiresPaidChoice)
            {
                RemoveChildIfExists("PaidMarker");
                return;
            }

            EnsureVisualPart("PaidMarker", PrimitiveType.Cube, new Vector3(0f, 0.34f, -0.48f), new Vector3(0.72f, 0.08f, 0.18f), new Color(1f, 0.82f, 0.24f));
            var paidMarker = transform.Find("PaidMarker");
            if (paidMarker != null)
            {
                paidMarker.localRotation = Quaternion.Euler(0f, 0f, -12f);
            }
        }

        private void RemoveChildIfExists(string childName)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        private void DestroyColliderIfAny(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        private Color Lighten(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                1f);
        }

        private Color ResolveDisplayColor(Color baseColor)
        {
            if (isHighlighted)
            {
                var pulse = 0.22f + Mathf.PingPong(Time.time * 1.8f, 0.20f);
                return Lighten(baseColor, pulse);
            }

            if (isDimmed)
            {
                return Color.Lerp(baseColor, new Color(0.08f, 0.10f, 0.12f), 0.55f);
            }

            return baseColor;
        }

        private Color ResolveGlowColor(Color baseColor)
        {
            if (isHighlighted)
            {
                var pulse = 0.38f + Mathf.PingPong(Time.time * 2.4f, 0.24f);
                return Lighten(baseColor, pulse);
            }

            if (isDimmed)
            {
                return Color.Lerp(baseColor, new Color(0.10f, 0.12f, 0.16f), 0.70f);
            }

            return Lighten(baseColor, 0.35f);
        }
    }
}
