using UnityEngine;
using PurrNet;
using PurrNet.StateMachine;

public class QuizGameManager : NetworkBehaviour
{
    public static QuizGameManager instance { get; private set; }

    [Header("Lobby Settings")]
    public int requiredPlayers = 2;

    [Header("PurrNet State Machine")]
    public PurrNet.StateMachine.StateMachine stateMachine;
    public StateNode waitingState;
    public StateNode freezeState; 
    public StateNode questionState;
    public StateNode obstacleRaceState;
    public StateNode changeRoundState;

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

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void OnQuizGenerationComplete()
    {

        quizReady = true;
    }

    [ObserversRpc]
    public void RPC_SyncCurrentQuestion(string text, string red, string green, string blue, string yellow, int correctIdx, int roundNum, int questionNum)
    {
        UpdateCurrentQuestionData(text, red, green, blue, yellow, correctIdx, roundNum, questionNum);
        RefreshQuizUIs(roundNum);
    }

    [ObserversRpc]
    public void RPC_ResetJailedPlayers()
    {
        MonoBehaviour[] sceneBehaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in sceneBehaviours)
        {
            if (behaviour is not IPlayerController player)
            {
                continue;
            }

            player.isJailed = false;
        }
    }

    [ObserversRpc]
    private void RPC_NotifyStateChange(string stateName)
    {
        NotifyQuizUIsAboutStateChange(stateName);
        HideLoadingScreenIfNeeded(stateName);

    }

    protected override void OnSpawned()
    {
        if (!isServer)
        {
            return;
        }

        UpdateRequiredPlayersFromLobby();
        LoadServerSettings();
        SetInitialState();
        SubscribeToStateMachine();
    }

    protected override void OnDespawned()
    {
        if (stateMachine != null)
        {
            stateMachine.onStateChanged -= OnStateMachineStateChanged;
        }
    }

    private void UpdateCurrentQuestionData(string text, string red, string green, string blue, string yellow, int correctIdx, int roundNum, int questionNum)
    {
        if (quizData == null)
        {
            return;
        }

        quizData.currentQuestionText = text;
        quizData.redOption = red;
        quizData.greenOption = green;
        quizData.blueOption = blue;
        quizData.yellowOption = yellow;
        quizData.currentCorrectAnswerIndex = correctIdx;
        quizData.currentRound = roundNum;
        quizData.currentQuestion = questionNum;
    }

    private void RefreshQuizUIs(int roundNum)
    {
        QuizUIManager[] allUIs = Object.FindObjectsByType<QuizUIManager>(FindObjectsSortMode.None);
        foreach (QuizUIManager ui in allUIs)
        {
            if (ui.quizDataScript == null)
            {
                ui.quizDataScript = quizData;
            }

            ui.UpdateUI(roundNum);
        }
    }

    private void NotifyQuizUIsAboutStateChange(string stateName)
    {
        QuizUIManager[] allUIs = Object.FindObjectsByType<QuizUIManager>(FindObjectsSortMode.None);
        foreach (QuizUIManager ui in allUIs)
        {
            if (ui.quizDataScript == null)
            {
                ui.quizDataScript = quizData;
            }

            ui.OnStateChanged(stateName);
        }
    }

    private void HideLoadingScreenIfNeeded(string stateName)
    {
        if (stateName == "FreezeStateNode")
        {
            LoadingScreen.Instance?.Hide();
        }
    }

    private void UpdateRequiredPlayersFromLobby()
    {
        PurrLobby.LobbyDataHolder lobbyHolder = FindFirstObjectByType<PurrLobby.LobbyDataHolder>();
        if (lobbyHolder == null)
        {
            return;
        }

        if (!lobbyHolder.CurrentLobby.IsValid)
        {
            return;
        }

        if (lobbyHolder.CurrentLobby.Members == null || lobbyHolder.CurrentLobby.Members.Count == 0)
        {
            return;
        }

        requiredPlayers = lobbyHolder.CurrentLobby.Members.Count;

    }

    private void LoadServerSettings()
    {
        totalRoundsSettings.value = PlayerPrefs.GetInt("Quiz_RoundNumber", 3);
        roundDurationSettings.value = PlayerPrefs.GetInt("Quiz_QuestionTimer", 30);
        currentRound.value = 1;
    }

    private void SetInitialState()
    {
        if (stateMachine != null && waitingState != null)
        {
            stateMachine.SetState(waitingState);
        }
    }

    private void SubscribeToStateMachine()
    {
        if (stateMachine == null)
        {
            return;
        }

        stateMachine.onStateChanged -= OnStateMachineStateChanged;
        stateMachine.onStateChanged += OnStateMachineStateChanged;
    }

    private void OnStateMachineStateChanged(StateNode previousState, StateNode nextState)
    {
        if (nextState == null)
        {
            return;
        }

        string safeName = nextState.GetType().Name;
        currentStateName.value = safeName;
        RPC_NotifyStateChange(safeName);
    }

    public void InitializeGamePlayers()
    {
        if (scoreManager == null)
        {
            return;
        }

        scoreManager.ChooseTeams();

        StationManager[] allRoomManagers = Object.FindObjectsByType<StationManager>(FindObjectsSortMode.None);
        foreach (StationManager roomManager in allRoomManagers)
        {
            if (roomManager.roundNumber == 1)
            {
                roomManager.AssignStations(new System.Collections.Generic.List<IPlayerController>(scoreManager.allPlayers));
            }
        }

        if (spawnManager != null)
        {
            spawnManager.TeleportAllPlayersToRound(scoreManager.allPlayers.ToArray(), 1);
        }
    }
}
