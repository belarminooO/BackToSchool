using UnityEngine;
using UnityEngine.UI;
using PurrLobby;
using TMPro;
using System.IO;

public class HostQuizManager : MonoBehaviour 
{
    [Header("Host UI (Only enabled for Host)")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private LobbyManager lobbyManager; 

    [SerializeField] private Slider roundTimerSlider;
    [SerializeField] private Slider roundNumberSlider;
    [SerializeField] private TMP_Text roundTimerDisplay;
    [SerializeField] private TMP_Text roundNumberDisplay;
    [SerializeField] private TMP_InputField quizThemesInput;

    private string csvFilePath;

    private void Start()
    {
        csvFilePath = Application.persistentDataPath + "/quiz_info.csv";

        int prefsTimer  = PlayerPrefs.GetInt("Quiz_QuestionTimer", 30);
        int prefsRounds = PlayerPrefs.GetInt("Quiz_RoundNumber", 3);
        string loadedThemes = LoadThemesFromFile();

        if (roundTimerSlider)   roundTimerSlider.value  = prefsTimer;
        if (roundNumberSlider)  roundNumberSlider.value = prefsRounds;
        if (quizThemesInput)    quizThemesInput.text    = loadedThemes;
        if (roundTimerDisplay)  roundTimerDisplay.text  = $"{prefsTimer} Sec";
        if (roundNumberDisplay) roundNumberDisplay.text = $"{prefsRounds} Rounds";

        roundTimerSlider?.onValueChanged.AddListener((_) => OnChangedSlider());
        roundNumberSlider?.onValueChanged.AddListener((_) => OnChangedSlider());
        quizThemesInput?.onEndEdit.AddListener((_) => SaveThemesToCSV());

        if (hostPanel != null) 
            hostPanel.SetActive(false); 

        if (lobbyManager != null)
        {
            lobbyManager.OnRoomJoined.AddListener(OnLobbyJoined);
            lobbyManager.OnRoomUpdated.AddListener(OnLobbyJoined);
            lobbyManager.OnRoomLeft.AddListener(OnLobbyLeft);
        }
    }

    private void OnLobbyJoined(Lobby lobby)
    {
        if (hostPanel != null)
            hostPanel.SetActive(lobby.IsOwner);
    }

    private void OnLobbyLeft()
    {
        if (hostPanel != null) hostPanel.SetActive(false);
    }

    private string LoadThemesFromFile()
    {
        if (File.Exists(csvFilePath))
            return File.ReadAllText(csvFilePath).Replace("\n", ", ").TrimEnd(',', ' ');
        return "General Knowledge, Math, Science, History";
    }

    private void OnChangedSlider()
    {
        int newTimer  = Mathf.RoundToInt(roundTimerSlider.value);
        int newRounds = Mathf.RoundToInt(roundNumberSlider.value);
        if (roundTimerDisplay)  roundTimerDisplay.text  = $"{newTimer} Sec";
        if (roundNumberDisplay) roundNumberDisplay.text = $"{newRounds} Rounds";
        PlayerPrefs.SetInt("Quiz_QuestionTimer",  newTimer);
        PlayerPrefs.SetInt("Quiz_RoundNumber", newRounds);
        PlayerPrefs.Save();
    }

    private void SaveThemesToCSV()
    {
        string formatted = quizThemesInput.text.Replace(", ", "\n").Replace(",", "\n");
        File.WriteAllText(csvFilePath, formatted);
        Debug.Log($"Host saved themes to {csvFilePath}");
    }

    private void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnRoomJoined.RemoveListener(OnLobbyJoined);
            lobbyManager.OnRoomUpdated.RemoveListener(OnLobbyJoined);
            lobbyManager.OnRoomLeft.RemoveListener(OnLobbyLeft);
        }
    }
}
