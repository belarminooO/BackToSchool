using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using GinjaGaming.FinalCharacterController;
using System.Linq;

public enum Team { Team1, Team2, Team3, Team4, Team5, Team6, Team7, Team8, None }

public class ScoreManager : NetworkBehaviour
{
    public SyncVar<int> team1Score = new SyncVar<int>();
    public SyncVar<int> team2Score = new SyncVar<int>();
    public SyncVar<int> team3Score = new SyncVar<int>();
    public SyncVar<int> team4Score = new SyncVar<int>();
    public SyncVar<int> team5Score = new SyncVar<int>();
    public SyncVar<int> team6Score = new SyncVar<int>();
    public SyncVar<int> team7Score = new SyncVar<int>();
    public SyncVar<int> team8Score = new SyncVar<int>();

    [Header("Players")]
    public SyncList<PlayerID> registeredPlayers = new SyncList<PlayerID>();
    public List<IPlayerController> arrivedPlayers = new List<IPlayerController>();
    public List<IPlayerController> allPlayers = new List<IPlayerController>();

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

    protected override void OnSpawned()
    {
        if (isServer)
        {
            foreach (var p in networkManager.players)
            {
                if (!registeredPlayers.Contains(p)) {
                    registeredPlayers.Add(p);
                    Debug.Log($"[ScoreManager] Player joined: {p}, total: {registeredPlayers.Count}");
                }
            }

            // Listeners for new players joining
            networkManager.onPlayerJoined += (player, isReconnect, asServer) => {
                if (!registeredPlayers.Contains(player)) {
                    registeredPlayers.Add(player);
                    Debug.Log($"[ScoreManager] Player joined: {player}, total: {registeredPlayers.Count}");
                }
            };

            // Listeners for players leaving
            networkManager.onPlayerLeft += (player, asServer) => {
                registeredPlayers.Remove(player);
                Debug.Log($"[ScoreManager] Player left: {player}, total: {registeredPlayers.Count}");
            };

        }
        team1Score.onChanged += (val) => UpdateLeaderboard();
        team2Score.onChanged += (val) => UpdateLeaderboard();
        team3Score.onChanged += (val) => UpdateLeaderboard();
        team4Score.onChanged += (val) => UpdateLeaderboard();
        team5Score.onChanged += (val) => UpdateLeaderboard();
        team6Score.onChanged += (val) => UpdateLeaderboard();
        team7Score.onChanged += (val) => UpdateLeaderboard();
        team8Score.onChanged += (val) => UpdateLeaderboard();
    }

    public void ChooseTeams() {
        if(!isServer) {
            return;
        }
        allPlayers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>().ToList();
        int playerCount = allPlayers.Count;
        if (playerCount == 0) {
            return;
        }

        Debug.Log($"[Server] Assigning Teams for {playerCount} players.");
        
        // int totalTeamsToUse = 2;

        // if (playerCount == 5 || playerCount == 6) {
        //     totalTeamsToUse = 3; 
        // }
        // else if (playerCount == 7 || playerCount >= 8) {
        //     totalTeamsToUse = 4; 
        // }

        // for (int i = 0; i < playerCount; i++) {
        //     //int teamIndex = i % totalTeamsToUse;
        //     Team assignedTeam = (Team)teamIndex;
        //     allPlayers[i].AssignTeam(assignedTeam);

        //     var network = allPlayers[i].GetComponent<NetworkIdentity>();
        //     if (network != null) {
        //         RPC_SyncTeam(network, (int)assignedTeam);
        //     }

        //     var nameTag = allPlayers[i].GetComponentInChildren<PlayerNameTag>(true);
        //     if (nameTag != null)
        //     {
        //         nameTag.steamName.onChanged += _ => RPC_UpdateLeaderboard();

        //     }

        //     Debug.Log($"[Server] Assigned {allPlayers[i].gameObject.name} to {assignedTeam}");
        // }

        for (int i = 0; i < playerCount; i++) {
            Team assignedTeam = (Team)(i % 8); 
            allPlayers[i].AssignTeam(assignedTeam);
            var network = allPlayers[i].gameObject.GetComponent<NetworkIdentity>();

            if (network != null) {
                RPC_SyncTeam(network, (int)assignedTeam);
            }

            var nameTag = allPlayers[i].gameObject.GetComponentInChildren<PlayerNameTag>(true);

            if (nameTag != null)
            {
                nameTag.steamName.onChanged += _ => RPC_UpdateLeaderboard();
            }

            Debug.Log($"[Server] Assigned {allPlayers[i].gameObject.name} to {assignedTeam}");
        }

        RPC_UpdateLeaderboard();
    }

    [ObserversRpc]
    public void RPC_SyncTeam(NetworkIdentity network, int teamIndex) {
        if (isServer) {
            return;
        }
        var allPlayers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>();
        foreach (var player in allPlayers)
        {
            var n = player.gameObject.GetComponent<NetworkIdentity>();
            if (n != null && n == network)
            {
                player.team = (Team)teamIndex;
                break;
            }
        }
    }

    public void PrepareForObstacleRace() {
        arrivedPlayers.Clear();
    }

