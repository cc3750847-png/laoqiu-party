using System.Collections.Generic;
using LaoqiuParty.Board.Runtime;
using UnityEngine;

namespace LaoqiuParty.Tools.Runtime
{
    public class SampleBoardBootstrap : MonoBehaviour
    {
        [SerializeField] private BoardGraph boardGraph;
        [SerializeField] private BoardTile tilePrefab;
        [SerializeField] private int tileCount = 12;
        [SerializeField] private float tileSpacing = 2.5f;
        [SerializeField] private bool rebuildOnStart = false;

        private void Start()
        {
            if (!rebuildOnStart || boardGraph == null || tilePrefab == null)
            {
                return;
            }

            BuildLinearBoard();
        }

        [ContextMenu("Build Linear Board")]
        public void BuildLinearBoard()
        {
            ClearChildren();

            var tiles = new List<BoardTile>(tileCount);
            for (var i = 0; i < tileCount; i++)
            {
                var tile = Instantiate(tilePrefab, transform);
                tile.name = $"Tile_{i:00}";
                tile.transform.localPosition = new Vector3(i * tileSpacing, 0f, 0f);
                tiles.Add(tile);
            }

            boardGraph.SetTiles(tiles);
        }

        private void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child.gameObject);
                }
                else
#endif
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
