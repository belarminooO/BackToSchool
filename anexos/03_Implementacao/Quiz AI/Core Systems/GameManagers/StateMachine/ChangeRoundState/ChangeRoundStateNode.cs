using UnityEngine;
using PurrNet.StateMachine;
public class ChangeRoundStateNode : StateNode
{
    public StateNode freezeState; 
    public override void Enter(bool asServer)
    {
        if (!asServer) 
            return;

        var manager = QuizGameManager.instance;
        manager.currentTimer.value = 10f;
        manager.maxTimer.value = 10f;

        if (manager.currentRound.value >= manager.totalRoundsSettings.value)
        {
            manager.currentTimer.value = 999f;
            if (manager.scoreManager != null) 
                manager.scoreManager.RPC_ShowEndGameScreen();
        }

        else
        {
            manager.currentRound.value++;

            manager.scoreManager.EvaluateRoundCosmetics();

            var players = manager.scoreManager.allPlayers;
            manager.spawnManager.TeleportAllPlayersToRound(players.ToArray(), manager.currentRound.value);

            StationManager[] stationManagers = Object.FindObjectsByType<StationManager>(FindObjectsSortMode.None);
            foreach (var stationManager in stationManagers)
                if (stationManager.roundNumber == manager.currentRound.value) stationManager.AssignStations(players);

            manager.quizData.GoNextQuestion();

            manager.RPC_SyncCurrentQuestion(
                manager.quizData.currentQuestionText,
                manager.quizData.redOption,
                manager.quizData.greenOption,
                manager.quizData.blueOption,
                manager.quizData.yellowOption,
                manager.quizData.currentCorrectAnswerIndex,
                manager.quizData.currentRound,
                manager.quizData.currentQuestion
            );

            manager.RPC_ResetJailedPlayers();
        }
    }
    public override void StateUpdate(bool asServer)
    {
        if (!asServer) 
            return;
        var manager = QuizGameManager.instance;
        if (manager.currentRound.value <= manager.totalRoundsSettings.value)
        {
            manager.currentTimer.value -= Time.deltaTime;
            if (manager.currentTimer.value <= 0) machine.SetState(freezeState);
        }
    }
}