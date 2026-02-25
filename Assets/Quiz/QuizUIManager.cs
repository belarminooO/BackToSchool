using UnityEngine;
using TMPro;

public class QuizUIManager : MonoBehaviour
{
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
