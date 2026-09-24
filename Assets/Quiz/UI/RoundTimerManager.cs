using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RoundTimerManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider timerBar;
    [SerializeField] private Image fillImage;

    [Header("Audio Warning")]
    [SerializeField] private float lowTimeWarningThreshold = 5f;

    [Header("State Colors")]
    [SerializeField] private Color freezeColor = new Color(0.8f, 0.8f, 0.8f); // Light Grey
    [SerializeField] private Color questionColor = new Color(0.20f, 0.60f, 1.00f); // Blue
    [SerializeField] private Color obstacleColor = new Color(0.20f, 0.85f, 0.30f); // Green
    [SerializeField] private Color changeColor = new Color(0.8f, 0.8f, 0.8f); // Light Grey

    [Header("Dependencies")]
    [SerializeField] private QuizGameManager gameManager; 

    private bool _playedLowTimeWarning;

    private void Awake()
    {
        if (gameManager == null) 
            gameManager = QuizGameManager.instance;
    }

    private void Update()
    {
        if (gameManager == null) {
            gameManager = QuizGameManager.instance;
        }
        
        if (gameManager == null || timerBar == null) {
           return; 
        } 

        string state = gameManager.currentStateName.value;
        float maximum = gameManager.maxTimer.value;

        if(maximum <= 0f) {
            timerBar.value = 0f;
        } else {
            timerBar.minValue = 0f;
            timerBar.maxValue = maximum;
            timerBar.value = Mathf.Clamp(gameManager.currentTimer.value, 0f, maximum);    

        }

        if (fillImage != null) 
        {
                if (state == "QuestionStateNode")
                    fillImage.color = questionColor;
                else if (state == "ObstacleRaceStateNode")
                    fillImage.color = obstacleColor;
                else if (state == "ChangeRoundStateNode")
                    fillImage.color = changeColor;
                else
                    fillImage.color = freezeColor;
        }

        UpdateLowTimeWarning(state, maximum, gameManager.currentTimer.value);
    }

    private void UpdateLowTimeWarning(string state, float maximum, float current)
    {
        if (state != "QuestionStateNode")
        {
            _playedLowTimeWarning = false;
            return;
        }

        if (lowTimeWarningThreshold <= 0f || maximum <= lowTimeWarningThreshold)
            return;

        if (current > lowTimeWarningThreshold)
        {
            _playedLowTimeWarning = false;
            return;
        }

        if (_playedLowTimeWarning)
            return;

        GeneratedQuizSfx.PlayQuestionTimeWarning();
        _playedLowTimeWarning = true;
    }
}
