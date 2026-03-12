using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaoqiuParty.Board.Data
{
    [Serializable]
    public class BoardNodeDefinition
    {
        public string nodeId = "node";
        public BoardTileType tileType = BoardTileType.Normal;
        public Vector3 position;
        public List<int> nextIndices = new();
        public bool requiresPaidChoice;
        public int paidChoiceCost;
        public string paidChoiceLabel = string.Empty;
        public string defaultChoiceLabel = string.Empty;
        public string branchOptionALabel = string.Empty;
        public string branchOptionBLabel = string.Empty;
    }

    [CreateAssetMenu(fileName = "BoardLayoutDefinition", menuName = "Laoqiu/Board Layout Definition")]
    public class BoardLayoutDefinition : ScriptableObject
    {
        public List<BoardNodeDefinition> nodes = new();
    }
}

