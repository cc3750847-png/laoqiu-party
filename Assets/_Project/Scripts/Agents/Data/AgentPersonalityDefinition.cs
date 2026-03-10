using UnityEngine;

namespace LaoqiuParty.Agents.Data
{
    [CreateAssetMenu(
        fileName = "AgentPersonality",
        menuName = "LaoqiuParty/Agents/Personality Definition")]
    public class AgentPersonalityDefinition : ScriptableObject
    {
        [Range(0f, 1f)] public float aggression = 0.5f;
        [Range(0f, 1f)] public float greed = 0.5f;
        [Range(0f, 1f)] public float vengeance = 0.5f;
        [Range(0f, 1f)] public float riskTolerance = 0.5f;
        [Range(0f, 1f)] public float showmanship = 0.5f;
    }
}
