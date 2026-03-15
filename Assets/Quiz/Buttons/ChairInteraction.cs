using UnityEngine;
using GinjaGaming.FinalCharacterController;

namespace GinjaGaming.FinalCharacterController
{
    public class ChairInteraction : MonoBehaviour
    {
        [Header("Sitting Settings")]
        [SerializeField] private Transform _sitPoint;
        [SerializeField] private Transform _exitPoint;
        
        private PlayerController _sittingPlayer;

        public void Interact(PlayerController player)
        {
            if (_sittingPlayer == null)
            {
                SitDown(player);
            }
            else if (_sittingPlayer == player)
            {
                StandUp();
            }
        }

        private void SitDown(PlayerController player)
        {
            _sittingPlayer = player;
            
            // Set position and rotation
            player.transform.position = _sitPoint.position;
            player.transform.rotation = _sitPoint.rotation;

            // Set state
            PlayerState playerState = player.GetComponent<PlayerState>();
            playerState.SetPlayerMovementState(PlayerMovementState.Sitting);
            
            // Disable CharacterController to prevent movement issues while sitting
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            Debug.Log("Player is now sitting.");
        }

        public void StandUp()
        {
            if (_sittingPlayer == null) return;

            // Set position to exit point
            if (_exitPoint != null)
            {
                _sittingPlayer.transform.position = _exitPoint.position;
            }

            // Restore CharacterController
            CharacterController cc = _sittingPlayer.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            // Set state
            PlayerState playerState = _sittingPlayer.GetComponent<PlayerState>();
            playerState.SetPlayerMovementState(PlayerMovementState.Idling);

            _sittingPlayer = null;
            Debug.Log("Player stood up.");
        }
    }
}
