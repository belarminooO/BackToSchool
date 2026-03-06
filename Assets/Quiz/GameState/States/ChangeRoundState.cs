using UnityEngine;
using System.Collections.Generic;
using GinjaGaming.FinalCharacterController;

public class ChangeRoundState : IGameState
{
    public void EnterState(QuizGameManager manager) {
        Debug.Log("[Server] Change Round State Entered.");
        manager.currentTimer.value = 10f;
        manager.maxTimer.value = 10f;
        if (manager.currentRound.value >= manager.totalRoundsSettings.value) {
            Debug.Log("[Server] Game Ended!");
            manager.currentTimer.value = 999f;
            if (manager.scoreManager != null) {
                manager.scoreManager.RPC_ShowEndGameScreen();
            }
        }
        else {
            manager.currentRound.value++;

            
            // var allPlayersArray = Object.FindObjectsByType<MyCharacterController>(FindObjectsSortMode.None);
            // var allPlayersList = new List<MyCharacterController>(allPlayersArray);

            var allPlayersList = manager.scoreManager.allPlayers;
            var allPlayersArray = allPlayersList.ToArray();
            
            manager.spawnManager.TeleportAllPlayersToRound(allPlayersArray, manager.currentRound.value);

            StationManager[] allRoomManagers = Object.FindObjectsByType<StationManager>(FindObjectsSortMode.None);
            foreach (StationManager stationManager in allRoomManagers)
            {
                if (stationManager.roundNumber == manager.currentRound.value)
                {
                    stationManager.AssignStations(allPlayersList);
                }
            }



            // manager.RPC_MoveCanvasToClassroom(manager.currentRound.value);
            manager.quizData.GoNextQuestion();
        }
    }

    public void UpdateState(QuizGameManager manager) {
        manager.currentTimer.value -= Time.deltaTime;
        
        if (manager.currentRound.value <= manager.totalRoundsSettings.value && manager.currentTimer.value <= 0)
        {
            manager.ChangeState(manager.freezeState);
        }
        // Debug.Log("[Server] Change Round State Updated.");
    }

    public void ExitState(QuizGameManager manager) {
        Debug.Log("[Server] Change Round State Exited.");
    }
}
