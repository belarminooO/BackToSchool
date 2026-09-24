using UnityEngine;
using PurrNet;

namespace GinjaGaming.FinalCharacterController
{

    public class ChairInteraction : NetworkBehaviour, IInteractable
    {
        [Header("Sitting Settings")]
        [SerializeField] private Transform _sitPoint;
        [SerializeField] private Transform _exitPoint;

        private PlayerController _sittingPlayer;

        public SyncVar<bool> isOccupied = new SyncVar<bool>(ownerAuth: false);

        public void Interact(GameObject interactor)
        {
            PlayerController player = interactor.GetComponentInParent<PlayerController>();
            if (player == null)
                player = interactor.GetComponent<PlayerController>();
            if (player != null)
                Interact(player);
        }

        public void Interact(PlayerController player)
        {
            if (player == null)
                return;

            if (player.CurrentChair != null && player.CurrentChair != this)
                return;

            if (_sittingPlayer == null && !isOccupied.value)
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
            if (_sitPoint == null)
            {
                Debug.LogError("[ChairInteraction] _sitPoint is not assigned on " + gameObject.name, this);
                return;
            }

            _sittingPlayer = player;

            CmdSetOccupiedStatus(true);

            player.transform.position = _sitPoint.position;
            player.transform.rotation = _sitPoint.rotation;

            PlayerState playerState = player.GetComponent<PlayerState>();
            playerState?.SetPlayerMovementState(PlayerMovementState.Sitting);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.SetCurrentChair(this);
            Debug.Log("[ChairInteraction] Player is now sitting.");
        }

        public void StandUp()
        {
            if (_sittingPlayer == null) return;

            if (_exitPoint != null)
                _sittingPlayer.transform.position = _exitPoint.position;

            CharacterController cc = _sittingPlayer.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            PlayerState playerState = _sittingPlayer.GetComponent<PlayerState>();
            playerState?.SetPlayerMovementState(PlayerMovementState.Idling);
            _sittingPlayer.SetCurrentChair(null);

            _sittingPlayer = null;

            CmdSetOccupiedStatus(false);

            Debug.Log("[ChairInteraction] Player stood up.");
        }

        [ServerRpc(requireOwnership: false)]
        private void CmdSetOccupiedStatus(bool status)
        {
            isOccupied.value = status;
        }
    }
}
