using UnityEngine;
using System.Collections.Generic;
using GinjaGaming.FinalCharacterController;


public class FreezeState : IGameState
{
    private bool isGenerating  = false;

    public void SetIsGenerating(bool isGenerate)
    {
        isGenerating = isGenerate;
    }
    public void EnterState(QuizGameManager manager)
    {
       if (isGenerating)
        {
            manager.currentTimer.value = 0f;
            manager.maxTimer.value = 0f;
            Debug.Log("[Server] Freeze State (Generating): Waiting for quiz JSON...");
            if (manager.isServer && manager.quizData != null)
            {
                manager.quizData.onQuizReady = () => manager.OnQuizGenerationComplete();
                manager.quizData.AutoGenerateQuiz();
            }
        }
        else
        {
            manager.currentTimer.value = 5f;
            manager.maxTimer.value = 5f;
            Debug.Log("[Server] Freeze State (Between Questions): 5 seconds");
            if (manager.isServer && manager.quizData != null)
                manager.quizData.UpdateCurrentQuestion();
        }
    }

    public void UpdateState(QuizGameManager manager)
    {
        if (isGenerating)
        {
            return;
        }

        manager.currentTimer.value -= Time.deltaTime;
        if (manager.currentTimer.value <= 0f) {
            manager.ChangeState(manager.questionState);
        }
    }

    public void ExitState(QuizGameManager manager)
    {
        isGenerating = false;
        Debug.Log("[Server] Freeze State Exited");
    }

}
