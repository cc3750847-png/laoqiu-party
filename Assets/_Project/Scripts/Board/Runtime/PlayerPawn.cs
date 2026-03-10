using LaoqiuParty.Board.Movement;
using UnityEngine;

namespace LaoqiuParty.Board.Runtime
{
    public class PlayerPawn : MonoBehaviour
    {
        [SerializeField] private int playerId;
        [SerializeField] private PawnMotor pawnMotor;

        public int PlayerId => playerId;
        public PawnMotor PawnMotor => pawnMotor;

        private void Reset()
        {
            pawnMotor = GetComponent<PawnMotor>();
        }

        public void Configure(int id, PawnMotor motor)
        {
            playerId = id;
            pawnMotor = motor;
        }
    }
}
