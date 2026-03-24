using PurrNet;
using GinjaGaming.FinalCharacterController;
using UnityEngine;

public class FirstPersonNetworkState : NetworkBehaviour
{
    private SyncVar<int> _syncState = new SyncVar<int>(ownerAuth: true);
    private SyncVar<Vector2> _syncInput = new SyncVar<Vector2>(ownerAuth: true);

    public Vector2 SyncedMovementInput => _syncInput.value;
    public PlayerMovementState SyncedMovementState => (PlayerMovementState)_syncState.value;

    private SyncVar<bool> _syncAttack = new SyncVar<bool>(ownerAuth: true);
    private SyncVar<bool> _syncGather = new SyncVar<bool>(ownerAuth: true);

    public bool SyncedAttack => _syncAttack.value;
    public bool SyncedGather => _syncGather.value;

    private PlayerState _playerState;
    private PlayerLocomotionInput _locomotionInput;
    private PlayerController _playerController;
    private PlayerActionsInput _actionsInput;


    private void Awake()
    {
        _playerState = GetComponent<PlayerState>();
        _locomotionInput = GetComponent<PlayerLocomotionInput>();

        _playerController = GetComponent<PlayerController>();
        _actionsInput = GetComponent<PlayerActionsInput>();

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
            // Fix: Layer name is "player" (lowercase) as seen in TagManager
            int playerLayer = LayerMask.NameToLayer("player");
            if (playerLayer != -1 && Camera.main != null)
            {
                Camera.main.cullingMask &= ~(1 << playerLayer);
            }

            RefreshBodyVisibility();

            if (_locomotionInput != null)
                _locomotionInput.enabled = true;
            if (_playerController != null)
                _playerController.enabled = true;
        }
        else
        {
            // For remote players, we can keep them on a different layer if needed
            // However, the Swapper handles the main layers now.
        }
    }

    public void RefreshBodyVisibility()
    {
        if (!isOwner) return;

        // Set all mesh renderers to ShadowsOnly for the local player
        // This hides the body from their own eyes but keeps it in the world
        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
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
        if (_actionsInput != null)
        {
            _syncAttack.value = _actionsInput.AttackPressed;
            _syncGather.value = _actionsInput.GatherPressed;
        }
    }

    private void OnStateChanged(int newState)
    {
        if (isOwner || _playerState == null)
            return;
        _playerState.SetPlayerMovementState((PlayerMovementState)newState);
    }
}
