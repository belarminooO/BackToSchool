using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using static UnityEngine.Rendering.DebugUI;

public class GenerateQuizJSON : MonoBehaviour
{
    [Header("Quiz Settings")]
    [SerializeField] public string themes;
    [SerializeField] public int roundNumber = 3;
    [SerializeField] public int questionsPerRound = 5;
    [SerializeField] public string currentQuestionText;

    [Header("UI")]
    [SerializeField] public int currentRound = 1;
    [SerializeField] public int currentQuestion = 1;
    [SerializeField] public string redOption;
    [SerializeField] public string greenOption;
    [SerializeField] public string blueOption;
    [SerializeField] public string yellowOption;

    [Header("UI Manager Script")]
    public QuizUIManager uiManager;

    private string apiKey;
    //private string csvFilePath = "Quiz/quiz_info.csv";
    private QuizDataRoot currentQuizData;

    void Start()
    {
        roundNumber = PlayerPrefs.GetInt("Quiz_RoundNumber", roundNumber);

        LoadEnvFile();
        LoadExistingQuiz();
    }

    public void GoNextQuestion()
    {
        currentQuestion++;
        Debug.Log("Current question: " + currentQuestion);

        if (currentQuestion > questionsPerRound)
        {
            Debug.Log("Round finished!");
            currentQuestion = 1;
            GoNextRound();
        }
        UpdateCurrentQuestion();
    }

    public void GoNextRound()
    {
        currentRound++;
        Debug.Log("Current round: " + currentRound);
        
        if (currentRound > roundNumber)
        {
            Debug.Log("Quiz finished!");
            currentRound = 1;
        }
    }

    public void OnClickedGenerateQuiz()
    {
        Debug.Log("Botão clicado!");

        //string csvData = ReadQuizCSV(csvFilePath);
        string csvData = ReadQuizCSV();

        csvData = DoubleCheckJSON(csvData);
        string prompt = BuildPrompt(csvData);

        Debug.Log(prompt);

        StartCoroutine(SendRequest(prompt));
    }

    private void LoadEnvFile()
    {
        string envPath = Path.Combine(Application.dataPath, "../.env");
        if (!File.Exists(envPath))
        {
            Debug.LogError(".env file not found at: " + envPath);
        }

        string[] lines = File.ReadAllLines(envPath);
        foreach (string line in lines)
        {
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

        //string csvFilePath = Path.Combine(Application.dataPath, fileName);
        string csvFilePath = Path.Combine(Application.persistentDataPath, "quiz_info.csv");
        if (!File.Exists(csvFilePath))
        {
            Debug.LogError("CSV file not found at: " + csvFilePath);
            File.WriteAllText(csvFilePath, "Math\nScience\nHistory\nGeography\nPop Culture");
        }

        System.Collections.Generic.List<string> loadedThemes = new System.Collections.Generic.List<string>();
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

        //using (StreamReader reader = new StreamReader(csvFilePath))
        //{
        //    string line;
        //    while ((line = reader.ReadLine()) != null)
        //    {
        //        themesBuilder.AppendLine(line);
        //    }
        //}
        //return themesBuilder.ToString();
    }

    private string BuildPrompt(string csvData)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Generate a trivia game with exactly {roundNumber} rounds.");
        sb.AppendLine();
        sb.AppendLine($"Each round must have exactly {questionsPerRound} questions.");
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
        return input.Replace("{", "")
                    .Replace("}", "")
                    .Replace("\"", "")
                    .Replace("`", "");
    }

// OPEN AI

private IEnumerator SendRequest(string prompt)
    {
        string apiKey = System.Environment.GetEnvironmentVariable("API_KEY");
        
        string modelName = "gemini-2.5-flash";
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("No API Key found!");
            yield break;
        }
        var requestData = new GeminiRequest
        {
            system_instruction = new GeminiSystemInstruction
            {
                parts = new GeminiPart[]
                {
                    new GeminiPart { text = "You are a helpful assistant that outputs raw JSON only." }
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
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonRequest));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            webRequest.SetRequestHeader("Content-Type", "application/json");
            Debug.Log("Sending request to Gemini...");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;

                ParseQuiz(jsonResponse);
                
                GeminiResponse responseData = JsonUtility.FromJson<GeminiResponse>(jsonResponse);
                
                if (responseData != null && responseData.candidates != null && responseData.candidates.Length > 0)
                {
                    string aiText = responseData.candidates[0].content.parts[0].text;
                    Debug.Log("Response: " + aiText);
                }
                else
                {
                    Debug.LogWarning("Failed to parse the JSON content.");
                }
            }
            else
            {
                Debug.LogError("Error: " + webRequest.error);
            }
        }
    }

    // GEMINI

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

    // QUIZ

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

    private void ParseQuiz(string response)
    {
        GeminiResponse responseData = JsonUtility.FromJson<GeminiResponse>(response);
                
        if (responseData == null || responseData.candidates == null || responseData.candidates.Length == 0)
        {
            Debug.LogError("Error reading Gemini API Response...");
            return;
        }

        // Clean-up
        string aiText = responseData.candidates[0].content.parts[0].text;
        string cleanJson = aiText.Replace("```json", "").Replace("```", "").Trim();

        //string savePath = Path.Combine(Application.dataPath, "Quiz/quiz_current.json");
        string savePath = Path.Combine(Application.persistentDataPath, "quiz_current.json");

        File.WriteAllText(savePath, cleanJson);
        Debug.Log("JSON saved to " + savePath);
        
        QuizDataRoot quizData = JsonUtility.FromJson<QuizDataRoot>(cleanJson);

        // Check if quiz data is valid
        if (quizData == null || quizData.match_data == null)
        {
            Debug.LogError("Failed to parse quiz data.");
            return;
        }

        currentQuizData = quizData; 
        UpdateCurrentQuestion(); 

    }

    private void UpdateCurrentQuestion()
    {
        int roundIndex = currentRound - 1;
        int questionIndex = currentQuestion - 1;

        if (roundIndex < currentQuizData.match_data.rounds.Length)
        {
            QuizRound activeRound = currentQuizData.match_data.rounds[roundIndex];
            if (questionIndex < activeRound.questions.Length)
            {
                QuizQuestion activeQuestion = activeRound.questions[questionIndex];
                Debug.Log($"Loaded Q{activeQuestion.question_id}: {activeQuestion.text}");
                        
                currentQuestionText = activeQuestion.text;
                redOption = activeQuestion.options[0];
                greenOption = activeQuestion.options[1];
                blueOption = activeQuestion.options[2];
                yellowOption = activeQuestion.options[3];

                if (uiManager != null) 
                {
                    uiManager.UpdateUI();
                }
            }
        }
    }

    private void LoadExistingQuiz()
    {
        //string savePath = Path.Combine(Application.dataPath, "Quiz/quiz_current.json");
        string savePath = Path.Combine(Application.persistentDataPath, "quiz_current.json");

        if (File.Exists(savePath))
        {
            string jsonContent = File.ReadAllText(savePath);
            QuizDataRoot quizData = JsonUtility.FromJson<QuizDataRoot>(jsonContent);
            if (quizData != null && quizData.match_data != null)
            {
                currentQuizData = quizData;
                UpdateCurrentQuestion();
            }
        }
    }
}
