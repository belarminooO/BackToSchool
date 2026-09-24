using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using PurrNet;
using GinjaGaming.FinalCharacterController;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ColegaEmpurrar : NetworkBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("Lista de waypoints para patrulha. Se vazia, tenta encontrar na cena.")]
    public List<Waypoint> waypoints;
    [Tooltip("Se ativo, escolhe waypoints aleatoriamente em vez de seguir a ordem.")]
    public bool randomPatrol = false;
    public float wanderSpeed = 2.5f;
    public float investigateSpeed = 4.5f;
    public float stopDistance = 0.8f;

    [Header("Vision Settings")]
    public float viewDistance = 12f;
    [Range(0, 180)]
    public float viewAngle = 80f;
    public LayerMask visionMask; 
    public LayerMask playerMask; 

    [Header("Push Settings")]
    public float pushForce = 35f;
    public float pushDistance = 1.2f;
    public float postPushDetectionCooldown = 0.5f;

    [Header("Debug Visuals")]
    [Tooltip("Mostra o cone de visão.")]
    public bool showDebugGizmos = true;
    [Tooltip("Mostra a linha de ligação entre os waypoints.")]
    public bool showPatrolPath = true;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private Vector3 investigationTarget;
    private float detectionSuppressedUntil = 0f;

    private IPlayerController _currentDetectionTarget;

    public SyncVar<VigiaState> CurrentState = new SyncVar<VigiaState>(VigiaState.Wandering);
    public SyncVar<float> SyncedSpeed = new SyncVar<float>(0f);

    private bool _patrolInitialized;

    private const float NavMeshSnapRadius = 5f;

    private static int inputXHash = Animator.StringToHash("inputX");
    private static int inputYHash = Animator.StringToHash("inputY");
    private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
    private static int isIdlingHash = Animator.StringToHash("isIdling");
    private static int isGroundedHash = Animator.StringToHash("isGrounded");

    [Header("Indicator Light")]
    [Tooltip("Luz que fica verde a vaguear e vermelha a perseguir.")]
    public Light indicatorLight;

    public enum VigiaState
    {
        Wandering,
        Waiting,
        Investigating
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        NpcVisualBootstrap.Configure(gameObject, animator);

        if (agent != null)
            agent.enabled = false;

        if (playerMask.value == 0) playerMask = LayerMask.GetMask("Player");
        if (visionMask.value == 0) visionMask = LayerMask.GetMask("Default", "Environment", "Ground");
        if (indicatorLight == null) indicatorLight = GetComponentInChildren<Light>();

        gameObject.isStatic = false;

        if (waypoints == null || waypoints.Count == 0)
        {
            FindWaypointsInScene();
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        if (isServer)
        {
            EnsureAgentReadyForNavigation();
        }
    }

    private void Update()
    {
        bool agentReady;
        if (isServer)
        {
            agentReady = EnsureAgentReadyForNavigation();
        }
        else
        {
            agentReady = HasValidNavMeshAgent();
        }

        if (agentReady && !_patrolInitialized && waypoints != null && waypoints.Count > 0)
        {
            GoToNextWaypoint();
        }

        if (!agentReady)
        {
            if (isServer)
                SyncedSpeed.value = 0f;

            UpdateAnimations();
            UpdateIndicatorLight();
            return;
        }

        if (isServer) 
        {
            if (CurrentState.value == VigiaState.Wandering || CurrentState.value == VigiaState.Waiting)
            {
                if (Time.time >= detectionSuppressedUntil)
                {
                    CheckVision();
                }
            }

            if (_patrolInitialized)
            {
                UpdateStateMachine();
                SyncedSpeed.value = agent.velocity.magnitude;
            }
            else
            {
                SyncedSpeed.value = 0f;
            }
        }

        UpdateAnimations();
        UpdateIndicatorLight();
    }

    private void OnValidate()
    {
        if (playerMask.value == 0) 
        {
            playerMask = LayerMask.GetMask("Player");
        }

        if (visionMask.value == 0)
        {
            visionMask = LayerMask.GetMask("Default", "Environment", "Ground");
        }

        NavMeshAgent nav = GetComponent<NavMeshAgent>();
        if (nav != null)
        {
            nav.speed = wanderSpeed;
            nav.stoppingDistance = stopDistance;
            nav.acceleration = 8f;
            nav.angularSpeed = 120f;
        }
    }

    private bool HasValidNavMeshAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private bool TryAlignAgentToNavMesh(float searchRadius = NavMeshSnapRadius)
    {
        if (agent == null)
            return false;

        if (agent.enabled && agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            transform.position = hit.position;

            if (!agent.enabled)
                agent.enabled = true;

            if (agent.isOnNavMesh)
                return true;

            return agent.Warp(hit.position);
        }

        return false;
    }

    private bool EnsureAgentReadyForNavigation()
    {
        return HasValidNavMeshAgent() || TryAlignAgentToNavMesh();
    }

    public void FindWaypointsInScene()
    {
        Waypoint[] foundWps = Object.FindObjectsByType<Waypoint>(FindObjectsSortMode.InstanceID);
        waypoints = new List<Waypoint>(foundWps);
        waypoints.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
    }

    private bool IsOnSameRamp(Vector3 targetPos)
    {
        bool myHitSuccess = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit myHit, 5f, visionMask);
        bool targetHitSuccess = Physics.Raycast(targetPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit targetHit, 5f, visionMask);

        if (myHitSuccess && targetHitSuccess)
        {
            return myHit.collider.gameObject == targetHit.collider.gameObject;
        }

        return Mathf.Abs(transform.position.x - targetPos.x) < 3.5f;
    }

    private void CheckVision()
    {
        int searchMask = playerMask.value | LayerMask.GetMask("Default");
        Collider[] targets = Physics.OverlapSphere(transform.position, viewDistance, searchMask);
        float halfViewAngle = viewAngle * 0.5f;

        foreach (var target in targets)
        {
            if (!target.CompareTag("Player"))
            {
                continue;
            }

            var p = target.GetComponentInParent<IPlayerController>();
            if (p != null && p.isJailed)
            {
                continue;
            }

            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            if (angleToTarget < halfViewAngle)
            {
                Vector3 startPos = transform.position + Vector3.up * 1.6f;
                Vector3 endPos = target.transform.position + Vector3.up * 1.6f;

                bool hitWall = Physics.Linecast(startPos, endPos, out RaycastHit hit, visionMask);

                if (!hitWall || hit.transform.root == target.transform.root)
                {
                    if (IsOnSameRamp(target.transform.position))
                    {
                        TriggerInvestigation(target.transform.position, p);
                        return;
                    }
                }
            }
        }
    }

    private void TriggerInvestigation(Vector3 origin, IPlayerController playerTarget)
    {
        if (!EnsureAgentReadyForNavigation())
        {
            return;
        }

        _currentDetectionTarget = playerTarget;
        investigationTarget = origin;
        CurrentState.value = VigiaState.Investigating;
        StopAllCoroutines();
        isWaiting = false;

        agent.isStopped = false;
        agent.speed = investigateSpeed;
        agent.SetDestination(investigationTarget);
    }

    private void UpdateStateMachine()
    {
        if (!_patrolInitialized || !HasValidNavMeshAgent())
            return;

        switch (CurrentState.value)
        {
            case VigiaState.Wandering:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    StartCoroutine(WaitAtWaypoint());
                }
                break;

            case VigiaState.Waiting:
                break;

            case VigiaState.Investigating:
                if (_currentDetectionTarget != null && !_currentDetectionTarget.isJailed)
                {
                    if (!IsOnSameRamp(_currentDetectionTarget.transform.position))
                    {
                        _currentDetectionTarget = null;
                        GoToNextWaypoint();
                        break;
                    }

                    investigationTarget = _currentDetectionTarget.transform.position;
                    agent.SetDestination(investigationTarget);

                    float dist = Vector3.Distance(transform.position, investigationTarget);
                    if (dist <= pushDistance)
                    {
                        PushPlayer(_currentDetectionTarget);
                    }
                }
                else
                {
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        StartCoroutine(WaitAfterInvestigation());
                    }
                }
                break;
        }
    }

    private void PushPlayer(IPlayerController player)
    {
        if (player == null)
        {
            return;
        }

        Vector3 pushDir = (player.transform.position - transform.position).normalized;
        pushDir.y = 0.5f;
        pushDir.Normalize();

        Vector3 force = pushDir * pushForce;
        detectionSuppressedUntil = Time.time + Mathf.Max(0f, postPushDetectionCooldown);

        FirstPersonNetworkState fps = player.gameObject.GetComponent<FirstPersonNetworkState>();
        if (fps != null)
        {
            fps.ReceivePushOnServer(force);
        }
        else
        {
            PlayerController pc = player.gameObject.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.ApplyKnockback(force);
            }
            else
            {
                Rigidbody rb = player.gameObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(force, ForceMode.Impulse);
                }
            }
        }

        Rpc_PlayPushAnimation();
        _currentDetectionTarget = null;
        StartCoroutine(WaitAfterInvestigation());
    }

    [ObserversRpc]
    private void Rpc_PlayPushAnimation()
    {
        if (animator != null)
        {
        }
    }

    private IEnumerator WaitAtWaypoint()
    {
        if (isWaiting)
        {
            yield break;
        }

        isWaiting = true;
        CurrentState.value = VigiaState.Waiting;

        float waitTime = 1f;
        if (waypoints != null && currentWaypointIndex >= 0 && currentWaypointIndex < waypoints.Count)
        {
            Waypoint currentWaypoint = waypoints[currentWaypointIndex];
            if (currentWaypoint != null)
            {
                waitTime = currentWaypoint.waitTime;
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (waypoints != null && waypoints.Count > 0)
        {
            if (randomPatrol && waypoints.Count > 1)
            {
                int nextIndex = currentWaypointIndex;
                while (nextIndex == currentWaypointIndex)
                {
                    nextIndex = Random.Range(0, waypoints.Count);
                }
                currentWaypointIndex = nextIndex;
            }
            else
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            }

            GoToNextWaypoint();
        }

        isWaiting = false;
    }

    private IEnumerator WaitAfterInvestigation()
    {
        if (isWaiting)
        {
            yield break;
        }

        isWaiting = true;
        CurrentState.value = VigiaState.Waiting;

        yield return new WaitForSeconds(4f);

        GoToNextWaypoint();
        isWaiting = false;
    }

    private void GoToNextWaypoint()
    {
        _patrolInitialized = false;
        if (!EnsureAgentReadyForNavigation())
        {
            return;
        }

        if (waypoints == null || waypoints.Count == 0)
        {
            return;
        }

        if (currentWaypointIndex < 0 || currentWaypointIndex >= waypoints.Count)
        {
            return;
        }

        Waypoint nextWaypoint = waypoints[currentWaypointIndex];
        if (nextWaypoint == null)
        {
            return;
        }

        CurrentState.value = VigiaState.Wandering;
        agent.isStopped = false;
        agent.speed = wanderSpeed;
        agent.SetDestination(nextWaypoint.transform.position);
        _patrolInitialized = true;
    }

    private void UpdateAnimations()
    {
        if (animator == null)
        {
            return;
        }

        float currentSpeed;
        if (isServer && HasValidNavMeshAgent())
        {
            currentSpeed = agent.velocity.magnitude;
        }
        else
        {
            currentSpeed = SyncedSpeed.value;
        }

        float targetAnimValue;
        float maxSpeed;
        if (CurrentState.value == VigiaState.Investigating)
        {
            targetAnimValue = 1.5f;
            maxSpeed = investigateSpeed;
        }
        else
        {
            targetAnimValue = 0.5f;
            maxSpeed = wanderSpeed;
        }

        float magnitude = 0f;
        if (maxSpeed > 0.01f)
        {
            magnitude = (currentSpeed / maxSpeed) * targetAnimValue;
        }

        animator.SetFloat(inputXHash, 0f);
        animator.SetFloat(inputYHash, magnitude);
        animator.SetFloat(inputMagnitudeHash, magnitude);
        animator.SetBool(isIdlingHash, currentSpeed < 0.1f);
        animator.SetBool(isGroundedHash, true);
    }

    private void UpdateIndicatorLight()
    {
        if (indicatorLight == null)
        {
            return;
        }

        if (CurrentState.value == VigiaState.Investigating)
        {
            indicatorLight.color = Color.red;
        }
        else
        {
            indicatorLight.color = Color.green;
        }
    }

    private void OnDrawGizmos()
    {
        if (showPatrolPath && waypoints != null && waypoints.Count > 1)
        {
            Gizmos.color = new Color(0, 1, 1, 0.5f);
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;

                Vector3 current = waypoints[i].transform.position;
                Vector3 next;

                if (randomPatrol)
                {
                    Gizmos.DrawWireSphere(current, 0.3f);
                }
                else
                {
                    int nextIndex = (i + 1) % waypoints.Count;
                    if (waypoints[nextIndex] != null)
                    {
                        next = waypoints[nextIndex].transform.position;
                        Gizmos.DrawLine(current + Vector3.up * 0.2f, next + Vector3.up * 0.2f);
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.3f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.6f, 0.2f);

        Vector3 forward = transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2f, 0) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up * 1.6f, transform.position + Vector3.up * 1.6f + leftDir * viewDistance);
        Gizmos.DrawLine(transform.position + Vector3.up * 1.6f, transform.position + Vector3.up * 1.6f + rightDir * viewDistance);

        if (CurrentState.value == VigiaState.Investigating)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, investigationTarget);
            Gizmos.DrawWireCube(investigationTarget, Vector3.one * 0.5f);
        }
    }
}
