# 🎒 Back To School - Game Design Document

## 1. Game Overview
**Back To School** is a chaotic, first-person multiplayer trivia game where players compete to answer questions while navigating a procedurally generated school environment. The game combines knowledge-based quiz rounds with physical disruption, allowing players to punch, push, and ragdoll their friends while racing through obstacle courses between classrooms.

## 2. Technical Stack & Architecture
The game relies on a peer-to-peer architecture to handle multiplayer lobbies, voice, and gameplay logic.



[Image of Peer-to-Peer (P2P) network architecture]


* **Networking:** Peer-to-Peer (P2P) utilizing **free Steam services**.
* **Networking Solution:** [PurrNet](https://purrnet.gitbook.io/docs/) for robust multiplayer synchronization.
* **Voice Chat:** Proximity-based voice chat using [MetaVoiceChat](https://github.com/Metater/MetaVoiceChat), adapted to run natively with PurrNet.
* **Content Generation:** AI-generated quizzes formulated dynamically at the start of the match.

---

## 3. User Interface (UI)
The game features a streamlined UI designed to get players into the action quickly. 

### Main Menu
* **Title:** Back To School
* **Create Lobby:** Opens the lobby configuration window.
* **Play Lobby:** Server browser/join via Steam friends.
* **Settings:** Audio, Video, and Control configurations.
* **Quit:** Exit to desktop.

### Lobby Settings (Host Only)
When a host creates a lobby, they can configure the match using the following parameters:

| Setting | Description |
| :--- | :--- |
| **Max Players** | The number of players allowed in the session. |
| **Round Timer** | Time limit for answering questions within a single round. |
| **Round Number** | Total number of quiz rounds in the match. |
| **Quiz Themes** | Uploaded via `.csv` format (e.g., History, Math, Pop Culture). |

---

## 4. Core Gameplay Loop
The game alternates between two primary phases: the **Quiz Phase** and the **Obstacle Race Phase**.

### Phase 1: The Quiz Room
1.  **Preparation:** At the start of the game, an AI generates all questions in `.json` format based on the host's `.csv` themes. 
2.  **Environment:** Players spawn in a classroom. A large board at the front displays the current question, and a countdown timer is visible in the top-right corner of the screen.
3.  **Objective:** Guess as many questions correctly as possible. Each round consists of exactly **5 questions**.
4.  **Disruption:** While guessing, players can freely move around and physically disrupt their opponents using the player mechanics.

### Phase 2: The Obstacle Race
1.  **Transition:** Once the 5-question round ends, the classroom doors fly open suddenly.
2.  **The Race:** Players must navigate a chaotic obstacle course through the school hallways to reach the next classroom.
3.  **The Punishment:** The last player to arrive in the next classroom is penalized—they are **blocked from answering the first question** of the new round.

---

## 5. Player Mechanics & Controls
The player character is built upon a modified **Kinematic Character Controller**, adjusted specifically for a first-person perspective. 



**Movement Capabilities:**
* Walk (Forward/Backward)
* Sprint (Forward only)
* Crouch
* Crouch Walk

**Interaction & Combat:**
* **Push & Grab:** Players can push opponents or grab items in the environment.
* **Punching:** Players can throw punches to disrupt others.
* **The Ragdoll System:** There is an internal combat counter. If a player is punched or grabbed **3 times** (interchangeably), they instantly turn into a physics ragdoll for **1 second** before standing back up. This is highly strategic for stopping players from answering questions or winning the hallway race.

---

## 6. Level Design & Procedural Generation
The map is procedurally generated to ensure that no two schools are exactly the same, keeping the hallway races fresh and unpredictable.



* **Grid System:** The map is built on a large grid layout.
* **Classrooms:** The algorithm generates a specific number of classroom rooms corresponding exactly to the *Round Number* set in the lobby settings. Each room hosts a different round of the quiz.
* **Hallway Presets:** The spaces between classrooms are filled using three core presets:
    1.  Standard Hallway
    2.  Hallway with a Door (chokepoints)
    3.  Cross Hallway (intersections)

---

## 7. Technical Breakdown: AI Generation & JSON Parsing

### 7.1 System Overview


The question generation pipeline bridges the host's lobby settings with the core gameplay loop. Because the game relies on a P2P architecture via PurrNet, the **Host** is responsible for reading the input files, communicating with the AI API, and broadcasting the parsed JSON data to all connected peers before the match begins.

### 7.2 The Input: Lobby CSV Format
When the host configures the lobby, they provide a `.csv` file containing the desired themes. To ensure the AI generates structured and relevant content, the CSV should follow a simple, single-column format.

**Example `themes.csv`:**
```csv
Theme
World War 2 History
Basic Calculus
Pop Culture 2010s
Video Game Trivia
```

### 7.3 The AI Generation Pipeline
Once the host clicks "Start Game", the game enters a brief loading state. During this time, the Host client performs the following steps:

* **Read CSV:** The client parses the `.csv` file into a string array or list.
* **Construct Prompt:** The client injects the parsed themes and the Round Number (from lobby settings) into a predefined prompt template.
* **Example Prompt:** "Generate a trivia game with {RoundNumber} rounds. Each round must have exactly 5 questions based on these themes: {CSV_Data}. Format the response strictly as a JSON object."
* **API Call:** Send the prompt to the chosen AI service.
* **Receive JSON:** The AI returns the generated questions.

### 7.4 The Output: JSON Schema
To parse the AI's response safely in a strongly typed language (like C#), the AI must adhere to a strict JSON structure.
Expected `questions.json` Structure:

```json
{
  "match_data": {
    "total_rounds": 3,
    "rounds": [
      {
        "round_id": 1,
        "theme": "World War 2 History",
        "questions": [
          {
            "question_id": 1,
            "text": "What year did WWII end?",
            "options": ["1943", "1944", "1945", "1946"],
            "correct_option_index": 2
          }
        ]
      }
    ]
  }
}
```

### 7.5 Parsing & Network Synchronization (PurrNet)
Once the Host receives the valid JSON string, it must be deserialized into internal classes and synchronized across the P2P network so all players see the same questions on the classroom boards.

#### Step A: Data Classes (C# Example)

```csharp
[System.Serializable]
public class QuestionData {
    public string text;
    public string[] options;
    public int correct_option_index;
}

[System.Serializable]
public class RoundData {
    public int round_id;
    public string theme;
    public QuestionData[] questions; // Array of exactly 5
}

[System.Serializable]
public class MatchData {
    public int total_rounds;
    public RoundData[] rounds;
}
```

#### Step B: Deserialization
Using a library like Newtonsoft.Json or UnityEngine.JsonUtility:

```csharp
// Host-side logic
string aiJsonResponse = GetAiResponse();
MatchData newMatch = JsonConvert.DeserializeObject<MatchData>(aiJsonResponse);
```

#### Step C: PurrNet Synchronization
Because JSON strings can be quite long, sending the raw string over the network to be parsed by every client is an option, but it's more efficient to send the structured data.

* Use a PurrNet RPC (Remote Procedure Call) or a Synchronized Network Variable to push the MatchData to all peers.
* Only once all peers send a "Data Received" RPC back to the Host, the physical classroom doors unlock, and the first timer begins.