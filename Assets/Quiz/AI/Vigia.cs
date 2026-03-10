using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using MetaVoiceChat.AudioFilters; // Para o NoiseGate

/// <summary>
/// Comportamento do Vigia (watchman).
/// Vagadeia pela sala entre waypoints, monitoriza visão e ruído.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[HelpURL("https://github.com/migboss/BackToSchool")]
public class Vigia : MonoBehaviour
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
    public float noiseThreshold = -45f;
    public float hearingRadius = 15f;

    [Header("Debug Visuals")]
    [Tooltip("Mostra o cone de visão e o raio de audição.")]
    public bool showDebugGizmos = true;
    [Tooltip("Mostra a linha de ligação entre os waypoints.")]
    public bool showPatrolPath = true;

    [Header("Status (Read Only)")]
    [SerializeField] private VigiaState _currentState = VigiaState.Wandering;
    public VigiaState CurrentState => _currentState;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private Vector3 investigationTarget;

    // Hashes de animação (FinalCharacterController Part 7)
    private static int inputXHash = Animator.StringToHash("inputX");
    private static int inputYHash = Animator.StringToHash("inputY");
    private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
    private static int isIdlingHash = Animator.StringToHash("isIdling");
    private static int isGroundedHash = Animator.StringToHash("isGrounded");

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

        // Força o objeto a não ser estático para garantir que o NavMeshAgent funciona
        gameObject.isStatic = false;

        if (waypoints == null || waypoints.Count == 0)
        {
            FindWaypointsInScene();
        }
    }

    private void Start()
    {
        if (waypoints != null && waypoints.Count > 0)
        {
            GoToNextWaypoint();
        }
    }

    private void Update()
    {
        // O vigia está sempre atento, mesmo quando parado ou a vaguear.
        UpdatePassivePerception();
        UpdateStateMachine();
        UpdateAnimations();
    }

    private void OnValidate()
    {
        // Auto-configuração de layers no editor para facilitar a vida do utilizador
        if (playerMask.value == 0) playerMask = LayerMask.GetMask("Player");
        if (visionMask.value == 0) visionMask = LayerMask.GetMask("Default", "Environment", "Ground");
        
        // Configurações sugeridas para o NavMeshAgent
        NavMeshAgent nav = GetComponent<NavMeshAgent>();
        if (nav != null)
        {
            nav.speed = wanderSpeed;
            nav.stoppingDistance = stopDistance;
            nav.acceleration = 8f;
            nav.angularSpeed = 120f;
        }
    }

    public void FindWaypointsInScene()
    {
        VigilaWaypoint[] foundWps = Object.FindObjectsByType<VigilaWaypoint>(FindObjectsSortMode.InstanceID);
        waypoints = new List<VigilaWaypoint>(foundWps);
        waypoints.Sort((a, b) => string.Compare(a.name, b.name));
    }

    private void UpdatePassivePerception()
    {
        // Se já estivermos a investigar algo muito perto, não precisamos de procurar novamente
        // Mas se estivermos a patrulhar, qualquer som ou visão interrompe.
        CheckVision();
        CheckHearing();
    }

    private void CheckVision()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, viewDistance, playerMask);

        foreach (var target in targets)
        {
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            if (angleToTarget < viewAngle / 2f)
            {
                // Raycast para verificar se não há paredes no meio
                // Offset de 1.5m para os olhos
                Vector3 startPos = transform.position + Vector3.up * 1.6f;
                Vector3 endPos = target.transform.position + Vector3.up * 1.6f;
                
                if (!Physics.Linecast(startPos, endPos, visionMask))
                {
                    Debug.Log($"<color=red>Vigia:</color> Jogador {target.name} visto!");
                    TriggerInvestigation(target.transform.position);
                    return;
                }
            }
        }
    }

    private void CheckHearing()
    {
        // Otimização: Apenas verifica se houver fontes de ruído
        NoiseGate[] noiseSources = Object.FindObjectsByType<NoiseGate>(FindObjectsSortMode.None);
        
        foreach (var source in noiseSources)
        {
            float dist = Vector3.Distance(transform.position, source.transform.position);
            if (dist <= hearingRadius)
            {
                if (source.currentVolumeDB > noiseThreshold)
                {
                    Debug.Log($"<color=blue>Vigia:</color> Ruído detetado em {source.name}!");
                    TriggerInvestigation(source.transform.position);
                    return;
                }
            }
        }
    }

    private void TriggerInvestigation(Vector3 origin)
    {
        // Se já estivermos a investigar muito perto desta origem, ignora para não "tremer"
        if (_currentState == VigiaState.Investigating && Vector3.Distance(agent.destination, origin) < 2f)
            return;

        investigationTarget = origin;
        _currentState = VigiaState.Investigating;
        StopAllCoroutines();
        isWaiting = false;
        
        agent.isStopped = false;
        agent.speed = investigateSpeed;
        agent.SetDestination(investigationTarget);
    }

    private void UpdateStateMachine()
    {
        switch (_currentState)
        {
            case VigiaState.Wandering:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    StartCoroutine(WaitAtWaypoint());
                }
                break;

            case VigiaState.Waiting:
                // Aguarda na corrotina
                break;

            case VigiaState.Investigating:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    StartCoroutine(WaitAfterInvestigation());
                }
                break;
        }
    }

    private IEnumerator WaitAtWaypoint()
    {
        if (isWaiting) yield break;
        isWaiting = true;
        _currentState = VigiaState.Waiting;

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
        _currentState = VigiaState.Waiting;

        // Fica a "scrutar" o ambiente
        yield return new WaitForSeconds(4f);

        GoToNextWaypoint();
        isWaiting = false;
    }

    private void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        _currentState = VigiaState.Wandering;
        agent.isStopped = false;
        agent.speed = wanderSpeed;
        agent.SetDestination(waypoints[currentWaypointIndex].transform.position);
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        float currentSpeed = agent.velocity.magnitude;
        // Normaliza a magnitude baseado na velocidade máxima atual
        float maxSpeed = (_currentState == VigiaState.Investigating) ? investigateSpeed : wanderSpeed;
        float magnitude = Mathf.Clamp01(currentSpeed / maxSpeed);

        // Parâmetros do FinalCharacterController Part 7
        animator.SetFloat(inputXHash, 0f);
        animator.SetFloat(inputYHash, magnitude);
        animator.SetFloat(inputMagnitudeHash, magnitude);
        animator.SetBool(isIdlingHash, currentSpeed < 0.1f);
        animator.SetBool(isGroundedHash, true);
    }

    private void OnDrawGizmos()
    {
        // Desenha o caminho da patrulha (sempre visível se ativado)
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
                    // No modo aleatório, apenas desenha pequenas esferas nos pontos
                    Gizmos.DrawWireSphere(current, 0.3f);
                }
                else
                {
                    // Desenha linha para o próximo WP
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

        // Cone de Visão
        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.3f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.6f, 0.2f);
        
        Vector3 forward = transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2f, 0) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up * 1.6f, transform.position + Vector3.up * 1.6f + leftDir * viewDistance);
        Gizmos.DrawLine(transform.position + Vector3.up * 1.6f, transform.position + Vector3.up * 1.6f + rightDir * viewDistance);

        // Raio de Escuta
        Gizmos.color = new Color(0, 0, 1, 0.2f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
        
        if (_currentState == VigiaState.Investigating)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, investigationTarget);
            Gizmos.DrawWireCube(investigationTarget, Vector3.one * 0.5f);
        }
    }
}

