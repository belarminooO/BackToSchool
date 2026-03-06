using UnityEngine;
using System.Collections.Generic;
using GinjaGaming.FinalCharacterController;


public class QuestionState : IGameState
{
    public void EnterState(QuizGameManager manager)
    {
        manager.currentTimer.value = manager.roundDurationSettings.value;
        manager.maxTimer.value = manager.currentTimer.value;
        Debug.Log("[Server] Question State Entered for " + manager.maxTimer.value + " seconds");
    }

    public void UpdateState(QuizGameManager manager)
    {
        manager.currentTimer.value -= Time.deltaTime;

        if (manager.currentTimer.value <= 0f) {

            EvaluateAnswers(manager);

            foreach (var player in manager.scoreManager.allPlayers) {
                player.hasAnswered = false;
            }

            if (manager.quizData.currentQuestion < manager.quizData.questionsPerRound) {
                manager.quizData.GoNextQuestion(); 
                manager.quizData.UpdateCurrentQuestion(); 
                manager.ChangeState(manager.freezeState); 
            } else {
                if (manager.currentRound.value >= manager.totalRoundsSettings.value) {
                    manager.ChangeState(manager.changeRoundState);
                } else {
                    manager.ChangeState(manager.obstacleRaceState);
                }
            }
        }
    }

    private void EvaluateAnswers(QuizGameManager manager)
    {
        AnswerButtonManager[] allStations = GameObject.FindObjectsByType<AnswerButtonManager>(FindObjectsSortMode.None);

        int correctAnswerIndex = manager.quizData.currentCorrectAnswerIndex;

        foreach (AnswerButtonManager station in allStations) {
            if (station.gameObject.activeInHierarchy && station.ownerPlayer.value != null && station.lockedAnswerIndex != -1)
            {
                bool correctAnswer = false;
                if (station.lockedAnswerIndex == correctAnswerIndex) {
                    MyCharacterController player = station.ownerPlayer.value.GetComponent<MyCharacterController>();
                    manager.scoreManager.AddScoreToTeam(player.team, 1);
                    correctAnswer = true;

                }

                station.RPC_ShowVignette(correctAnswer);
                station.RPC_ResetButtons();
       
            }
        }
        
        manager.scoreManager.RPC_UpdateLeaderboard();
    }

    public void ExitState(QuizGameManager manager)
    {
        manager.currentTimer.value = 0f;
        Debug.Log("[Server] Question State Exited");
    }

    private bool AllPlayersAnswered(QuizGameManager manager)
    {
        foreach (var player in manager.scoreManager.allPlayers) {
            if (!player.hasAnswered) {
                return false;
            }
        }
        return true;
    }

}
