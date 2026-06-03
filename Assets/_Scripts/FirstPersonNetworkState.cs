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
    private GeneratedFootstepEmitter _footstepEmitter;

    private void Awake()
    {
        _playerState = GetComponent<PlayerState>();
        _locomotionInput = GetComponent<PlayerLocomotionInput>();

        _playerController = GetComponent<PlayerController>();
        _actionsInput = GetComponent<PlayerActionsInput>();
        GeneratedQuizSfx.EnsureExists();

        _footstepEmitter = GetComponent<GeneratedFootstepEmitter>();
        if (_footstepEmitter == null)
            _footstepEmitter = gameObject.AddComponent<GeneratedFootstepEmitter>();
        _footstepEmitter.ConfigureForPlayer();

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
        {
            UpdateFootsteps(Time.deltaTime);
            return;
        }
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

        UpdateFootsteps(Time.deltaTime);
    }

    private void OnAttackStarted()
    {
        if (!isSpawned || !isOwner)
            return;

        if (Time.time < _nextPushTime)
            return;

        Transform sourceTransform = null;
        if (_playerController != null && _playerController.PlayerCamera != null)
            sourceTransform = _playerController.PlayerCamera.transform;
        if (sourceTransform == null && _playerController != null && _playerController.CameraTarget != null)
            sourceTransform = _playerController.CameraTarget;
        if (sourceTransform == null)
            sourceTransform = transform;

        _nextPushTime = Time.time + pushCooldown;
        Vector3 origin = sourceTransform.position;
        Vector3 direction = sourceTransform.forward.normalized;
        FirstPersonNetworkState localTarget = FindLocalPushTarget(sourceTransform);
        NetworkIdentity targetIdentity = localTarget != null ? localTarget.GetComponent<NetworkIdentity>() : null;

        Debug.Log($"[PUSH-DBG INPUT] AttackStarted on {gameObject.name}. source={sourceTransform.name} origin={origin} dir={direction} localTarget={(localTarget == null ? "NULL" : localTarget.gameObject.name)}");
        RequestPushServerRpc(targetIdentity, origin, direction);
    }

    [ServerRpc]
    private void RequestPushServerRpc(NetworkIdentity targetIdentity, Vector3 rayOrigin, Vector3 rayDirection)
    {
        Vector3 fallbackOrigin = transform.position + Vector3.up * 1.4f;
        if (Vector3.Distance(rayOrigin, fallbackOrigin) > pushRange + 2f)
            rayOrigin = fallbackOrigin;

        if (rayDirection.sqrMagnitude < 0.0001f)
            rayDirection = transform.forward;

        if (targetIdentity != null)
        {
            FirstPersonNetworkState targetState = targetIdentity.GetComponent<FirstPersonNetworkState>();
            if (IsValidClientChosenTarget(targetState, rayDirection))
            {
                Debug.Log($"[PUSH-DBG TARGET] Client-selected target accepted: {targetState.gameObject.name}");
                ApplyPushToTarget(targetState, rayDirection);
                return;
            }

            Debug.Log($"[PUSH-DBG TARGET] Client-selected target rejected: {(targetState == null ? "NULL" : targetState.gameObject.name)}");
        }

        TryPushOtherPlayers(rayOrigin, rayDirection.normalized);
    }

    private FirstPersonNetworkState FindLocalPushTarget(Transform sourceTransform)
    {
        Ray r = new Ray(sourceTransform.position, sourceTransform.forward);
        int layerMask = (1 << 0) | (1 << LayerMask.NameToLayer("player")) | (1 << LayerMask.NameToLayer("Player"));
        RaycastHit[] hits = Physics.SphereCastAll(r, 0.45f, pushRange + 0.75f, layerMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                continue;

            FirstPersonNetworkState otherState = hit.collider.GetComponentInParent<FirstPersonNetworkState>();
            if (otherState != null && otherState != this)
            {
                Debug.Log($"[PUSH-DBG LOCAL] Local target candidate: {otherState.gameObject.name} via {hit.collider.name} at distance={hit.distance}");
                return otherState;
            }
        }

        Debug.Log($"[PUSH-DBG LOCAL] No local target found for {gameObject.name}");
        return null;
    }

    private bool IsValidClientChosenTarget(FirstPersonNetworkState targetState, Vector3 rayDirection)
    {
        if (targetState == null || targetState == this)
            return false;

        Vector3 fromAttacker = targetState.transform.position - transform.position;
        Vector3 flatToTarget = Vector3.ProjectOnPlane(fromAttacker, Vector3.up);
        float planarDistance = flatToTarget.magnitude;
        if (planarDistance > pushRange + 1.75f)
            return false;

        Vector3 flatForward = Vector3.ProjectOnPlane(rayDirection, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = transform.forward;

        if (planarDistance > 0.01f)
        {
            float facingDot = Vector3.Dot(flatForward, flatToTarget.normalized);
            if (facingDot < -0.15f)
                return false;
        }

        return true;
    }

    private void ApplyPushToTarget(FirstPersonNetworkState targetState, Vector3 pushDirection)
    {
        Vector3 forceDir = pushDirection;
        forceDir.y = 0.2f;
        Debug.Log($"[PUSH-DBG 2] Server applying push to {targetState.gameObject.name}. owner={targetState.owner} pushDir={forceDir}");
        targetState.ReceivePushOnServer(forceDir.normalized * pushForce);
    }

    private void TryPushOtherPlayers(Vector3 rayOrigin, Vector3 rayDirection)
    {
        int layerMask = (1 << 0) | (1 << LayerMask.NameToLayer("player")) | (1 << LayerMask.NameToLayer("Player"));
        Vector3 searchOrigin = transform.position + Vector3.up * 0.9f;
        Vector3 flatForward = Vector3.ProjectOnPlane(rayDirection, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = transform.forward;

        Collider[] hits = Physics.OverlapSphere(searchOrigin, pushRange + 0.75f, layerMask, QueryTriggerInteraction.Ignore);
        Debug.Log($"[PUSH-DBG 1] TryPushOtherPlayers fired. isServer={isServer} isOwner={isOwner} overlaps={hits.Length} rayOrigin={rayOrigin} rayDir={rayDirection} searchOrigin={searchOrigin}");

        FirstPersonNetworkState bestTarget = null;
        float bestScore = float.MinValue;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform))
            {
                Debug.Log($"[PUSH-DBG 1a] skipping self-hit: {hit.name}");
                continue;
            }

            FirstPersonNetworkState otherState = hit.GetComponentInParent<FirstPersonNetworkState>();
            if (otherState == null || otherState == this)
                continue;

            Vector3 targetCenter = otherState.transform.position + Vector3.up * 0.9f;
            Vector3 toTarget = targetCenter - searchOrigin;
            float distance = toTarget.magnitude;
            if (distance > pushRange + 0.75f || distance < 0.05f)
                continue;

            Vector3 toTargetDir = toTarget / distance;
            float facingDot = Vector3.Dot(flatForward, Vector3.ProjectOnPlane(toTargetDir, Vector3.up).normalized);
            if (facingDot < 0.2f)
            {
                Debug.Log($"[PUSH-DBG 1b] rejecting {otherState.gameObject.name} due to angle. dot={facingDot}");
                continue;
            }

            if (Physics.Linecast(searchOrigin, targetCenter, out RaycastHit blockHit, ~0, QueryTriggerInteraction.Ignore))
            {
                FirstPersonNetworkState blockedPlayer = blockHit.collider.GetComponentInParent<FirstPersonNetworkState>();
                if (blockedPlayer != otherState && !blockHit.collider.transform.IsChildOf(transform))
                {
                    Debug.Log($"[PUSH-DBG 1c] line blocked for {otherState.gameObject.name} by {blockHit.collider.name}");
                    continue;
                }
            }

            float score = (facingDot * 3f) - distance;
            Debug.Log($"[PUSH-DBG 1d] candidate={otherState.gameObject.name} dist={distance} dot={facingDot} score={score}");
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = otherState;
            }
        }

        if (bestTarget != null)
        {
            Debug.Log($"[PUSH-DBG 2-FALLBACK] Fallback server target={bestTarget.gameObject.name} bestScore={bestScore}");
            ApplyPushToTarget(bestTarget, flatForward);
        }
        else
        {
            Debug.Log($"[PUSH-DBG 2-NO-HIT] No valid push target found for {gameObject.name}");
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

    private void UpdateFootsteps(float deltaTime)
    {
        if (_footstepEmitter == null)
            return;

        PlayerMovementState currentState = isOwner && _playerState != null
            ? _playerState.CurrentPlayerMovementState
            : SyncedMovementState;

        Vector2 moveInput = isOwner && _locomotionInput != null
            ? _locomotionInput.MovementInput
            : SyncedMovementInput;

        bool hasMovementInput = moveInput.sqrMagnitude > 0.02f;
        bool isGroundedState =
            currentState == PlayerMovementState.Walking ||
            currentState == PlayerMovementState.Running ||
            currentState == PlayerMovementState.Sprinting ||
            currentState == PlayerMovementState.Crouching;

        if (!isGroundedState || !hasMovementInput)
        {
            _footstepEmitter.Tick(false, 0f, deltaTime);
            return;
        }

        float speed = currentState switch
        {
            PlayerMovementState.Sprinting => 7f,
            PlayerMovementState.Running => 4f,
            PlayerMovementState.Walking => 2.2f,
            PlayerMovementState.Crouching => 1.6f,
            _ => 0f
        };

        _footstepEmitter.Tick(true, speed, deltaTime);
    }

    
}
