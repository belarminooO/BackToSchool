using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using GinjaGaming.FinalCharacterController;

public enum Team { Team1, Team2, Team3, Team4, None }

public class ScoreManager : NetworkBehaviour
{
    public SyncVar<int> team1Score = new SyncVar<int>();
    public SyncVar<int> team2Score = new SyncVar<int>();
    public SyncVar<int> team3Score = new SyncVar<int>();
    public SyncVar<int> team4Score = new SyncVar<int>();

    [Header("Players")]
    public List<MyCharacterController> allPlayers = new List<MyCharacterController>();
    public List<MyCharacterController> arrivedPlayers = new List<MyCharacterController>();

    [Header("UI")]
    public GameObject regularGameUI;
    public TMPro.TextMeshProUGUI leaderboardText;

    [Header("End Game UI")]
    public GameObject endGameUI;
    public TMPro.TextMeshProUGUI finalLeaderboardText;

    private void Start() {
        if (regularGameUI != null) {
            regularGameUI.SetActive(true);
        }
        
        if (endGameUI != null) {
            endGameUI.SetActive(false);
        }
    }

    public void ChooseTeams() {
        if(!isServer) {
            return;
        }
        allPlayers = new List<MyCharacterController>(FindObjectsByType<MyCharacterController>(FindObjectsSortMode.None));
        int playerCount = allPlayers.Count;
        if (playerCount == 0) {
            return;
        }

        Debug.Log($"[Server] Assigning Teams for {playerCount} players.");
        
        int totalTeamsToUse = 2;

        if (playerCount == 5 || playerCount == 6) {
            totalTeamsToUse = 3; 
        }
        else if (playerCount == 7 || playerCount >= 8) {
            totalTeamsToUse = 4; 
        }

        for (int i = 0; i < playerCount; i++) {
            int teamIndex = i % totalTeamsToUse;
            Team assignedTeam = (Team)teamIndex;
            allPlayers[i].AssignTeam(assignedTeam);
            Debug.Log($"[Server] Assigned {allPlayers[i].gameObject.name} to {assignedTeam}");
        }

        RPC_UpdateLeaderboard();
    }

    public void PrepareForObstacleRace() {
        arrivedPlayers.Clear();
    }

    public void PlayerArrived(MyCharacterController player) {
        if(!isServer) {
            return;
        }

        if (!arrivedPlayers.Contains(player))
        {
            arrivedPlayers.Add(player);
            Debug.Log($"[Server] {player.gameObject.name} arrived safely!");
        }
    }

    public bool allPlayersArrived() {

        if (allPlayers.Count == 0) {
            return true;
        }

        return arrivedPlayers.Count >= allPlayers.Count;
    }

    public List<MyCharacterController> GetPenalizedPlayers() {
        List<MyCharacterController> penalized = new List<MyCharacterController>();

        if (!isServer) {
            return penalized;
        }

        foreach (var player in allPlayers) {
            if (!arrivedPlayers.Contains(player)) {
                penalized.Add(player);
            }
        }
        return penalized;
    }

    public void AddScoreToTeam(Team team, int amount)
    {
        if (!isServer) {
            return;
        }
        switch (team)
        {
            case Team.Team1: team1Score.value += amount; 
            break;
            case Team.Team2: team2Score.value += amount; 
            break;
            case Team.Team3: team3Score.value += amount; 
            break;
            case Team.Team4: team4Score.value += amount; 
            break;
        }
    }

    public void CalculateScores() {
        if (!isServer) {
            return;
        }

        foreach (var player in allPlayers)
        {
            switch(player.team)
            {
                case Team.Team1: team1Score.value += 10; 
                break;
                case Team.Team2: team2Score.value += 10; 
                break;
                case Team.Team3: team3Score.value += 10; 
                break;
                case Team.Team4: team4Score.value += 10; 
                break;
                case Team.None: 
                break;
            }
        }

        RPC_UpdateLeaderboard();
    }

    public void PenalizePlayers()
    {
        if (!isServer) {
            return;
        }

        List<MyCharacterController> stragglers = GetPenalizedPlayers();
        
        foreach (var player in stragglers)
        {
            switch(player.team)
            {
                case Team.Team1: 
                    team1Score.value -= 2; 
                    break;
                case Team.Team2: 
                    team2Score.value -= 2; 
                    break;
                case Team.Team3: 
                    team3Score.value -= 2; 
                    break;
                case Team.Team4: 
                    team4Score.value -= 2;
                    break;
                case Team.None: 
                    break;
            }
        }

        RPC_UpdateLeaderboard();
    }

    public void UpdateLeaderboard()
    {        
        
        List<string> team1Names = new List<string>();
        List<string> team2Names = new List<string>();
        List<string> team3Names = new List<string>();
        List<string> team4Names = new List<string>();

        foreach (var player in allPlayers)
        {
            string nameToDisplay = "Null";

            PlayerNameTag nametag = player.GetComponentInChildren<PlayerNameTag>(true);
            if (nametag != null && !string.IsNullOrEmpty(nametag.steamName.value)) {
                nameToDisplay = nametag.steamName.value;
            }

            switch(player.team)
            {
                case Team.Team1: team1Names.Add(nameToDisplay); 
                break;
                case Team.Team2: team2Names.Add(nameToDisplay); 
                break;
                case Team.Team3: team3Names.Add(nameToDisplay); 
                break;
                case Team.Team4: team4Names.Add(nameToDisplay); 
                break;
                case Team.None: 
                break;
            }
        }

        string GetTeamString(List<string> names, string defaultName)
        {
            if (names.Count == 0) 
            {
                return defaultName;
            }

            return string.Join(" & ", names);
        }

        if (leaderboardText == null) {
            return;
        }
        string text = $"Leaderboard:\n" +
            $"{GetTeamString(team1Names, "Team 1")}: {team1Score.value} pts\n" +
            $"{GetTeamString(team2Names, "Team 2")}: {team2Score.value} pts\n" +
            $"{GetTeamString(team3Names, "Team 3")}: {team3Score.value} pts\n" +
            $"{GetTeamString(team4Names, "Team 4")}: {team4Score.value} pts";

        if (leaderboardText != null) 
            leaderboardText.text = text;
        if (finalLeaderboardText != null) 
            finalLeaderboardText.text = text;
    }

    [ObserversRpc]
    public void RPC_UpdateLeaderboard() 
    {
        UpdateLeaderboard();
    }

    [ObserversRpc]
    public void RPC_ShowEndGameScreen()
    {
        if (endGameUI != null) {
            endGameUI.SetActive(true);
        }
        if (regularGameUI != null) {
            regularGameUI.SetActive(false);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var controllers = Object.FindObjectsByType<MyCharacterController>(FindObjectsSortMode.None);
        foreach (var controller in controllers)
        {
            controller.IsMovementEnabled = false;
        }
    }

    // Leave to Lobby
    public void LeaveToLobby()
    {
        var lobbyManager = Object.FindFirstObjectByType<PurrLobby.LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.LeaveLobby();
        }

        if (NetworkManager.main != null) {
            NetworkManager.main.StopClient();
        }

        if (NetworkManager.main != null && NetworkManager.main.isServer) {
            NetworkManager.main.StopServer();
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbySample");
    }
}
