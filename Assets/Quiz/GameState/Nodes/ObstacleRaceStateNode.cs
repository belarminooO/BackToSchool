using UnityEngine;
using PurrNet.StateMachine;
public class ObstacleRaceStateNode : StateNode
{
    [SerializeField] private float obstacleRaceTime = 75f;
    public StateNode nextState; // Connects to ChangeRoundStateNode
    public override void Enter(bool asServer)
    {
        if (asServer)
        {
            var manager = QuizGameManager.instance;
            manager.currentTimer.value = obstacleRaceTime;
            manager.maxTimer.value = obstacleRaceTime;
            
            if (manager.scoreManager != null) 
                manager.scoreManager.PrepareForObstacleRace();

            if (manager.spawnManager != null && manager.scoreManager != null)
                manager.spawnManager.TeleportAllPlayersToOutside(manager.scoreManager.allPlayers.ToArray(), manager.currentRound.value);

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
