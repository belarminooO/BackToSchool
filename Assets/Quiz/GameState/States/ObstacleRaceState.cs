using UnityEngine;
using System.Collections.Generic;
using GinjaGaming.FinalCharacterController;


public class ObstacleRaceState : IGameState
{
    public void EnterState(QuizGameManager manager)
    {
        manager.currentTimer.value = 30f; 
        manager.maxTimer.value = 30f;
        Debug.Log("[Server] Obstacle Race State Entered for " + manager.currentTimer.value + " seconds");

        if (manager.scoreManager != null)
        {
            manager.scoreManager.PrepareForObstacleRace();
        }
    }

    public void UpdateState(QuizGameManager manager)
    {
        manager.currentTimer.value -= Time.deltaTime;

        if (manager.currentTimer.value <= 0f || manager.scoreManager.allPlayersArrived())
        {
            if (manager.currentTimer.value <= 0f && !manager.scoreManager.allPlayersArrived())
            {
                Debug.Log("[Server] Time's up! Penalizing and teleporting stragglers.");
                
                manager.scoreManager.PenalizePlayers();
                var stragglers = manager.scoreManager.GetPenalizedPlayers();
                manager.spawnManager.TeleportStragglersToRound(stragglers, manager.scoreManager.allPlayers, manager.currentRound.value + 1);

            }
            else 
            {
                Debug.Log("[Server] All players have arrived in time!");
            }
            manager.ChangeState(manager.changeRoundState);
        }
    }

    public void ExitState(QuizGameManager manager)
    {
        Debug.Log("[Server] Obstacle Race State Exited");
    }

}