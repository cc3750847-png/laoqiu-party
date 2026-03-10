using UnityEngine;

namespace LaoqiuParty.Rules.Dice
{
    public class DiceRoller : MonoBehaviour
    {
        [SerializeField] private int minimumRoll = 1;
        [SerializeField] private int maximumRoll = 6;

        public int Roll()
        {
            return Random.Range(minimumRoll, maximumRoll + 1);
        }
    }
}
