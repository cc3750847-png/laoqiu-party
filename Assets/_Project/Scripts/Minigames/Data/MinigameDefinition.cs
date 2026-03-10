using UnityEngine;

namespace LaoqiuParty.Minigames.Data
{
    [CreateAssetMenu(
        fileName = "MinigameDefinition",
        menuName = "LaoqiuParty/Minigames/Definition")]
    public class MinigameDefinition : ScriptableObject
    {
        public string minigameId;
        public string displayName;
        [TextArea] public string description;
        [Min(10)] public int durationSeconds = 45;
        [Min(2)] public int minimumPlayers = 2;
        [Min(2)] public int maximumPlayers = 4;
    }
}
