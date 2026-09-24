using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PurrNet;
using GinjaGaming.FinalCharacterController;

/// <summary>
/// Comportamento do ColegaEmpurrar.
/// Vagadeia pela sala entre waypoints, monitoriza visão.
/// Empurra o jogador caso o veja na mesma rampa.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ColegaEmpurrar : NetworkBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("Lista de waypoints para patrulha. Se vazia, tenta encontrar na cena.")]
    public List<VigilaWaypoint> waypoints;
    [Tooltip("Se ativo, escolhe waypoints aleatoriamente em vez de seguir a ordem.")]
    public bool randomPatrol = false;
    public float wanderSpeed = 2.5f;
    public float investigateSpeed = 4.5f;
    public float stopDistance = 0.8f;

    [Header("Vision Settings")]
    public float viewDistance = 12f;
    [Range(0, 180)]
    public float viewAngle = 80f;
    public LayerMask visionMask; // O que bloqueia a visão
    public LayerMask playerMask; // O que o NPC procura (ex: Player)

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

    // Hashes de animação
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
            EnsureAgentReadyForNavigation();
    }

    private bool IsActiveDetectionState()
    {
        if (QuizGameManager.instance == null || QuizGameManager.instance.currentStateName == null) 
            return false;

        return QuizGameManager.instance.currentStateName.value == "QuestionStateNode";
    }

    private void Update()
    {
        bool agentReady = isServer ? EnsureAgentReadyForNavigation() : HasValidNavMeshAgent();

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
            // O Colega vai sempre verificar visão se a ronda decorre e não estiver a focar-se em empurrar ou preso.
            // Para debug, vamos verificar sempre
            bool isDetectionActive = true; 
            if (QuizGameManager.instance != null && QuizGameManager.instance.currentStateName != null)
            {
                isDetectionActive = (QuizGameManager.instance.currentStateName.value == "QuestionStateNode");
            }
            
            if (CurrentState.value == VigiaState.Wandering || CurrentState.value == VigiaState.Waiting)
            {
                if (Time.time >= detectionSuppressedUntil)
                    CheckVision();
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
            playerMask = LayerMask.GetMask("Player");
        if (visionMask.value == 0) 
            visionMask = LayerMask.GetMask("Default", "Environment", "Ground");
        
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
        VigilaWaypoint[] foundWps = Object.FindObjectsByType<VigilaWaypoint>(FindObjectsSortMode.InstanceID);
        waypoints = new List<VigilaWaypoint>(foundWps);
        waypoints.Sort((a, b) => string.Compare(a.name, b.name));
    }

    private bool IsOnSameRamp(Vector3 targetPos)
    {
        // Verificar usando mesh chao embaixo deles
        bool myHitSuccess = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit myHit, 5f, visionMask);
        bool targetHitSuccess = Physics.Raycast(targetPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit targetHit, 5f, visionMask);

        if (myHitSuccess && targetHitSuccess)
        {
            // Se estão sob o mesmo objeto de chão, estão na mesma rampa.
            bool sameObj = myHitSuccess && targetHitSuccess && myHit.collider.gameObject == targetHit.collider.gameObject;
            Debug.Log($"[IsOnSameRamp] Raycast success. Same Ramp: {sameObj} (My: {myHit.collider.name}, Target: {targetHit.collider.name})");
            return sameObj;
        }
        
        Debug.Log($"[IsOnSameRamp] Raycast failed (Me: {myHitSuccess}, Target: {targetHitSuccess}). Using X distance. Difference: {Mathf.Abs(transform.position.x - targetPos.x)}");
        // Tolerância (fallbak): baseada na distância lateral do eixo X porque as rampas são paralelas ao eixo Z
        return Mathf.Abs(transform.position.x - targetPos.x) < 3.5f;
    }

    private void CheckVision()
    {
        // Alterado para incluir layer Default temporariamente, porque o prefab do Player está na layer Default!
        int searchMask = playerMask.value | LayerMask.GetMask("Default");
        Collider[] targets = Physics.OverlapSphere(transform.position, viewDistance, searchMask);
        
        // Debug.Log($"[CheckVision] Objetos encontrados: {targets.Length}");

        foreach (var target in targets)
        {
            if (!target.CompareTag("Player")) continue;

            var p = target.GetComponentInParent<IPlayerController>();
            if (p != null && p.isJailed) 
                continue;

            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            if (angleToTarget < viewAngle / 2f)
            {
                Vector3 startPos = transform.position + Vector3.up * 1.6f;
                Vector3 endPos = target.transform.position + Vector3.up * 1.6f;
                
                bool hitWall = Physics.Linecast(startPos, endPos, out RaycastHit hit, visionMask);
                
                // Se não acertou em nada, ou se a coisa em que acertou fôr o próprio jogador, a visão está limpa!
                if (!hitWall || hit.transform.root == target.transform.root)
                {
                    if (IsOnSameRamp(target.transform.position))
                    {
                        Debug.Log("[CheckVision] Player DETECTED on same ramp!");
                        TriggerInvestigation(target.transform.position, p);
                        return;
                    }
                    else
                    {
                        Debug.Log($"[CheckVision] Player is in LOS but NOT on same ramp! Distance: {Vector3.Distance(transform.position, target.transform.position)}");
                    }
                }
                else
                {
                    Debug.Log($"[CheckVision] Line of sight blocked by: {hit.collider.name}");
                }
            }
            else
            {
                // Debug.Log($"[CheckVision] Player out of view cone. Angle: {angleToTarget}");
            }
        }
    }

    private void TriggerInvestigation(Vector3 origin, IPlayerController playerTarget)
    {
        if (!EnsureAgentReadyForNavigation())
            return;

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
                    // Avalia se o jogador saiu da rampa atual ou fugiu com sucesso
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
                    // Perdeu alvo (null ou ja na prisao). Wait and go back.
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
        if (player == null) return;
        
        Debug.Log($"[Colega] PUSHING THE PLAYER: {player.gameObject.name}!");
        Vector3 pushDir = (player.transform.position - transform.position).normalized;
        pushDir.y = 0.5f; // Um pouco de força vertical
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
                if (rb != null) rb.AddForce(force, ForceMode.Impulse);
            }
        }

        // Tenta rodar animação via rede usando ObserversRpc
        Rpc_PlayPushAnimation();

        // Alvo solto, volta para patrulhar.
        _currentDetectionTarget = null;
        StartCoroutine(WaitAfterInvestigation());
    }

    [ObserversRpc]
    private void Rpc_PlayPushAnimation()
    {
        if (animator != null)
        {
            // Opcional caso venham a adicionar a animação "Push" ao Animator
            // animator.SetTrigger("push");
        }
    }

    private IEnumerator WaitAtWaypoint()
    {
        if (isWaiting) yield break;
        isWaiting = true;
        CurrentState.value = VigiaState.Waiting;

        float waitTime = 1f;
        if (waypoints != null && currentWaypointIndex < waypoints.Count)
        {
            waitTime = waypoints[currentWaypointIndex].waitTime;
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
        if (isWaiting) yield break;
        isWaiting = true;
        CurrentState.value = VigiaState.Waiting;

        yield return new WaitForSeconds(4f);

        GoToNextWaypoint();
        isWaiting = false;
    }

    private void GoToNextWaypoint()
    {
        _patrolInitialized = false;
        if (!EnsureAgentReadyForNavigation() || waypoints == null || waypoints.Count == 0) return;

        CurrentState.value = VigiaState.Wandering;
        agent.isStopped = false;
        agent.speed = wanderSpeed;
        agent.SetDestination(waypoints[currentWaypointIndex].transform.position);
        _patrolInitialized = true;
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        float currentSpeed = isServer && HasValidNavMeshAgent() ? agent.velocity.magnitude : SyncedSpeed.value;
        float targetAnimValue = (CurrentState.value == VigiaState.Investigating) ? 1.5f : 0.5f;
        float maxSpeed = (CurrentState.value == VigiaState.Investigating) ? investigateSpeed : wanderSpeed;
        float magnitude = (maxSpeed > 0.01f) ? (currentSpeed / maxSpeed) * targetAnimValue : 0f;

        animator.SetFloat(inputXHash, 0f);
        animator.SetFloat(inputYHash, magnitude);
        animator.SetFloat(inputMagnitudeHash, magnitude);
        animator.SetBool(isIdlingHash, currentSpeed < 0.1f);
        animator.SetBool(isGroundedHash, true);
    }

    private void UpdateIndicatorLight()
    {
        if (indicatorLight == null) 
            return;
        
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
