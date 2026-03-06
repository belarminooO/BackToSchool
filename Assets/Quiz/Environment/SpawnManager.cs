using UnityEngine;
using System.Collections.Generic;
using GinjaGaming.FinalCharacterController;
using PurrNet;

[System.Serializable]
public class RoundSpawnData
{
    public Transform[] spawnPoints;
}
public class SpawnManager : MonoBehaviour, IProvideSpawnPoints 
{
    [Header("Settings")]
    [Tooltip("Size is the number of rounds. Element 0 is Round 1, Element 1 is Round 2, etc.")]
    [SerializeField] private RoundSpawnData[] roundSpawns;
    
    private PlayerSpawner _playerSpawner;

    private void Start()
    {
        _playerSpawner = Object.FindFirstObjectByType<PlayerSpawner>();
        
        if (_playerSpawner != null)
        {
            _playerSpawner.SetRespawnPointProvider(this);
        }
    }

    private void OnDestroy()
    {
        if (_playerSpawner != null)
        {
            _playerSpawner.ResetSpawnPointProvider();
        }
    }

    public SpawnPoint NextSpawnPoint(PlayerID player, SceneID scene)
    {
        Debug.Log($"[SpawnManager] PlayerID raw value: {player.id.value}");
        int playerIndex = (int)(player.id.value - 1);
        Transform deskTransform = GetSpawnTransform(playerIndex, 1);
        if (deskTransform == null) {
            Debug.LogError("[SpawnManager] Failsafe: Round 1 Spawns not setup properly!");
            return new SpawnPoint() { position = Vector3.zero, rotation = Quaternion.identity };
        }
        return new SpawnPoint() { 
            position = deskTransform.position, 
            rotation = deskTransform.rotation 
        };
       
    }

    private Transform GetSpawnTransform(int playerIndex, int currentRound)
    {
        if (roundSpawns == null || roundSpawns.Length == 0) return null;
        
        int arrayIndex = Mathf.Clamp(currentRound - 1, 0, roundSpawns.Length - 1);
        Transform[] currentSpawnPoints = roundSpawns[arrayIndex].spawnPoints;
        
        if (currentSpawnPoints == null || currentSpawnPoints.Length == 0) return null;
        
        return currentSpawnPoints[playerIndex % currentSpawnPoints.Length];
    }

    public Vector3 GetSpawnPosition(int playerIndex, int currentRound) 
    {
        if (roundSpawns == null || roundSpawns.Length == 0) {
            return Vector3.zero;
        }
        int arrayIndex = Mathf.Clamp(currentRound - 1, 0, roundSpawns.Length - 1);
        
        Transform[] currentSpawnPoints = roundSpawns[arrayIndex].spawnPoints;
        if (currentSpawnPoints == null || currentSpawnPoints.Length == 0) {
            return Vector3.zero;
        }
        Transform specificSpawn = currentSpawnPoints[playerIndex % currentSpawnPoints.Length];
        return specificSpawn.position;
    }
    public void TeleportAllPlayersToRound(MyCharacterController[] allPlayers, int currentRound)
    {
        for (int i = 0; i < allPlayers.Length; i++)
        {
            //allPlayers[i].transform.position = GetSpawnPosition(i, currentRound);
        
            // Specific for Kinematic Controller
            allPlayers[i].Motor.SetPosition(GetSpawnPosition(i, currentRound));

        
        }
        Debug.Log($"[Server] Teleported {allPlayers.Length} players accurately to their desks for Round {currentRound}.");
    }
    public void TeleportStragglersToRound(List<MyCharacterController> stragglers, List<MyCharacterController> allPlayers, int currentRound) 
    {
        for (int i = 0; i < stragglers.Count; i++)
        {
            int originalIndex = allPlayers.IndexOf(stragglers[i]);
            
            if (originalIndex != -1)
            {
                //stragglers[i].transform.position = GetSpawnPosition(originalIndex, currentRound);
            
                // Specific for Kinematic Controller
                stragglers[i].Motor.SetPosition(GetSpawnPosition(originalIndex, currentRound));            
            }
        }
    }
}