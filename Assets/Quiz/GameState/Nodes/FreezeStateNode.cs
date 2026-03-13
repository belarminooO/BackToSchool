using UnityEngine;
using PurrNet.StateMachine;

public class FreezeStateNode : StateNode
{
    public StateNode questionState;
    private const float FreezeDuration = 3f;

    public override void Enter(bool asServer)
    {
        if (!asServer) return;
        var manager = QuizGameManager.instance;
        manager.currentTimer.value = FreezeDuration;
        manager.maxTimer.value = FreezeDuration;
        Debug.Log("[Server] Freeze State Entered — showing next question.");
    }

    public override void StateUpdate(bool asServer)
    {
        if (!asServer) return;
        var manager = QuizGameManager.instance;
        manager.currentTimer.value -= Time.deltaTime;
        if (manager.currentTimer.value <= 0f)
            machine.SetState(questionState);
    }
}
