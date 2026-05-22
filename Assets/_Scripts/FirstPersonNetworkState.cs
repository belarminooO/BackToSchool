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
    public float pushCooldown = 0.45f;
    private bool _wasAttacking = false;
    private float _nextPushTime = 0f;

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
        if (_actionsInput != null)
            _actionsInput.AttackStarted += OnAttackStarted;
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

    private void OnDestroy()
    {
        if (_actionsInput != null)
            _actionsInput.AttackStarted -= OnAttackStarted;
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
            if (_actionsInput.AttackPressed != _wasAttacking)
            {
                Debug.Log($"[PUSH-DBG 0] AttackPressed transition on {gameObject.name}: was={_wasAttacking} now={_actionsInput.AttackPressed} isServer={isServer} isOwner={isOwner} _actionsInput.enabled={_actionsInput.enabled}");
            }
            _wasAttacking = _actionsInput.AttackPressed;

            _syncAttack.value = _actionsInput.AttackPressed;
            _syncGather.value = _actionsInput.GatherPressed;
        }
        else
        {
            Debug.Log($"[PUSH-DBG 0-NULL] _actionsInput is NULL on {gameObject.name} isServer={isServer} isOwner={isOwner}");
        }
    }

    private void OnAttackStarted()
    {
        if (!isSpawned || !isOwner)
            return;

        if (Time.time < _nextPushTime)
            return;

        Camera cam = _playerController != null ? _playerController.PlayerCamera : null;
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning($"[PUSH-DBG INPUT] Could not find a camera for {gameObject.name}");
            return;
        }

        _nextPushTime = Time.time + pushCooldown;
        Vector3 origin = cam.transform.position;
        Vector3 direction = cam.transform.forward.normalized;

        Debug.Log($"[PUSH-DBG INPUT] AttackStarted on {gameObject.name}. Requesting server push from {origin} dir={direction}");
        RequestPushServerRpc(origin, direction);
    }

    [ServerRpc]
    private void RequestPushServerRpc(Vector3 rayOrigin, Vector3 rayDirection)
    {
        Vector3 fallbackOrigin = transform.position + Vector3.up * 1.4f;
        if (Vector3.Distance(rayOrigin, fallbackOrigin) > pushRange + 2f)
            rayOrigin = fallbackOrigin;

        if (rayDirection.sqrMagnitude < 0.0001f)
            rayDirection = transform.forward;

        TryPushOtherPlayers(rayOrigin, rayDirection.normalized);
    }

    private void TryPushOtherPlayers(Vector3 rayOrigin, Vector3 rayDirection)
    {
        Ray r = new Ray(rayOrigin, rayDirection);

        // Include Layer 0 (Default) since your Player Prefab is currently on the Default layer
        int layerMask = (1 << 0) | (1 << LayerMask.NameToLayer("player")) | (1 << LayerMask.NameToLayer("Player"));

        // SphereCast forgives exact aiming
        RaycastHit[] hits = Physics.SphereCastAll(r, 0.4f, pushRange, layerMask, QueryTriggerInteraction.Ignore);
        Debug.Log($"[PUSH-DBG 1] TryPushOtherPlayers fired. isServer={isServer} isOwner={isOwner} hits={hits.Length} rayOrigin={rayOrigin} rayDir={rayDirection}");
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                Debug.Log($"[PUSH-DBG 1a] skipping self-hit: {hit.collider.name}");
                continue;
            }
            FirstPersonNetworkState otherState = hit.collider.GetComponentInParent<FirstPersonNetworkState>();
            Debug.Log($"[PUSH-DBG 1b] hit={hit.collider.name} otherState={(otherState == null ? "NULL" : otherState.gameObject.name)}");

            if (otherState != null)
            {
                Vector3 forceDir = rayDirection;
                forceDir.y = 0.2f;

                Debug.Log($"[PUSH-DBG 2] Server applying push to {otherState.gameObject.name}. otherState.isOwner={otherState.isOwner} otherState.owner={otherState.owner}");
                otherState.ReceivePushOnServer(forceDir.normalized * pushForce);
                break;
            }
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void PushServerRpc(Vector3 forceVector)
    {
        Debug.Log($"[PUSH-DBG 3] PushServerRpc reached server on {gameObject.name}. isServer={isServer} owner={owner}");
        ReceivePushOnServer(forceVector);
    }

    public void ReceivePushOnServer(Vector3 forceVector)
    {
        Debug.Log($"[PUSH-DBG 4] ReceivePushOnServer on {gameObject.name}. isServer={isServer} owner.HasValue={owner.HasValue} owner={owner}");
        if (owner.HasValue)
            TargetPushRpc(owner.Value, forceVector);
    }

    [TargetRpc]
    private void TargetPushRpc(PlayerID target, Vector3 forceVector)
    {
        Debug.Log($"[PUSH-DBG 5] TargetPushRpc fired on {gameObject.name}. target={target} isOwner={isOwner} _playerController={(_playerController == null ? "NULL" : "OK")}");
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

    private void OnStateChanged(int newState)
    {
        if (isOwner || _playerState == null)
            return;
        _playerState.SetPlayerMovementState((PlayerMovementState)newState);
    }

    
}
