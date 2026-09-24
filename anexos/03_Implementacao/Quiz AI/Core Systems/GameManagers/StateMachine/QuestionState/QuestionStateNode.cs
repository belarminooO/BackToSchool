using UnityEngine;
using PurrNet.StateMachine;
using GinjaGaming.FinalCharacterController;
public class QuestionStateNode : StateNode
{
    public StateNode nextPhase; 
    public StateNode freezeState; 
    public override void Enter(bool asServer)
    {
        if (asServer)
        {
            var manager = QuizGameManager.instance;
            manager.currentTimer.value = manager.roundDurationSettings.value;
            manager.maxTimer.value = manager.currentTimer.value;

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
        }
    }
    public override void StateUpdate(bool asServer)
    {
        if (!asServer) return;
        var manager = QuizGameManager.instance;
        manager.currentTimer.value -= Time.deltaTime;
        if (manager.currentTimer.value <= 0f)
        {
            EvaluateAnswers(manager);
            foreach (var player in manager.scoreManager.allPlayers) player.hasAnswered = false;
            if (manager.quizData.currentQuestion < manager.quizData.questionsPerRound)
            {
                manager.quizData.GoNextQuestion();
                machine.SetState(freezeState);
            }
            else
            {
                if (manager.currentRound.value >= manager.totalRoundsSettings.value)
                {
                    manager.currentTimer.value = 999f;
                    if (manager.scoreManager != null) {
                        manager.scoreManager.RPC_ShowEndGameScreen();
                    }
                }
                else
                {
                    machine.SetState(nextPhase);
                }
            }
        }
    }
    private void EvaluateAnswers(QuizGameManager manager)
    {
        AnswerButtonManager[] stations = Object.FindObjectsByType<AnswerButtonManager>(FindObjectsSortMode.None);
        int correctIdx = manager.quizData.currentCorrectAnswerIndex;
        foreach (var station in stations)
        {
            if (station.gameObject.activeInHierarchy && station.ownerPlayer.value != null && station.lockedAnswerIndex != -1)
            {
                bool isCorrect = (station.lockedAnswerIndex == correctIdx);
                if (isCorrect) 
                    manager.scoreManager.AddScoreToTeam(station.ownerPlayer.value.GetComponent<IPlayerController>().team, 1);
                station.RPC_ShowVignette(isCorrect);
                station.RPC_ResetButtons();
            }
        }
        manager.scoreManager.RPC_UpdateLeaderboard();
    }
}