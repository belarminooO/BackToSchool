using UnityEngine;
using PurrNet;
using GinjaGaming.FinalCharacterController;

public class QuizGameManager : NetworkBehaviour
{
    public static QuizGameManager instance { get; private set; }

    private IGameState currentState;

    public FreezeState freezeState { get; private set; }
    public QuestionState questionState { get; private set; }
    public ObstacleRaceState obstacleRaceState { get; private set; }
    public ChangeRoundState changeRoundState { get; private set; }

    public SyncVar<float> maxTimer = new SyncVar<float>();
    public SyncVar<int> currentRound = new SyncVar<int>();
    public SyncVar<float> currentTimer = new SyncVar<float>();
    public SyncVar<int> totalRoundsSettings = new SyncVar<int>();
    public SyncVar<int> roundDurationSettings = new SyncVar<int>();

    [Header("Managers")]
    public ScoreManager scoreManager;
    public SpawnManager spawnManager;
    public GenerateQuizJSON quizData;

    [System.NonSerialized] public string localStateName = "FreezeState";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        freezeState = new FreezeState();
        questionState = new QuestionState();
        obstacleRaceState = new ObstacleRaceState();
        changeRoundState = new ChangeRoundState();
    }

    protected override void OnSpawned()
    {
        if(isServer) {
            totalRoundsSettings.value = PlayerPrefs.GetInt("Quiz_RoundNumber", 3);
            roundDurationSettings.value = PlayerPrefs.GetInt("Quiz_QuestionTimer", 30);
            currentRound.value = 1;
            currentTimer.value = 0f;

            freezeState.SetIsGenerating(true);
            ChangeState(freezeState);

            // if (scoreManager) {
            //     scoreManager.ChooseTeams(); 
            // }
        }
    }

    public void OnQuizGenerationComplete()
    {
        Debug.Log("[Server] Quiz generation complete!");

        if (scoreManager != null) {
            scoreManager.ChooseTeams();

            StationManager[] allRoomManagers = Object.FindObjectsByType<StationManager>(FindObjectsSortMode.None);

            foreach (StationManager manager in allRoomManagers) {
                if (manager.roundNumber == 1)
                {
                    manager.AssignStations(new System.Collections.Generic.List<MyCharacterController>(scoreManager.allPlayers));
                }
            }
        }
        
        freezeState.SetIsGenerating(false);
        ChangeState(freezeState);
    }

    void Update()
    {
        if (!isServer || currentState == null) {
            return;
        }
        
        currentState.UpdateState(this);
    }

    public void ChangeState(IGameState newState)
    {
        if (currentState != null) {
            currentState.ExitState(this);
        }

        currentState = newState;
        currentState.EnterState(this);

        RPC_NotifyStateChange(newState.GetType().Name);
    }

    [ObserversRpc]
    private void RPC_NotifyStateChange(string stateName)
    {
        localStateName = stateName;
    }

    // [ObserversRpc]
    // public void RPC_MoveCanvasToClassroom(int roundNum)
    // {
    //     GameObject roundObj = GameObject.Find("Round");
    //     GameObject currentClassroom = GameObject.Find("Classroom " + roundNum);
    //     if (roundObj != null && currentClassroom != null)
    //     {
    //         Transform boardPos = currentClassroom.transform.Find("Round UI Spawnpoint");
    //         if (boardPos != null)
    //         {
    //             roundObj.transform.position = boardPos.position;
    //             roundObj.transform.rotation = boardPos.rotation;
    //         }
    //         else
    //         {
    //             roundObj.transform.position = currentClassroom.transform.position + new Vector3(0, 3f, 5f);
    //         }
    //     }
    // }
}
