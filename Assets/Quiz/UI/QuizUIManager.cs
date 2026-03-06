using UnityEngine;
using TMPro;

public class QuizUIManager : MonoBehaviour
{
    public static QuizUIManager Instance { get; private set; }

    [Header("Script")]
    public GenerateQuizJSON quizDataScript;

    [Header("UI Text")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI roundNumberText;
    public TextMeshProUGUI questionNumberText;

    public TextMeshProUGUI redButtonText;
    public TextMeshProUGUI greenButtonText;
    public TextMeshProUGUI blueButtonText;
    public TextMeshProUGUI yellowButtonText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void OnStateChanged(string stateName) {
        if (stateName == "QuestionState") {
            UpdateUI();
        } else if (stateName == "FreezeState") {
            // Call FreezeTime UI
        } else if (stateName == "ObstacleRaceState") {
            // Call ObstacleRace UI
        }
    }

    public void UpdateUI()
    {
        if (quizDataScript == null)
        {
            Debug.LogError("QuizDataScript is not assigned in the inspector.");
            return;
        }

        if (questionText != null) 
            questionText.text = quizDataScript.currentQuestionText;
        if (roundNumberText != null) 
            roundNumberText.text = "Round " + quizDataScript.currentRound.ToString();
        if (questionNumberText != null) 
            questionNumberText.text = quizDataScript.currentQuestion.ToString();
        if (redButtonText != null)
            redButtonText.text = quizDataScript.redOption;
        if (greenButtonText != null) 
            greenButtonText.text = quizDataScript.greenOption;
        if (blueButtonText != null) 
            blueButtonText.text = quizDataScript.blueOption;
        if (yellowButtonText != null) 
            yellowButtonText.text = quizDataScript.yellowOption;

        Debug.Log("UI Updated!");
    }
}
