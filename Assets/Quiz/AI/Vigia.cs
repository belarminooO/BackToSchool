using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PurrNet;
using MetaVoiceChat.AudioFilters; // Para o NoiseGate

/// <summary>
/// Comportamento do Vigia (watchman).
/// Vagadeia pela sala entre waypoints, monitoriza visão e ruído.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Vigia : NetworkBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("Lista de waypoints para patrulha. Se vazia, tenta encontrar na cena.")]
    public List<VigilaWaypoint> waypoints;
    [Tooltip("Se ativo, o vigia escolhe waypoints aleatoriamente em vez de seguir a ordem.")]
    public bool randomPatrol = false;
    public float wanderSpeed = 2.5f;
    public float investigateSpeed = 4.5f;
    public float stopDistance = 0.8f;

    [Header("Vision Settings")]
    public float viewDistance = 12f;
    [Range(0, 180)]
    public float viewAngle = 80f;
    public LayerMask visionMask; // O que bloqueia a visão (ex: Default)
    public LayerMask playerMask; // O que o vigia procura (ex: Player)

    [Header("Hearing Settings")]
    [Tooltip("Volume em dB acima do qual o vigia ouve o jogador.")]
    public float noiseThreshold = -35f;
    public float hearingRadius = 15f;

    [Header("Debug Visuals")]
    [Tooltip("Mostra o cone de visão e o raio de audição.")]
    public bool showDebugGizmos = true;
    [Tooltip("Mostra a linha de ligação entre os waypoints.")]
    public bool showPatrolPath = true;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private Vector3 investigationTarget;

    private IPlayerController _currentDetectionTarget;
    private float _gracePeriodTimer = 0f;
    private GeneratedFootstepEmitter _footstepEmitter;
    private VigiaState _lastAudioState;
    private bool _audioStateInitialized;
    private bool _patrolInitialized;

    private const float NavMeshSnapRadius = 5f;

    public SyncVar<VigiaState> CurrentState = new SyncVar<VigiaState>(VigiaState.Wandering);
    public SyncVar<float> SyncedSpeed = new SyncVar<float>(0f);

    // Hashes de animação
    private static int inputXHash = Animator.StringToHash("inputX");
    private static int inputYHash = Animator.StringToHash("inputY");
    private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
    private static int isIdlingHash = Animator.StringToHash("isIdling");
    private static int isGroundedHash = Animator.StringToHash("isGrounded");

    [Header("Jail Settings")]
    public float jailDetectionTime = 2f;
    public float jailDetectionRadius = 2.5f; 
    public float voiceGracePeriod = 0.5f;

    [Header("Indicator Light")]
    [Tooltip("Light that turns green by default and red when investigating.")]
    public Light indicatorLight;

    [Header("Detection Bar UI")]
    public GameObject detectionBarRoot;
    public UnityEngine.UI.Slider detectionSlider;
    public SyncVar<float> DetectionProgress = new SyncVar<float>(0f);

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
        GeneratedQuizSfx.EnsureExists();

        NpcVisualBootstrap.Configure(gameObject, animator);

        if (agent != null)
            agent.enabled = false;

        _footstepEmitter = GetComponent<GeneratedFootstepEmitter>();
        if (_footstepEmitter == null)
            _footstepEmitter = gameObject.AddComponent<GeneratedFootstepEmitter>();
        _footstepEmitter.ConfigureForVigia();

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

        _lastAudioState = CurrentState.value;
        _audioStateInitialized = true;
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
            UpdateDetectionUI();
            UpdateIndicatorLight();
            UpdateAudioFeedback();
            return;
        }

        if (isServer) 
        {
            if (IsActiveDetectionState())
            {
                UpdatePassivePerception();
                UpdateJailDetection();
            }
            else
            {
                DecayDetectionProgress(); 
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
        UpdateDetectionUI();
        UpdateIndicatorLight();
        UpdateAudioFeedback();
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

    private void UpdatePassivePerception()
    {
        CheckVision();
        CheckHearing();
    }

    private void CheckVision()
    {
        int searchMask = playerMask.value | LayerMask.GetMask("Default");
        Collider[] targets = Physics.OverlapSphere(transform.position, viewDistance, searchMask);

        foreach (var target in targets)
        {
            if (!target.CompareTag("Player"))
                continue;

            var p = target.GetComponentInParent<IPlayerController>();
            if (p == null)
                continue;

            if (p.isJailed)
                continue;

            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            if (angleToTarget < viewAngle / 2f)
            {
                Vector3 startPos = transform.position + Vector3.up * 1.6f;
                Vector3 endPos = target.transform.position + Vector3.up * 1.6f;
                
                if (!Physics.Linecast(startPos, endPos, visionMask))
                {
                    TriggerInvestigation(target.transform.position);
                    return;
                }
            }
        }
    }

    private void CheckHearing()
    {
        PlayerVoiceState[] playerVoices = Object.FindObjectsByType<PlayerVoiceState>(FindObjectsSortMode.None);         
        foreach (var playerVoice in playerVoices)
        {
            var p = playerVoice.GetComponentInParent<IPlayerController>();
            if (p != null && p.isJailed) 
                continue;

            float dist = Vector3.Distance(transform.position, playerVoice.transform.position);
            if (dist <= hearingRadius)
            {
                if (playerVoice.isTalking.value)
                {
                    TriggerInvestigation(playerVoice.transform.position);
                    return;
                }
            }
        }
    }

    private void TriggerInvestigation(Vector3 origin)
    {
        if (!EnsureAgentReadyForNavigation())
            return;

        if (CurrentState.value == VigiaState.Investigating && Vector3.Distance(agent.destination, origin) < 0.5f)
            return;

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
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!HasActiveJailCandidate())
                    {
                        StartCoroutine(WaitAfterInvestigation());
                    }
                }
                break;
        }
    }

    private bool HasActiveJailCandidate()
    {
        var nearbyPlayers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>();
        foreach (var player in nearbyPlayers)
        {
            if (player == null || player.isJailed)
                continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= jailDetectionRadius)
            {
                var voiceState = player.gameObject.GetComponent<PlayerVoiceState>();
                if (voiceState != null && voiceState.isTalking.value)
                {
                    return true;
                }
            }
        }
        return false;
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

    private void UpdateJailDetection()
    {
        if (CurrentState.value != VigiaState.Investigating)
        {
            DecayDetectionProgress();
            return;
        }

        var nearbyPlayers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>();
        IPlayerController talkingPlayer = null;

        foreach (var player in nearbyPlayers)
        {
            if (player == null || player.isJailed)
                continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= jailDetectionRadius)
            {
                var voiceState = player.gameObject.GetComponent<PlayerVoiceState>();
                if (voiceState != null && voiceState.isTalking.value)
                {
                    talkingPlayer = player;
                    break;
                }
            }
        }

        if (talkingPlayer != null)
        {
            _gracePeriodTimer = 0f;
            _currentDetectionTarget = talkingPlayer;
            DetectionProgress.value = Mathf.Min(DetectionProgress.value + Time.deltaTime, jailDetectionTime);

            if (DetectionProgress.value >= jailDetectionTime)
            {
                SendPlayerToJail(_currentDetectionTarget);
                DetectionProgress.value = 0f;
                _currentDetectionTarget = null;
            }
        }
        else if (DetectionProgress.value > 0f)
        {
            _gracePeriodTimer += Time.deltaTime;

            if (_gracePeriodTimer > voiceGracePeriod)
                DecayDetectionProgress();
        }
    }

    private void DecayDetectionProgress()
    {
        if (DetectionProgress.value > 0f)
        {
            DetectionProgress.value = Mathf.MoveTowards(DetectionProgress.value, 0f, Time.deltaTime);
            if (DetectionProgress.value <= 0f)
            {
                _currentDetectionTarget = null;
                _gracePeriodTimer = 0f;
            }
        }
    }
    
    private void SendPlayerToJail(IPlayerController player) 
    {
        if (player == null) 
            return;

        int currentRound = QuizGameManager.instance != null ? QuizGameManager.instance.currentRound.value : 1;
        Vector3 jailPos = Vector3.zero;
        if (QuizGameManager.instance != null && QuizGameManager.instance.spawnManager != null)
        {
            jailPos = QuizGameManager.instance.spawnManager.GetJailPosition(currentRound);
        }

        if (jailPos == Vector3.zero)
        {
            Debug.LogError($"[Vigia] No Jail Position found for Round {currentRound}!");
            return;
        }

        jailPos += new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
        
        player.TeleportTo(jailPos);
        player.isJailed = true;

        var network = player.gameObject.GetComponent<NetworkIdentity>();
        if (network != null && network.owner.HasValue)
            Rpc_JailPlayer(network.owner.Value, jailPos);

        StopAllCoroutines();
        isWaiting = false;
        GoToNextWaypoint();
        Debug.Log($"[Vigia] Jailed {player.gameObject.name}!");
    }

    [TargetRpc]
    private void Rpc_JailPlayer(PlayerID player, Vector3 jailPosition)
    {
        var allPlayers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>();
        foreach (var p in allPlayers)
        {
            var net = p.gameObject.GetComponent<NetworkIdentity>();
            if (net != null && net.isOwner)
            {
                p.TeleportTo(jailPosition);
                p.isJailed = true;
                break;
            }
        }
    }

    private void UpdateIndicatorLight()
    {
        if (indicatorLight == null) 
            return;
        if (CurrentState.value == VigiaState.Investigating)
        {
            indicatorLight.color = Color.red;
        } else
        {
            indicatorLight.color = Color.green;
        }
    }

    private void UpdateDetectionUI()
    {
        if (detectionBarRoot == null) return;

        bool showBar = DetectionProgress.value > 0.001f;
        if (detectionBarRoot.activeSelf != showBar)
            detectionBarRoot.SetActive(showBar);

        if (showBar)
        {
            if (detectionSlider != null)
                detectionSlider.value = DetectionProgress.value / jailDetectionTime;

            if (Camera.main != null)
            {
                detectionBarRoot.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    private void UpdateAudioFeedback()
    {
        if (_footstepEmitter != null)
        {
            _footstepEmitter.SetVigiaMovementMode(CurrentState.value == VigiaState.Investigating);
            float speed = isServer && HasValidNavMeshAgent() ? agent.velocity.magnitude : SyncedSpeed.value;
            _footstepEmitter.Tick(true, speed, Time.deltaTime);
        }

        if (!_audioStateInitialized)
        {
            _lastAudioState = CurrentState.value;
            _audioStateInitialized = true;
            return;
        }

        if (_lastAudioState == CurrentState.value)
            return;

        if (CurrentState.value == VigiaState.Investigating)
        {
            GeneratedQuizSfx.PlayVigiaAlert(transform.position + Vector3.up * 1.6f);
        }

        _lastAudioState = CurrentState.value;
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

        Gizmos.color = new Color(0, 0, 1, 0.2f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
        
        if (CurrentState.value == VigiaState.Investigating)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, investigationTarget);
            Gizmos.DrawWireCube(investigationTarget, Vector3.one * 0.5f);
        }
    }
}
