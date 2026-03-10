using System.Collections.Generic;
using LaoqiuParty.Board.Data;
using UnityEngine;

namespace LaoqiuParty.Board.Runtime
{
    public class BoardTile : MonoBehaviour
    {
        [SerializeField] private string tileId;
        [SerializeField] private BoardTileType tileType;
        [SerializeField] private List<BoardTile> nextTiles = new();

        public string TileId => tileId;
        public BoardTileType TileType => tileType;
        public IReadOnlyList<BoardTile> NextTiles => nextTiles;

        public void Configure(string id, BoardTileType type, List<BoardTile> links = null)
        {
            tileId = id;
            tileType = type;
            nextTiles = links ?? new List<BoardTile>();
            RefreshVisual();
        }

        private void Awake()
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.material.color = tileType switch
            {
                BoardTileType.Reward => new Color(0.45f, 0.85f, 0.35f),
                BoardTileType.Trap => new Color(0.95f, 0.35f, 0.35f),
                BoardTileType.Shop => new Color(0.35f, 0.65f, 0.95f),
                BoardTileType.DirectorEvent => new Color(0.8f, 0.55f, 0.95f),
                BoardTileType.Duel => new Color(0.95f, 0.75f, 0.35f),
                BoardTileType.Teleport => new Color(0.35f, 0.95f, 0.9f),
                _ => new Color(0.85f, 0.85f, 0.85f)
            };
        }
    }
}
