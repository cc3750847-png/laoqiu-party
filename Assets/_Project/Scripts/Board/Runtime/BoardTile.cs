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
    }
}
