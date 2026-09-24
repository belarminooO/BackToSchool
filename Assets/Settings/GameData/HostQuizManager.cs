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
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TMP_Text roundTimerDisplay;
    [SerializeField] private TMP_Text roundNumberDisplay;
    [SerializeField] private TMP_Text difficultyDisplay;
    [SerializeField] private TMP_InputField quizThemesInput;

    private static readonly string[] DifficultyLabels = { "Easy", "Medium", "Hard" };

    private static string DifficultyToLabel(int value)
    {
        return DifficultyLabels[Mathf.Clamp(value, 0, DifficultyLabels.Length - 1)];
    }

    private string csvFilePath;

    private void Start()
    {
        csvFilePath = Application.persistentDataPath + "/quiz_info.csv";

        int prefsTimer  = PlayerPrefs.GetInt("Quiz_QuestionTimer", 30);
        int prefsRounds = PlayerPrefs.GetInt("Quiz_RoundNumber", 3);
        int prefsDifficulty = PlayerPrefs.GetInt("Quiz_Difficulty", 1);
        string loadedThemes = LoadThemesFromFile();

        if (roundTimerSlider)   roundTimerSlider.value  = prefsTimer;
        if (roundNumberSlider)  roundNumberSlider.value = prefsRounds;
        if (difficultySlider)   difficultySlider.value  = prefsDifficulty;
        if (quizThemesInput)    quizThemesInput.text    = loadedThemes;
        if (roundTimerDisplay)  roundTimerDisplay.text  = $"{prefsTimer} Sec";
        if (roundNumberDisplay) roundNumberDisplay.text = $"{prefsRounds} Rounds";
        if (difficultyDisplay)  difficultyDisplay.text  = DifficultyToLabel(prefsDifficulty);

        roundTimerSlider?.onValueChanged.AddListener((_) => OnChangedSlider());
        roundNumberSlider?.onValueChanged.AddListener((_) => OnChangedSlider());
        difficultySlider?.onValueChanged.AddListener((_) => OnChangedSlider());
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
        int newDifficulty = difficultySlider ? Mathf.RoundToInt(difficultySlider.value) : 1;
        if (roundTimerDisplay)  roundTimerDisplay.text  = $"{newTimer} Sec";
        if (roundNumberDisplay) roundNumberDisplay.text = $"{newRounds} Rounds";
        if (difficultyDisplay)  difficultyDisplay.text  = DifficultyToLabel(newDifficulty);
        PlayerPrefs.SetInt("Quiz_QuestionTimer",  newTimer);
        PlayerPrefs.SetInt("Quiz_RoundNumber", newRounds);
        PlayerPrefs.SetInt("Quiz_Difficulty", newDifficulty);
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
