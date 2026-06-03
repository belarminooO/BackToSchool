using UnityEngine;
using TMPro;

[System.Serializable]
public class RoundBoardRefs
{
    public GameObject boardObject;

    public TextMeshProUGUI questionText;
    public TextMeshProUGUI questionNumberText;

    public TextMeshProUGUI redButtonText;
    public TextMeshProUGUI greenButtonText;
    public TextMeshProUGUI blueButtonText;
    public TextMeshProUGUI yellowButtonText;
}

public class QuizUIManager : MonoBehaviour
{
    public static QuizUIManager Instance { get; private set; }
    private string _lastStateName;

    [Header("Script")]
    public GenerateQuizJSON quizDataScript;

    [Header("General UI")]
    public TextMeshProUGUI roundNumberText;

    [Header("Board Index")]
    public RoundBoardRefs[] roundBoards;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private RoundBoardRefs GetCurrentBoard(int specificRound = -1)
    {
        if (roundBoards == null || roundBoards.Length == 0)
            return null;

        int round = specificRound != -1 ? specificRound : (QuizGameManager.instance != null ? QuizGameManager.instance.currentRound.value : 1);
        int index = Mathf.Clamp(round - 1, 0, roundBoards.Length - 1);
        return roundBoards[index];
    }

    public void OnStateChanged(string stateName)
    {
        if (stateName != _lastStateName)
        {
            if (stateName == "QuestionStateNode")
                GeneratedQuizSfx.PlayQuestionStart();
            else if (stateName == "ObstacleRaceStateNode")
                GeneratedQuizSfx.PlayIntervalBell();
            else if (stateName == "ChangeRoundStateNode" && _lastStateName == "ObstacleRaceStateNode")
                GeneratedQuizSfx.PlayIntervalBell();

            _lastStateName = stateName;
        }

        if (stateName == "WaitingStateNode")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        bool showBoard = stateName == "QuestionStateNode" || stateName == "FreezeStateNode";

        if (roundBoards != null)
        {
            int round = QuizGameManager.instance != null ? QuizGameManager.instance.currentRound.value : 1;
            for (int i = 0; i < roundBoards.Length; i++)
            {
                if (roundBoards[i].boardObject != null)
                    roundBoards[i].boardObject.SetActive(showBoard && i == round - 1);
            }
        }

        if (stateName == "QuestionStateNode")
        {
            UpdateUI();
        }
        else if (stateName == "FreezeStateNode")
        {
            // Call FreezeTime UI
        }
        else if (stateName == "ObstacleRaceStateNode")
        {
            // Call ObstacleRace UI
        }
    }

    public void UpdateUI(int specificRound = -1)
    {
        if (quizDataScript == null)
        {
            Debug.LogError("[QuizUIManager] quizDataScript is not assigned.");
            return;
        }

        if (roundNumberText != null)
            roundNumberText.text = "Round " + quizDataScript.currentRound.ToString();

        RoundBoardRefs board = GetCurrentBoard(specificRound);
        if (board == null)
        {
            Debug.LogWarning("[QuizUIManager] No board configured for current round.");
            return;
        }

        if (board.questionText != null)
            board.questionText.text = quizDataScript.currentQuestionText;
        if (board.questionNumberText != null)
            board.questionNumberText.text = quizDataScript.currentQuestion.ToString();
        if (board.redButtonText != null)
            board.redButtonText.text = quizDataScript.redOption;
        if (board.greenButtonText != null)
            board.greenButtonText.text = quizDataScript.greenOption;
        if (board.blueButtonText != null)
            board.blueButtonText.text = quizDataScript.blueOption;
        if (board.yellowButtonText != null)
            board.yellowButtonText.text = quizDataScript.yellowOption;

        Debug.Log($"[QuizUIManager] UI updated for Round {quizDataScript.currentRound}, Q{quizDataScript.currentQuestion}");
    }
}
