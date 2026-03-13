using UnityEngine;
using PurrNet.StateMachine;
using GinjaGaming.FinalCharacterController;
public class WaitingStateNode : StateNode
{
    public StateNode freezeState;
    private bool started = false;

    public override void Enter(bool asServer)
    {
        if (freezeState == null)
        {
            Debug.LogError("[WaitingState] CRITICAL: freezeState reference is NULL! Check Inspector.");
            return;
        }
        started = false;
        if (asServer)
        {
            Debug.Log("[Server] Waiting: Generating Quiz in the Background.");
            var manager = QuizGameManager.instance;
            if (manager.quizData != null)
            {
                manager.quizData.onQuizReady = () => manager.OnQuizGenerationComplete();
                manager.quizData.AutoGenerateQuiz();
            }
        }
    }

    public override void StateUpdate(bool asServer)
    {
        if (!asServer || started)
        {
            return;
        }
        var manager = QuizGameManager.instance;

        if (!manager.quizReady)
        {
            return;
        }

        if (manager.scoreManager == null)
        {
            return;
        }

        int registeredCount = manager.scoreManager.registeredPlayers.Count;
        int spawnedCount = Object.FindObjectsByType<MyCharacterController>(FindObjectsSortMode.None).Length;

        Debug.Log($"[WaitingState] quizReady={manager.quizReady}, " +
                  $"registered={registeredCount}/{manager.requiredPlayers}, " +
                  $"spawned={spawnedCount}/{manager.requiredPlayers}");

        // Wait for both: PlayerIDs registered AND their GameObjects spawned
        if (registeredCount >= manager.requiredPlayers && spawnedCount >= manager.requiredPlayers)
        {
            started = true;
            Debug.Log($"[Server] {spawnedCount} players registered and spawned. Starting game!");
            manager.InitializeGamePlayers();
            machine.SetState(freezeState);
        }
    }
}