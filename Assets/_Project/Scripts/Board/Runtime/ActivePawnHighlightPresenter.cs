using System.Collections.Generic;
using LaoqiuParty.GameFlow.Controllers;
using UnityEngine;

namespace LaoqiuParty.Board.Runtime
{
    public class ActivePawnHighlightPresenter : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoopController;
        [SerializeField] private List<PlayerPawn> pawns = new();
        [SerializeField] private Color inactiveColor = new Color(0.14f, 0.16f, 0.20f, 0.88f);
        [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.26f, 0.98f);

        public void Configure(GameLoopController loopController, List<PlayerPawn> playerPawns)
        {
            gameLoopController = loopController;
            pawns = playerPawns ?? new List<PlayerPawn>();
        }

        private void Update()
        {
            if (gameLoopController == null || gameLoopController.MatchState == null)
            {
                return;
            }

            var activePlayer = gameLoopController.MatchState.GetActivePlayer();
            var activePlayerId = activePlayer != null ? activePlayer.playerId : -1;

            foreach (var pawn in pawns)
            {
                if (pawn == null)
                {
                    continue;
                }

                var indicator = pawn.transform.Find("ActiveIndicator");
                if (indicator == null)
                {
                    continue;
                }

                var renderer = indicator.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                var isActive = pawn.PlayerId == activePlayerId;
                renderer.material.color = isActive ? Pulse(activeColor) : inactiveColor;
                indicator.localScale = isActive
                    ? new Vector3(0.80f, 0.02f, 0.80f)
                    : new Vector3(0.66f, 0.015f, 0.66f);
            }
        }

        private Color Pulse(Color color)
        {
            var t = 0.75f + Mathf.PingPong(Time.time * 2.4f, 0.25f);
            return new Color(color.r * t, color.g * t, color.b * t, color.a);
        }
    }
}

