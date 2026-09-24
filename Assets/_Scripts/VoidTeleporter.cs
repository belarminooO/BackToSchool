using UnityEngine;
using PurrNet;
using GinjaGaming.FinalCharacterController;
public class VoidTeleporter : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        IPlayerController player = other.GetComponentInParent<IPlayerController>();

        if (player != null)
        {
            var host = QuizGameManager.instance;

            if (host != null && host.isServer && host.spawnManager != null)
            {

                int currentRound = host.currentRound.value;

                host.spawnManager.TeleportPlayerToOutside(player, currentRound);
                Debug.Log($"[VoidTeleporter] {player.gameObject.name} fell in the void! Sent back to the start of obstacle race {currentRound}.");
            }
        }
    }
}