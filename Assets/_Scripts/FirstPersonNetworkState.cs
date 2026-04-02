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

    [Header("Push Settings")]
    public float pushRange = 3f;
    public float pushForce = 7.5f;
    private bool _wasAttacking = false;

    [Header("Camera")]
    public PlayerCameraStack playerCameraStack;

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
        if (_actionsInput != null)       
            _actionsInput.enabled = false;   
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
            if (_actionsInput != null)             
                _actionsInput.enabled = true;   

            // FORCE LOAD CAMERA STACK
            // if (playerCameraStack == null)
            // {
            //     playerCameraStack = GetComponentInChildren<PlayerCameraStack>();
            //     if (playerCameraStack == null) 
            //         playerCameraStack = Object.FindAnyObjectByType<PlayerCameraStack>();
            // }


            if (playerCameraStack != null)
                playerCameraStack.initLocalPlayer();
            else
                Debug.LogWarning("PlayerCameraStack could not be found!");

        }
        else
        {
            CapsuleCollider bodyCollider = GetComponent<CapsuleCollider>();
            if (bodyCollider != null)
            {
                bodyCollider.height = 1.8f;
                bodyCollider.center = new Vector3(0, 0.9f, 0);
                bodyCollider.radius = 0.3f;
            }
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
            // Execute precisely when the animation event SetAttackPressedFalse fires
            if (!_actionsInput.AttackPressed && _wasAttacking)
            {
                TryPushOtherPlayers();
            }
            _wasAttacking = _actionsInput.AttackPressed;

            _syncAttack.value = _actionsInput.AttackPressed;
            _syncGather.value = _actionsInput.GatherPressed;
        }
    }

private void TryPushOtherPlayers()
    {
        Camera cam = _playerController != null ? _playerController.PlayerCamera : null;
        if (Camera.main == null) 
            return;
        Ray r = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        
        // Include Layer 0 (Default) since your Player Prefab is currently on the Default layer
        int layerMask = (1 << 0) | (1 << LayerMask.NameToLayer("player")) | (1 << LayerMask.NameToLayer("Player"));
        
        // SphereCast forgives exact aiming
        RaycastHit[] hits = Physics.SphereCastAll(r, 0.4f, pushRange, layerMask, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                continue;
            FirstPersonNetworkState otherState = hit.collider.GetComponentInParent<FirstPersonNetworkState>();
            NetworkIdentity otherIdentity = hit.collider.GetComponentInParent<NetworkIdentity>();
            
            if (otherState != null && otherIdentity != null)
            {
                Vector3 forceDir = Camera.main.transform.forward;
                forceDir.y = 0.2f; 
                
                CmdPushPlayerServerRpc(otherIdentity, forceDir.normalized * pushForce);
                break; 
            }
        }
    }

    [ServerRpc] 
    public void CmdPushPlayerServerRpc(NetworkIdentity targetIdentity, Vector3 forceVector)
    {
        if (targetIdentity != null)
        {
            FirstPersonNetworkState targetState = targetIdentity.GetComponent<FirstPersonNetworkState>();
            if (targetState != null)
            {
                targetState.ReceivePushOnServer(forceVector);
            }
        }
    }

    public void ReceivePushOnServer(Vector3 forceVector)
    {
        if (_playerController != null && _playerController.enabled)
        {
            _playerController.ApplyKnockback(forceVector);
        }
        else
        {
            TargetPushObserversRpc(forceVector);
        }
    }

    [ObserversRpc] 
    private void TargetPushObserversRpc(Vector3 forceVector)
    {
        if (!isOwner) 
            return;
            
        if (_playerController != null)
            _playerController.ApplyKnockback(forceVector);
    }

    // [ServerRpc(requireOwnership: false)]
    // public void PushServerRpc(Vector3 forceVector)
    // {

    //     if (isOwner)
    //     {
    //         if (_playerController != null)
    //             _playerController.ApplyKnockback(forceVector);
    //         return;
    //     }

    //     var network = GetComponent<NetworkIdentity>();
    //     if (network != null && network.owner.HasValue)
    //     {
    //         PushTargetRpc(network.owner.Value, forceVector);
    //     }
    // }

    // [TargetRpc]
    // public void PushTargetRpc(PlayerID target, Vector3 forceVector)
    // {
    //     if (_playerController != null)
    //     {
    //         _playerController.ApplyKnockback(forceVector);
    //     }
    // }

    [ServerRpc(requireOwnership:false)]
    public void PushServerRpc(Vector3 forceVector)
    {
        if (_playerController != null && _playerController.enabled)
        {
            _playerController.ApplyKnockback(forceVector);
            return;
        }
        PushOwnerRpc(forceVector);
    }

    [ObserversRpc]
    private void PushOwnerRpc(Vector3 forceVector)
    {
        if (!isOwner) 
            return;
        if (_playerController != null)
            _playerController.ApplyKnockback(forceVector);
    }   

    private void OnStateChanged(int newState)
    {
        if (isOwner || _playerState == null)
            return;
        _playerState.SetPlayerMovementState((PlayerMovementState)newState);
    }

    
}
