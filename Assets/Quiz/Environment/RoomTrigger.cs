using UnityEngine;
using PurrNet;
using GinjaGaming.FinalCharacterController;

public class RoomTrigger : NetworkBehaviour
{
    [Header("Setup")]
    public int roomIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer)
        {
            return;
        }
        if (QuizGameManager.instance.localStateName != "ObstacleRaceState")
        {
            return;
        }

        MyCharacterController player = other.GetComponentInParent<MyCharacterController>();
        if (player != null)
        {
            int targetRoomIndex = QuizGameManager.instance.currentRound.value;
            if (roomIndex == targetRoomIndex)
            {
                QuizGameManager.instance.scoreManager.PlayerArrived(player);
            }

        }
    
    }
}
