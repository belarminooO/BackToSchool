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

            return;
        }
        started = false;
        if (asServer)
        {

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

        int expected = PurrLobby.LobbyDataHolder.CapturedExpectedPlayerCount
            ?? Mathf.Max(GetLiveLobbyMemberCount(), manager.requiredPlayers);

        if (registeredCount >= expected && spawnedCount >= registeredCount && registeredCount > 0)
        {
            started = true;

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