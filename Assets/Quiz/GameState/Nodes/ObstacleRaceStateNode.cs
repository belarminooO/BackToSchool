using UnityEngine;
using PurrNet.StateMachine;
public class ObstacleRaceStateNode : StateNode
{
    public StateNode nextState; // Connects to ChangeRoundStateNode
    public override void Enter(bool asServer)
    {
        if (asServer)
        {
            var manager = QuizGameManager.instance;
            manager.currentTimer.value = 30f;
            manager.maxTimer.value = 30f;
            if (manager.scoreManager != null) 
                manager.scoreManager.PrepareForObstacleRace();
        }
    }
    public override void StateUpdate(bool asServer)
    {
        if (!asServer) return;
        var manager = QuizGameManager.instance;
        manager.currentTimer.value -= Time.deltaTime;
        if (manager.currentTimer.value <= 0f || manager.scoreManager.allPlayersArrived())
        {
            if (manager.currentTimer.value <= 0f && !manager.scoreManager.allPlayersArrived())
            {
                manager.scoreManager.PenalizePlayers();
                var stragglers = manager.scoreManager.GetPenalizedPlayers();
                manager.spawnManager.TeleportStragglersToRound(stragglers, manager.scoreManager.allPlayers, manager.currentRound.value + 1);
            }
            machine.SetState(nextState);
        }
    }
}