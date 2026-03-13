using UnityEngine;
using System.Collections.Generic;
using GinjaGaming.FinalCharacterController;
using PurrNet;

[System.Serializable]
public class RoundSpawnData
{
    public Transform[] spawnPoints;
}
public class SpawnManager : NetworkBehaviour, IProvideSpawnPoints
{
    [Header("Settings")]
    [SerializeField] private RoundSpawnData[] roundSpawns;

    private PlayerSpawner playerSpawner;

    private void Awake()
    {
        if (playerSpawner == null)
            playerSpawner = Object.FindFirstObjectByType<PlayerSpawner>();

        if (playerSpawner != null)
            playerSpawner.SetRespawnPointProvider(this);
        else
            Debug.LogError("[SpawnManager] No PlayerSpawner found in scene.");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (playerSpawner != null)
            playerSpawner.ResetSpawnPointProvider();
    }

    public SpawnPoint NextSpawnPoint(PlayerID player, SceneID scene)
    {
        Debug.Log($"[SpawnManager] PlayerID raw value: {player.id.value}");
        
        int playerIndex = Mathf.Abs((int)(player.id.value - 1));
        Transform deskTransform = GetSpawnTransform(playerIndex, 1);
        
        if (deskTransform == null) {
            Debug.LogError("[SpawnManager] Failsafe: Round 1 Spawns not setup properly!");
            return new SpawnPoint() { 
                position = new Vector3(90f, 10f, 0), 
                rotation = Quaternion.identity 
            };
        }

        Vector3 safePosition = deskTransform.position + Vector3.up * 0.5f;
        return new SpawnPoint() { 
            position = safePosition, 
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
            Vector3 pos = GetSpawnPosition(i, currentRound);
            pos.y += 0.5f;
            allPlayers[i].Motor.SetPosition(pos);
            var network = allPlayers[i].GetComponent<NetworkIdentity>();
            if (network != null && network.owner.HasValue)
                Rpc_TeleportPlayer(network.owner.Value, pos);

        }
        Debug.Log($"[Server] Teleported {allPlayers.Length} players accurately to their desks for Round {currentRound}.");
    }

    [TargetRpc]
    private void Rpc_TeleportPlayer(PlayerID player, Vector3 position) {

        var allFound = Object.FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);
        foreach (var network in allFound)
        {
            if (network.isOwner) {
                var p = network.GetComponent<MyCharacterController>();
                if (p != null) {
                    p.Motor.SetPosition(position);
                    break;
                }
            }
        }
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