    public void PlayerArrived(IPlayerController player) {
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

    public List<IPlayerController> GetPenalizedPlayers() {
        List<IPlayerController> penalized = new List<IPlayerController>();

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
            case Team.Team1: 
                team1Score.value += amount; 
                break;
            case Team.Team2: 
                team2Score.value += amount; 
                break;
            case Team.Team3: 
                team3Score.value += amount; 
                break;
            case Team.Team4: 
                team4Score.value += amount; 
                break;
            case Team.Team5: 
                team5Score.value += amount; 
                break;
            case Team.Team6: 
                team6Score.value += amount; 
                break;
            case Team.Team7: 
                team7Score.value += amount; 
                break;
            case Team.Team8: 
                team8Score.value += amount; 
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
                case Team.Team5: team5Score.value += 10; 
                break;
                case Team.Team6: team6Score.value += 10; 
                break;
                case Team.Team7: team7Score.value += 10; 
                break;
                case Team.Team8: team8Score.value += 10; 
                break;
                case Team.None: 
                break;
            }
        }

        //RPC_UpdateLeaderboard();
    }

    public void PenalizePlayers()
    {
        if (!isServer) {
            return;
        }

        List<IPlayerController> stragglers = GetPenalizedPlayers();
        
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
                case Team.Team5: 
                    team5Score.value -= 2;
                    break;
                case Team.Team6: 
                    team6Score.value -= 2;
                    break;
                case Team.Team7: 
                    team7Score.value -= 2;
                    break;
                case Team.Team8: 
                    team8Score.value -= 2;
                    break;
                case Team.None: 
                    break;
            }
        }

        //RPC_UpdateLeaderboard();
    }

    public void UpdateLeaderboard()
    {        
        allPlayers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>().ToList();
        
        List<string> team1Names = new List<string>();
        List<string> team2Names = new List<string>();
        List<string> team3Names = new List<string>();
        List<string> team4Names = new List<string>();
        List<string> team5Names = new List<string>();
        List<string> team6Names = new List<string>();
        List<string> team7Names = new List<string>();
        List<string> team8Names = new List<string>();

        foreach (var player in allPlayers)
        {
            string nameToDisplay = "Null";

            PlayerNameTag nametag = player.gameObject.GetComponentInChildren<PlayerNameTag>(true);
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
                case Team.Team5: team5Names.Add(nameToDisplay); 
                break;
                case Team.Team6: team6Names.Add(nameToDisplay); 
                break;
                case Team.Team7: team7Names.Add(nameToDisplay); 
                break;
                case Team.Team8: team8Names.Add(nameToDisplay); 
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
        string text = $"Leaderboard:\n";
        
        if (team1Names.Count > 0) 
            text += $"{GetTeamString(team1Names, "")}: {team1Score.value} pts\n";
        if (team2Names.Count > 0) 
            text += $"{GetTeamString(team2Names, "")}: {team2Score.value} pts\n";
        if (team3Names.Count > 0) 
            text += $"{GetTeamString(team3Names, "")}: {team3Score.value} pts\n";
        if (team4Names.Count > 0) 
            text += $"{GetTeamString(team4Names, "")}: {team4Score.value} pts\n";
        if (team5Names.Count > 0) 
            text += $"{GetTeamString(team5Names, "")}: {team5Score.value} pts\n";
        if (team6Names.Count > 0) 
            text += $"{GetTeamString(team6Names, "")}: {team6Score.value} pts\n";
        if (team7Names.Count > 0) 
            text += $"{GetTeamString(team7Names, "")}: {team7Score.value} pts\n";
        if (team8Names.Count > 0) text += $"{GetTeamString(team8Names, "")}: {team8Score.value} pts\n";

        if (leaderboardText != null) 
            leaderboardText.text = text;
        if (finalLeaderboardText != null) 
            finalLeaderboardText.text = text;
    }

    [ObserversRpc]
    public void RPC_UpdateLeaderboard() 
    {
        if (!isServer)
        {
            allPlayers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>().ToList();
        }
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

        var controllers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerController>();
        foreach (var controller in controllers)
        {
            controller.IsMovementEnabled = false;
        }
    }

    // Leave to Lobby
    public void LeaveToLobby()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbySample");
    }
    
    public int GetPlayerIndex(PlayerID id)
    {
        return registeredPlayers.IndexOf(id);
    }

    public void EvaluateRoundCosmetics()
    {
        if (!isServer || allPlayers.Count == 0) 
            return;

        int highestScore = int.MinValue;
        int lowestScore = int.MaxValue;

        foreach (var player in allPlayers)
        {
            int score = GetScoreForTeam(player.team);
            if (score > highestScore) highestScore = score;
            if (score < lowestScore) lowestScore = score;
        }

        foreach (var player in allPlayers)
        {
            int score = GetScoreForTeam(player.team);
            
            bool getsCrown = (score == highestScore && score > 0); 
            bool getsNose = (score == lowestScore && score < highestScore);

            PlayerCosmetics cosmetics = player.gameObject.GetComponent<PlayerCosmetics>();
            if (cosmetics != null)
            {
                cosmetics.SetCosmeticsServerAuth(getsCrown, getsNose);
            }
        }
    }

    private int GetScoreForTeam(Team t)
    {
        return t switch
        {
            Team.Team1 => team1Score.value,
            Team.Team2 => team2Score.value,
            Team.Team3 => team3Score.value,
            Team.Team4 => team4Score.value,
            Team.Team5 => team5Score.value,
            Team.Team6 => team6Score.value,
            Team.Team7 => team7Score.value,
            Team.Team8 => team8Score.value,
            _ => 0
        };
    }

}
