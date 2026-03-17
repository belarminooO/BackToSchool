using PurrNet;
using GinjaGaming.FinalCharacterController;
using UnityEngine;

public class FirstPersonNetworkState : NetworkBehaviour
{
    private SyncVar<int> _syncState = new SyncVar<int>(ownerAuth: true);
    private SyncVar<Vector2> _syncInput = new SyncVar<Vector2>(ownerAuth: true);

    public Vector2 SyncedMovementInput => _syncInput.value;
    public PlayerMovementState SyncedMovementState => (PlayerMovementState)_syncState.value;

    private PlayerState _playerState;
    private PlayerLocomotionInput _locomotionInput;
    private PlayerController _playerController;


    private void Awake()
    {
        _playerState = GetComponent<PlayerState>();
        _locomotionInput = GetComponent<PlayerLocomotionInput>();

        _playerController = GetComponent<PlayerController>();

        if (_locomotionInput != null) 
            _locomotionInput.enabled = false;
        if (_playerController != null) 
            _playerController.enabled = false;
    }

    // protected override void OnSpawned()
    // {
    //     _syncState.onChanged += OnStateChanged;

    //     if (isOwner)
    //     {
    //         if (_locomotionInput != null) 
    //             _locomotionInput.enabled = true;
    //         if (_playerController != null) 
    //             _playerController.enabled = true;
    //     }
    //     else
    //     {

    //     }
    // }

    protected override void OnSpawned()
    {
        _syncState.onChanged += OnStateChanged;

        if (isOwner)
        {
            if (Camera.main != null)
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                Camera.main.cullingMask &= ~(1 << playerLayer);
            }

            if (_locomotionInput != null)
                _locomotionInput.enabled = true;
            if (_playerController != null)
                _playerController.enabled = true;
        }
        else
        {
            int remoteLayer = LayerMask.NameToLayer("RemotePlayer");
            if (remoteLayer != -1)
            {
                foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                    renderer.gameObject.layer = remoteLayer;
            }
        }
    }

    protected override void OnDespawned()
    {
        _syncState.onChanged -= OnStateChanged;
    }

    private void Update()
    {
        if (!isSpawned || !isOwner)
            return;
        if (_playerState != null)
            _syncState.value = (int)_playerState.CurrentPlayerMovementState;
        if (_locomotionInput != null)
            _syncInput.value = _locomotionInput.MovementInput;
    }

    private void OnStateChanged(int newState)
    {
        if (isOwner || _playerState == null)
            return;
        _playerState.SetPlayerMovementState((PlayerMovementState)newState);
    }
}
