using System.Collections.Generic;
using UnityEngine;

namespace LaoqiuParty.Board.Runtime
{
    public class BoardGraph : MonoBehaviour
    {
        [SerializeField] private List<BoardTile> tiles = new();

        public IReadOnlyList<BoardTile> Tiles => tiles;

        public BoardTile GetTile(int index)
        {
            if (index < 0 || index >= tiles.Count)
            {
                return null;
            }

            return tiles[index];
        }
    }
}
