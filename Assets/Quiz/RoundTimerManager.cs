using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RoundTimerManager : MonoBehaviour
{
    [Header("Settings")]
    public float roundDuration = 30f; // Seconds

    [Header("UI")]
    private Slider timerBar;

    [Header("State")]
    public float timeRemaining;

    public bool isOn = false;

    private void Start()
    {
        roundDuration = PlayerPrefs.GetInt("Quiz_RoundTimer", (int)roundDuration);
        timeRemaining = roundDuration;
    }

    private void Update()
    {
        if (!isOn) 
            return;

        timeRemaining -= Time.deltaTime;

        if(timerBar != null)
            timerBar.value = timeRemaining / roundDuration;

        if (timeRemaining <= 0f) {
            timeRemaining = 0f;
            isOn = false;
            OnTimerExpire();
        }
    }

    public void StartTimer()
    {
        timeRemaining = roundDuration;
        isOn = true;
    }

    public void StopTimer()
    {
        isOn = false;
    }

    private void OnTimerExpire()
    {
        Debug.Log("Round timer expired!");
    }
}
