using UnityEngine;
using PurrNet;
using PurrNet.StateMachine;
using System.Linq;

public class QuizGameManager : NetworkBehaviour
{
    public static QuizGameManager instance { get; private set; }

    // private IGameState currentState;

    [Header("Lobby Settings")]
    public int requiredPlayers = 2; // Connect with Steam Lobby System

    [Header("PurrNet State Machine")]
    public PurrNet.StateMachine.StateMachine stateMachine;
    public StateNode waitingState;
    public StateNode freezeState; 
    public StateNode questionState;
    public StateNode obstacleRaceState;
    public StateNode changeRoundState;

    // public WaitingState waitingState { get; private set; }
    // public FreezeState freezeState { get; private set; }
    // public QuestionState questionState { get; private set; }
    // public ObstacleRaceState obstacleRaceState { get; private set; }
    // public ChangeRoundState changeRoundState { get; private set; }

    [Header("Game Settings (SyncVars)")]
    public SyncVar<float> maxTimer = new SyncVar<float>();
    public SyncVar<int> currentRound = new SyncVar<int>();
    public SyncVar<float> currentTimer = new SyncVar<float>();
    public SyncVar<int> totalRoundsSettings = new SyncVar<int>();
    public SyncVar<int> roundDurationSettings = new SyncVar<int>();
    public SyncVar<string> currentStateName = new SyncVar<string>("WaitingState");

    [Header("Managers")]
    public ScoreManager scoreManager;
    public SpawnManager spawnManager;
    public GenerateQuizJSON quizData;

    public bool quizReady { get; private set; } = false;

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

    }

    // public void StartGame()
    // {
    //     if (!isServer) 
    //         return;

    //     if (!_quizReady)
    //     {
    //         Debug.LogWarning("[Server] Quiz not ready yet! Please wait for generation to complete.");
    //         return;
    //     }

    //     if (scoreManager != null)
    //     {
    //         scoreManager.ChooseTeams();

    //         StationManager[] allRoomManagers = Object.FindObjectsByType<StationManager>(FindObjectsSortMode.None);
    //         foreach (StationManager rm in allRoomManagers)
    //         {
    //             if (rm.roundNumber == 1)
    //                 rm.AssignStations(new System.Collections.Generic.List<IPlayerController>(scoreManager.allPlayers));
    //         }
    //     }

    //     ChangeState(freezeState);
    // }

    public void OnQuizGenerationComplete()
    {
        Debug.Log("[GameManager] Quiz ready to start!");
        quizReady = true;
    }

    // void Update()
    // {
    //     if (!isServer || currentState == null) {
    //         return;
    //     }
        
    //     currentState.UpdateState(this);
    // }

    // public void ChangeState(IGameState newState)
    // {
    //     if (currentState != null) {
    //         currentState.ExitState(this);
    //     }

    //     currentState = newState;
    //     currentState.EnterState(this);

    //     currentStateName.value = newState.GetType().Name;
    // }


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

    [ObserversRpc]
    public void RPC_SyncCurrentQuestion(string text, string red, string green, string blue, string yellow, int correctIdx, int roundNum, int questionNum)
    {
        if (quizData != null)
        {
            quizData.currentQuestionText = text;
            quizData.redOption = red;
            quizData.greenOption = green;
            quizData.blueOption = blue;
            quizData.yellowOption = yellow;
            quizData.currentCorrectAnswerIndex = correctIdx;
            quizData.currentRound = roundNum;
            quizData.currentQuestion = questionNum;
        }

        QuizUIManager[] allUIs = Object.FindObjectsByType<QuizUIManager>(FindObjectsSortMode.None);
        foreach (var ui in allUIs)
        {
            if (ui.quizDataScript == null)
                ui.quizDataScript = quizData;
                
            ui.UpdateUI(roundNum);
        }
    }

    [ObserversRpc]
    public void RPC_ResetJailedPlayers()
    {
        var allPlayers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>();
        foreach (var player in allPlayers)
            player.isJailed = false;
    }


    [ObserversRpc]
        private void RPC_NotifyStateChange(string stateName) 
        {
            QuizUIManager[] allUIs = Object.FindObjectsByType<QuizUIManager>(FindObjectsSortMode.None);
            foreach (var ui in allUIs)
            {
                if (ui.quizDataScript == null)
                {
                    ui.quizDataScript = quizData;
                }
                ui.OnStateChanged(stateName);
            }

            Debug.Log($"[GameManager] State changed to {stateName}, notifying UIs.");
            if (stateName == "FreezeStateNode")
            {
                LoadingScreen.Instance?.Hide();
                Debug.Log("[LoadingScreen] Hiding loading screen.");
            }
        }

        protected override void OnSpawned()
        {
            if(isServer) {
                var lobbyHolder = FindFirstObjectByType<PurrLobby.LobbyDataHolder>();
                if (lobbyHolder != null && lobbyHolder.CurrentLobby.IsValid &&
                    lobbyHolder.CurrentLobby.Members != null && lobbyHolder.CurrentLobby.Members.Count > 0)
                {
                    requiredPlayers = lobbyHolder.CurrentLobby.Members.Count;
                    Debug.Log($"[GameManager] Required players set to {requiredPlayers} from lobby");
                }

                totalRoundsSettings.value = PlayerPrefs.GetInt("Quiz_RoundNumber", 3);
                roundDurationSettings.value = PlayerPrefs.GetInt("Quiz_QuestionTimer", 30);
                currentRound.value = 1;

                if (stateMachine != null && waitingState != null) {
                    stateMachine.SetState(waitingState);
                }

                if (stateMachine != null)
                {
                    stateMachine.onStateChanged += (prev, next) =>
                    {
                        if (next != null) 
                        {
                            string safeName = next.GetType().Name;
                            currentStateName.value = safeName;
                            RPC_NotifyStateChange(safeName);
                        }
                    };
                }

                //currentTimer.value = 0f;

                //ChangeState(waitingState);

                // freezeState.SetIsGenerating(true);
                // ChangeState(freezeState);

                // if (scoreManager) {
                //     scoreManager.ChooseTeams(); 
                // }
            }

        }


        public void InitializeGamePlayers()
        {
            if (scoreManager != null)
            {
                scoreManager.ChooseTeams();
                StationManager[] allRoomManagers = Object.FindObjectsByType<StationManager>(FindObjectsSortMode.None);
                foreach (StationManager rm in allRoomManagers)
                {
                    if (rm.roundNumber == 1)
                        rm.AssignStations(new System.Collections.Generic.List<IPlayerController>(scoreManager.allPlayers));
                }

                if (spawnManager != null) 
                {
                    spawnManager.TeleportAllPlayersToRound(scoreManager.allPlayers.ToArray(), 1);
                }
            }
        }
}
