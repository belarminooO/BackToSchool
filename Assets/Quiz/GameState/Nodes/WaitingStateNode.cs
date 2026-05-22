using UnityEngine;
using PurrNet.StateMachine;
using System.Linq;
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
        int spawnedCount = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>().Count();

        // Expected count comes from the snapshot captured at SceneSwitcher time
        // (when OnAllReady fired and everyone was provably in the lobby).
        // The live Members list isn't trustworthy after the scene change because Steam
        // can fire a phantom disconnect once a peer stops running its callbacks during
        // the switch, shrinking the frozen Members snapshot in LobbyDataHolder.
        int expected = PurrLobby.LobbyDataHolder.CapturedExpectedPlayerCount
            ?? Mathf.Max(GetLiveLobbyMemberCount(), manager.requiredPlayers);

        Debug.Log($"[WaitingState] quizReady={manager.quizReady}, " +
                  $"registered={registeredCount}/{expected}, " +
                  $"spawned={spawnedCount}/{registeredCount} (captured={PurrLobby.LobbyDataHolder.CapturedExpectedPlayerCount?.ToString() ?? "null"})");

        // Gate requires:
        //   1) enough players registered to match the captured lobby size, AND
        //   2) every registered player's GameObject has spawned (no straggler).
        // Condition 2 catches the race where onPlayerJoined bumps registeredCount up
        // for a late connection but PlayerSpawner hasn't instantiated their prefab yet.
        if (registeredCount >= expected && spawnedCount >= registeredCount && registeredCount > 0)
        {
            started = true;
            Debug.Log($"[Server] {spawnedCount} players registered and spawned. Starting game!");
            manager.InitializeGamePlayers();
            machine.SetState(freezeState);
        }
    }

    private static int GetLiveLobbyMemberCount()
    {
        var lobbyHolder = Object.FindFirstObjectByType<PurrLobby.LobbyDataHolder>();
        if (lobbyHolder == null) return 0;
        if (!lobbyHolder.CurrentLobby.IsValid) return 0;
        if (lobbyHolder.CurrentLobby.Members == null) return 0;
        return lobbyHolder.CurrentLobby.Members.Count;
    }
}