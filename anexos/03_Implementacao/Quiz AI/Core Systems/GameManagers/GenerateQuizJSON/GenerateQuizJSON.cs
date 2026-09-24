using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GenerateQuizJSON : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool skipQuizGeneration = false;

    [Header("Quiz Settings")]
    [SerializeField] public string themes;
    [SerializeField] public int roundNumber = 3;
    [SerializeField] public int questionsPerRound = 5;
    [SerializeField] public string difficulty = "Medium";
    [SerializeField] public string currentQuestionText;

    private static readonly string[] DifficultyLabels = { "Easy", "Medium", "Hard" };

    [Header("UI")]
    [SerializeField] public int currentRound = 1;
    [SerializeField] public int currentQuestion = 1;
    [SerializeField] public string redOption;
    [SerializeField] public string greenOption;
    [SerializeField] public string blueOption;
    [SerializeField] public string yellowOption;

    [SerializeField] public int currentCorrectAnswerIndex = 0;

    [Header("UI Manager Script")]
    public QuizUIManager uiManager;

    private QuizDataRoot currentQuizData;

    public bool isQuizReady { get; private set; } = false;
    public System.Action onQuizReady;

    private void Start()
    {
        LoadQuizSettings();
        LoadEnvFile();
    }

    private void LoadQuizSettings()
    {
        roundNumber = PlayerPrefs.GetInt("Quiz_RoundNumber", roundNumber);

        int difficultyIndex = PlayerPrefs.GetInt("Quiz_Difficulty", 1);
        difficultyIndex = Mathf.Clamp(difficultyIndex, 0, DifficultyLabels.Length - 1);
        difficulty = DifficultyLabels[difficultyIndex];
    }

    public void GoNextQuestion()
    {
        currentQuestion++;

        if (currentQuestion > questionsPerRound)
        {

            currentQuestion = 1;
        }
        UpdateCurrentQuestion();
    }

    public void GoNextRound()
    {
        currentRound++;

        if (currentRound > roundNumber)
        {

            currentRound = 1;
        }
    }

    public void OnClickedGenerateQuiz()
    {
        SyncRoundNumberFromManager();

        string prompt = CreateQuizPrompt();

        StartCoroutine(SendRequest(prompt));
    }

    public void AutoGenerateQuiz()
    {
        isQuizReady = true;
        SyncRoundNumberFromManager();

        if (skipQuizGeneration)
        {

            StartCoroutine(LoadLocalQuizNextFrame());
            return;
        }

        string prompt = CreateQuizPrompt();
        StartCoroutine(SendRequestAndNotify(prompt));
    }

    private void SyncRoundNumberFromManager()
    {
        if (QuizGameManager.instance != null)
        {
            roundNumber = QuizGameManager.instance.totalRoundsSettings.value;
        }
    }

    private string CreateQuizPrompt()
    {
        string csvData = ReadQuizCSV();
        string cleanCsvData = DoubleCheckJSON(csvData);
        return BuildPrompt(cleanCsvData);
    }

    private IEnumerator LoadLocalQuizNextFrame()
    {
        yield return new WaitForSeconds(0.5f);
        LoadExistingQuiz();
        isQuizReady = true;
        onQuizReady?.Invoke();
    }

    private IEnumerator SendRequestAndNotify(string prompt)
    {
        yield return StartCoroutine(SendRequest(prompt));
        isQuizReady = true;

        onQuizReady?.Invoke();
    }

    private void LoadEnvFile()
    {
        string envPath = Path.Combine(Application.dataPath, "../.env");
        if (!File.Exists(envPath))
        {

            return;
        }

        string[] lines = File.ReadAllLines(envPath);
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            int separatorIndex = line.IndexOf('=');

            if (separatorIndex > 0)
            {
                string key = line.Substring(0, separatorIndex).Trim();
                string value = line.Substring(separatorIndex + 1).Trim();
                System.Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private string ReadQuizCSV()
    {
        string csvFilePath = Path.Combine(Application.persistentDataPath, "quiz_info.csv");
        if (!File.Exists(csvFilePath))
        {

            File.WriteAllText(csvFilePath, "Math\nScience\nHistory\nGeography\nPop Culture");
        }

        List<string> loadedThemes = new List<string>();
        using (StreamReader reader = new StreamReader(csvFilePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    loadedThemes.Add(line.Trim());
                }
            }
        }

        if (loadedThemes.Count == 0)
        {
            loadedThemes.Add("General Knowledge");
        }

        StringBuilder themesBuilder = new StringBuilder();
        int themeIndex = 0;

        for (int i = 0; i < roundNumber; i++)
        {
            themesBuilder.AppendLine(loadedThemes[themeIndex]);

            themeIndex = (themeIndex + 1) % loadedThemes.Count;
        }

        return themesBuilder.ToString();
    }

    private string BuildPrompt(string csvData)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Generate a trivia game with exactly {roundNumber} rounds.");
        sb.AppendLine();
        sb.AppendLine($"Each round must have exactly {questionsPerRound} questions.");
        sb.AppendLine($"All questions must be at {difficulty} difficulty level.");
        sb.AppendLine("Themes must ONLY come from the following list:");
        sb.AppendLine(csvData);
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine($"- total_rounds must equal {roundNumber}");
        sb.AppendLine("- round_id must start at 1 and increment sequentially");
        sb.AppendLine("- question_id must start at 1 inside each round");
        sb.AppendLine("- correct_option_index must be 0-based (0,1,2,3)");
        sb.AppendLine("- IMPORTANT: You MUST randomize the correct_option_index for every question. The correct answer should NOT always be at index 0.");
        sb.AppendLine("- CRITICAL: Every question must be fully self-contained. Do not use pronouns like 'he', 'she', or 'it' assuming the player knows the theme.");
        sb.AppendLine("- CRITICAL: The question must explicitly state the subject (e.g., 'What year did World War 2 start?' instead of 'What year did it start?').");
        sb.AppendLine("- Each question must have exactly 4 options");
        sb.AppendLine("- Each option must contain a maximum of 1 to 3 words");
        sb.AppendLine("- No duplicate questions");
        sb.AppendLine();
        sb.AppendLine("Return JSON in this EXACT structure:");
        sb.AppendLine(@"
{
  ""match_data"": {
    ""total_rounds"": number,
    ""rounds"": [
      {
        ""round_id"": number,
        ""theme"": ""string"",
        ""questions"": [
          {
            ""question_id"": number,
            ""text"": ""string"",
            ""options"": [""string"", ""string"", ""string"", ""string""],
            ""correct_option_index"": number
          }
        ]
      }
    ]
  }
}
");
        sb.AppendLine("Return raw JSON only. No markdown.");

        return sb.ToString();
    }

    private string DoubleCheckJSON(string input)
    {
        string cleanInput = input.Replace("{", "");
        cleanInput = cleanInput.Replace("}", "");
        cleanInput = cleanInput.Replace("\"", "");
        cleanInput = cleanInput.Replace("`", "");
        return cleanInput;
    }

    private IEnumerator SendRequest(string prompt)
    {
        string apiKey = System.Environment.GetEnvironmentVariable("API_KEY");

        string modelName = "gemini-2.5-flash";
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
        if (string.IsNullOrEmpty(apiKey))
        {

            yield break;
        }
        var requestData = new GeminiRequest
        {
            system_instruction = new GeminiSystemInstruction
            {
                parts = new GeminiPart[]
                {
                    new GeminiPart { text = "You are a helpful assistant that outputs raw JSON only. NEVER use markdown like ```json." }
                }
            },
            contents = new GeminiContent[]
            {
                new GeminiContent
                {
                    role = "user",
                    parts = new GeminiPart[]
                    {
                        new GeminiPart { text = prompt }
                    }
                }
            }
        };
        string jsonRequest = JsonUtility.ToJson(requestData);

        int maxRetries = 3;
        int currentAttempt = 0;
        bool success = false;

        while (currentAttempt < maxRetries && !success)
        {
            currentAttempt++;

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonRequest));
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;

                    success = ParseQuiz(jsonResponse);

                    if (!success)
                    {

                    }
                }
                else
                {

                }
            }

            if (!success && currentAttempt < maxRetries)
            {
                yield return new WaitForSeconds(1f);
            }
        }

        if (!success)
        {

        }
    }

    [System.Serializable]
    private class GeminiRequest
    {
        public GeminiSystemInstruction system_instruction;
        public GeminiContent[] contents;
    }
    [System.Serializable]
    private class GeminiSystemInstruction
    {
        public GeminiPart[] parts;
    }
    [System.Serializable]
    private class GeminiContent
    {
        public string role;
        public GeminiPart[] parts;
    }
    [System.Serializable]
    private class GeminiPart
    {
        public string text;
    }
    [System.Serializable]
    private class GeminiResponse
    {
        public GeminiCandidate[] candidates;
    }
    [System.Serializable]
    private class GeminiCandidate
    {
        public GeminiContent content;
    }

    [System.Serializable]
    public class QuizDataRoot
    {
        public MatchData match_data;
    }
    [System.Serializable]
    public class MatchData
    {
        public int total_rounds;
        public QuizRound[] rounds;
    }
    [System.Serializable]
    public class QuizRound
    {
        public int round_id;
        public string theme;
        public QuizQuestion[] questions;
    }
    [System.Serializable]
    public class QuizQuestion
    {
        public int question_id;
        public string text;
        public string[] options;
        public int correct_option_index;
    }

    private bool ParseQuiz(string response)
    {
        GeminiResponse responseData = null;
        try
        {
            responseData = JsonUtility.FromJson<GeminiResponse>(response);
        }
        catch (System.ArgumentException e)
        {

            return false;
        }

        if (responseData == null || responseData.candidates == null || responseData.candidates.Length == 0)
        {
            return false;
        }

        GeminiCandidate firstCandidate = responseData.candidates[0];
        if (firstCandidate == null || firstCandidate.content == null || firstCandidate.content.parts == null || firstCandidate.content.parts.Length == 0)
        {
            return false;
        }

        string aiText = firstCandidate.content.parts[0].text;
        if (string.IsNullOrWhiteSpace(aiText))
        {
            return false;
        }

        string cleanJson = aiText.Replace("```json", "").Replace("```", "").Trim();
        string savePath = Path.Combine(Application.persistentDataPath, "quiz_current.json");
        File.WriteAllText(savePath, cleanJson);

        QuizDataRoot quizData = null;
        try
        {
            quizData = JsonUtility.FromJson<QuizDataRoot>(cleanJson);
        }
        catch (System.ArgumentException e)
        {

            return false;
        }

        if (quizData == null || quizData.match_data == null)
        {
            return false;
        }

        currentQuizData = quizData;
        UpdateCurrentQuestion();
        return true;
    }

    public void UpdateCurrentQuestion()
    {
        if (QuizGameManager.instance != null)
        {
            currentRound = QuizGameManager.instance.currentRound.value;
        }

        if (currentQuizData == null || currentQuizData.match_data == null || currentQuizData.match_data.rounds == null)
        {

            return;
        }

        int roundIndex = currentRound - 1;
        int questionIndex = currentQuestion - 1;

        if (roundIndex < 0 || roundIndex >= currentQuizData.match_data.rounds.Length)
        {

            return;
        }

        QuizRound activeRound = currentQuizData.match_data.rounds[roundIndex];
        if (activeRound == null || activeRound.questions == null)
        {

            return;
        }

        if (questionIndex < 0 || questionIndex >= activeRound.questions.Length)
        {

            return;
        }

        QuizQuestion activeQuestion = activeRound.questions[questionIndex];
        if (activeQuestion == null || activeQuestion.options == null || activeQuestion.options.Length < 4)
        {

            return;
        }

        currentQuestionText = activeQuestion.text;
        redOption = activeQuestion.options[0];
        blueOption = activeQuestion.options[1];
        yellowOption = activeQuestion.options[2];
        greenOption = activeQuestion.options[3];
        currentCorrectAnswerIndex = activeQuestion.correct_option_index;

        QuizUIManager[] allUIs = Object.FindObjectsByType<QuizUIManager>(FindObjectsSortMode.None);
        foreach (QuizUIManager ui in allUIs)
        {
            ui.quizDataScript = this;
            ui.UpdateUI();
        }
    }

    private void LoadExistingQuiz()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "quiz_current.json");

        if (!File.Exists(savePath))
        {
            return;
        }

        string jsonContent = File.ReadAllText(savePath);
        QuizDataRoot quizData = JsonUtility.FromJson<QuizDataRoot>(jsonContent);
        if (quizData != null && quizData.match_data != null)
        {
            currentQuizData = quizData;
            UpdateCurrentQuestion();
        }
    }
}
