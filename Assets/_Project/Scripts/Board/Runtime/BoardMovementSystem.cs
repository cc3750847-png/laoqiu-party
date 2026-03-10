using System.Collections;
using LaoqiuParty.GameFlow.Data;
using UnityEngine;

namespace LaoqiuParty.Board.Runtime
{
    public class BoardMovementSystem : MonoBehaviour
    {
        [SerializeField] private BoardGraph boardGraph;

        public void Configure(BoardGraph graph)
        {
            boardGraph = graph;
        }

        public IEnumerator MovePlayer(PlayerRuntimeState player, PlayerPawn pawn, int steps)
        {
            yield return MovePlayer(player, pawn, steps, null);
        }

        public IEnumerator MovePlayer(
            PlayerRuntimeState player,
            PlayerPawn pawn,
            int steps,
            System.Func<BoardTile, BoardTile> nextTileSelector)
        {
            if (player == null || pawn == null || pawn.PawnMotor == null || boardGraph == null || boardGraph.Tiles.Count == 0)
            {
                yield break;
            }

            for (var step = 0; step < steps; step++)
            {
                var currentTile = boardGraph.GetTile(player.boardPosition);
                BoardTile nextTile = null;
                while (nextTile == null)
                {
                    nextTile = GetNextTile(currentTile, player.boardPosition, nextTileSelector);
                    if (nextTile == null)
                    {
                        // Wait for a human branch selection callback to provide a valid tile.
                        yield return null;
                    }
                }

                if (nextTile == null)
                {
                    yield break;
                }

                player.boardPosition = GetTileIndex(nextTile);
                yield return pawn.PawnMotor.MoveToPosition(GetPawnPosition(nextTile, player.playerId));
            }
        }

        public void SnapPlayerToBoard(PlayerRuntimeState player, PlayerPawn pawn)
        {
            if (player == null || pawn == null || boardGraph == null)
            {
                return;
            }

            var tile = boardGraph.GetTile(player.boardPosition);
            if (tile != null)
            {
                pawn.PawnMotor?.SnapToPosition(GetPawnPosition(tile, player.playerId));
            }
        }

        private Vector3 GetPawnPosition(BoardTile tile, int playerId)
        {
            var lane = playerId % 4;
            var xOffset = lane switch
            {
                0 => -0.35f,
                1 => 0.35f,
                2 => -0.35f,
                _ => 0.35f
            };
            var zOffset = lane switch
            {
                0 => -0.35f,
                1 => -0.35f,
                2 => 0.35f,
                _ => 0.35f
            };

            return tile.transform.position + new Vector3(xOffset, 0.8f, zOffset);
        }

        private BoardTile GetNextTile(BoardTile currentTile, int currentIndex, System.Func<BoardTile, BoardTile> nextTileSelector)
        {
            if (currentTile != null && currentTile.NextTiles.Count > 0)
            {
                // If a selector is provided (human or AI decision), do not fallback implicitly.
                // Returning null means "still waiting for a decision".
                if (nextTileSelector != null)
                {
                    return nextTileSelector(currentTile);
                }

                return currentTile.NextTiles[0];
            }

            var fallbackIndex = currentIndex + 1;
            if (fallbackIndex >= boardGraph.Tiles.Count)
            {
                fallbackIndex = 0;
            }

            return boardGraph.GetTile(fallbackIndex);
        }

        private int GetTileIndex(BoardTile tile)
        {
            for (var i = 0; i < boardGraph.Tiles.Count; i++)
            {
                if (boardGraph.Tiles[i] == tile)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
