# 🎒 Back To School - Game Design Document

## 1. Game Overview
**Back To School** is a chaotic, first-person multiplayer trivia game where players compete to answer questions while navigating the school environment. The game combines knowledge-based quiz rounds with physical disruption, allowing players to push, grab, and throw objects at their friends while racing through obstacle courses between classrooms. Players compete in teams of two, communicating via proximity voice chat to find the right answers without getting caught by the classroom guard (Vigia), with the highest total score winning at the end.

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
* **Browse Lobby:** Server browser to access existing lobbies.
* **Join Lobby:** Join an existing lobby via a room code.
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

### HUD
During gameplay, the UI includes:
* **Crosshair:** Identifies interactable objects and players in the center of the screen.
* **Dynamic Timer:** Shows time remaining for questions or the hallway race.
* **Scoreboard:** Displays the total score for each team.
* **Interaction Visualizer:** Displays what actions can be taken on targeted objects/players.
* **Voice Chat Indicator:** Highlights who is currently speaking via Proximity Voice Chat.

---

## 4. Core Gameplay Loop
The game alternates between two primary phases: the **Quiz Phase** and the **Obstacle Race Phase**.

### Phase 1: The Quiz Room
1.  **Preparation:** At the start of the game, an AI generates all questions in `.json` format based on the host's `.csv` themes. 
2.  **Environment:** Players spawn in a classroom seated at their respective desks. A large board at the front displays the current question, and a countdown timer is visible.
3.  **Objective:** Answer as many questions correctly as possible. Each round consists of exactly **5 questions**.
4.  **Interaction & Disruption:** Each desk has 4 physical buttons corresponding to the possible answers. Any player can press the buttons on any desk to register the answer for that desk's owner. Players can freely move around, push/grab opponents, or throw small physical objects (pencils, paper balls, cans) to cause chaos.
5.  **The Guard (Vigia):** A guard NPC supervises the room via a vision cone and listening. If players talk too loudly via proximity chat, he investigates. If caught, the player is sent to a classroom "jail" and blocked from answering further questions that round.

### Phase 2: The Obstacle Race
1.  **Transition:** Once the 5-question round ends, the classroom doors fly open suddenly.
2.  **The Race:** Players must navigate a chaotic obstacle course through the school hallways to reach the next classroom. The hallways are populated with simple wandering student NPCs (Colegas).
3.  **The Punishment:** The last player to arrive in the new classroom, or any player who fails to arrive in time, receives a penalty to their team's total score.

---

## 5. Player Mechanics & Controls
The player character is built upon a modified **Kinematic Character Controller**, adjusted specifically for a first-person perspective. 



**Movement Capabilities:**
* Walk (Forward/Backward/Left/Right)
* Sprint (Forward only)
* Jump
* Crouch
* Crouch Walk

**Interaction & Combat:**
* **Interact (E):** Use utility objects (e.g., doors, physical quiz buttons) and pick up throwable physics objects.
* **Push (Left Mouse Button):** Push an opponent away from their position.
* **Grab (G):** Grab opponents, pulling them towards the player.
* **Throw Objects:** Players can throw small physics objects to disrupt other players.

---

## 6. Level Design
The game takes place within a fixed school environment. 

* **Classrooms:** Different rooms host the various rounds of the quiz. 
* **Hallways:** The spaces between classrooms act as obstacle courses for the transition phases (Intervalo), filled with props, chokepoints, and moving student NPCs to hinder players during the race.

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