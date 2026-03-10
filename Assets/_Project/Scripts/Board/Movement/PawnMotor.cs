using System.Collections;
using LaoqiuParty.Board.Runtime;
using UnityEngine;

namespace LaoqiuParty.Board.Movement
{
    public class PawnMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;

        public IEnumerator MoveToTile(BoardTile tile)
        {
            if (tile == null)
            {
                yield break;
            }

            while (Vector3.Distance(transform.position, tile.transform.position) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    tile.transform.position,
                    moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = tile.transform.position;
        }

        public void SnapToTile(BoardTile tile)
        {
            if (tile != null)
            {
                transform.position = tile.transform.position;
            }
        }
    }
}
