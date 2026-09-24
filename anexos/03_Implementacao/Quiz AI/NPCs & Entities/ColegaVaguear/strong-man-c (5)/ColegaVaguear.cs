using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using PurrNet;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ColegaVaguear : NetworkBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("Lista de waypoints para patrulha. Se vazia, tenta encontrar na cena.")]
    public List<Waypoint> waypoints;
    [Tooltip("Se ativo, escolhe waypoints aleatoriamente em vez de seguir a ordem.")]
    public bool randomPatrol = false;
    public float wanderSpeed = 2.5f;
    public float stopDistance = 0.8f;

    [Header("Debug Visuals")]
    [Tooltip("Mostra a linha de ligação entre os waypoints.")]
    public bool showPatrolPath = true;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;

    public SyncVar<VigiaState> CurrentState = new SyncVar<VigiaState>(VigiaState.Wandering);
    public SyncVar<float> SyncedSpeed = new SyncVar<float>(0f);

    private bool _patrolInitialized;

    private const float NavMeshSnapRadius = 5f;

    private static int inputXHash = Animator.StringToHash("inputX");
    private static int inputYHash = Animator.StringToHash("inputY");
    private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
    private static int isIdlingHash = Animator.StringToHash("isIdling");
    private static int isGroundedHash = Animator.StringToHash("isGrounded");

    public enum VigiaState
    {
        Wandering,
        Waiting
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        NpcVisualBootstrap.Configure(gameObject, animator);

        if (agent != null)
            agent.enabled = false;

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
            return;
        }

        if (isServer) 
        {
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
    }

    private void OnValidate()
    {
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

    private void UpdateStateMachine()
    {
        if (!_patrolInitialized || !HasValidNavMeshAgent())
        {
            return;
        }

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

        float magnitude = 0f;
        if (wanderSpeed > 0.01f)
        {
            magnitude = (currentSpeed / wanderSpeed) * 0.5f;
        }

        animator.SetFloat(inputXHash, 0f);
        animator.SetFloat(inputYHash, magnitude);
        animator.SetFloat(inputMagnitudeHash, magnitude);
        animator.SetBool(isIdlingHash, currentSpeed < 0.1f);
        animator.SetBool(isGroundedHash, true);
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
        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.3f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.6f, 0.2f);
    }
}
