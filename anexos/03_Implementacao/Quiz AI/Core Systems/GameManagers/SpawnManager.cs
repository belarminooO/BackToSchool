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
    [Header("Inside Room Spawnpoints")]
    [SerializeField] private RoundSpawnData[] roundSpawns;

    [Header("Outside Room Spawnpoints")]
    [SerializeField] private RoundSpawnData[] outsideRoundSpawns;
    private PlayerSpawner playerSpawner;

    [Header("Jail Spawnpoints")]
    [SerializeField] private Transform[] jailSpawns;

    private void Awake()
    {
        if (playerSpawner == null)
            playerSpawner = Object.FindFirstObjectByType<PlayerSpawner>();

        if (playerSpawner != null)
            playerSpawner.SetRespawnPointProvider(this);
        else

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (playerSpawner != null)
            playerSpawner.ResetSpawnPointProvider();
    }

    public SpawnPoint NextSpawnPoint(PlayerID player, SceneID scene)
    {

        int playerIndex = Mathf.Abs((int)(player.id.value - 1));
        Transform deskTransform = GetSpawnTransform(playerIndex, 1);

        if (deskTransform == null) {

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
    public void TeleportAllPlayersToRound(IPlayerController[] allPlayers, int currentRound)
    {
        for (int i = 0; i < allPlayers.Length; i++)
        {
            int originalIndex = i; 
            if (originalIndex != -1) {

                Vector3 pos = GetSpawnPosition(originalIndex, currentRound);
                pos.y += 0.5f;
                allPlayers[i].TeleportTo(pos);

                var network = allPlayers[i].gameObject.GetComponent<NetworkIdentity>();
                if (network != null && network.owner.HasValue)
                    Rpc_TeleportPlayer(network.owner.Value, pos);
            }
        }

    }

    public void TeleportAllPlayersToOutside(IPlayerController
    [] allPlayers, int currentRound)
    {
        if (outsideRoundSpawns == null || outsideRoundSpawns.Length == 0)
        {

            return;
        }

        int arrayIndex = Mathf.Clamp(currentRound - 1, 0, outsideRoundSpawns.Length - 1);
        Transform[] spawnPoints = outsideRoundSpawns[arrayIndex].spawnPoints;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {

            return;
        }

        for (int i = 0; i < allPlayers.Length; i++)
        {
            Transform spawn = spawnPoints[i % spawnPoints.Length];
            Vector3 pos = spawn.position + Vector3.up * 0.5f;
            allPlayers[i].TeleportTo(pos);
            var network = allPlayers[i].gameObject.GetComponent<NetworkIdentity>();
            if (network != null && network.owner.HasValue)
                Rpc_TeleportPlayer(network.owner.Value, pos);
        }

    }

    public void TeleportPlayerToOutside(IPlayerController player, int currentRound)
    {
        if (outsideRoundSpawns == null || outsideRoundSpawns.Length == 0) 
        {
            return;
        }

        int arrayIndex = Mathf.Clamp(currentRound - 1, 0, outsideRoundSpawns.Length - 1);

        Transform[] spawnPoints = outsideRoundSpawns[arrayIndex].spawnPoints;
        if (spawnPoints == null || spawnPoints.Length == 0) {
            return;
        }

        var scoreManager = QuizGameManager.instance?.scoreManager;
        if (scoreManager == null) {
           return; 
        } 

        int playerIndex = scoreManager.allPlayers.IndexOf(player);
        if (playerIndex == -1) {
            return;
        }

        Transform spawn = spawnPoints[playerIndex % spawnPoints.Length];
        Vector3 pos = spawn.position + Vector3.up * 0.5f;

        player.TeleportTo(pos);
        var network = player.gameObject.GetComponent<NetworkIdentity>();
        if (network != null && network.owner.HasValue)
            Rpc_TeleportPlayer(network.owner.Value, pos);
    }

    [TargetRpc]
    private void Rpc_TeleportPlayer(PlayerID player, Vector3 position) {

        var allFound = Object.FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);
        foreach (var network in allFound)
        {
            if (network.isOwner) {
                var p = network.GetComponent<IPlayerController>();
                if (p != null) 
                {
                    p.TeleportTo(position); 
                    break; 
                }
            }
        }
    }

    public void TeleportStragglersToRound(List<IPlayerController> stragglers, List<IPlayerController> allPlayers, int currentRound) 
    {
        for (int i = 0; i < stragglers.Count; i++)
        {
            int originalIndex = allPlayers.IndexOf(stragglers[i]);

            if (originalIndex != -1)
            {

                stragglers[i].TeleportTo(GetSpawnPosition(originalIndex, currentRound));            
            }
        }
    }

    public Vector3 GetJailPosition(int currentRound)
    {
        if (jailSpawns == null || jailSpawns.Length == 0) 
            return Vector3.zero;

        int arrayIndex = Mathf.Clamp(currentRound - 1, 0, jailSpawns.Length - 1);
        Transform point = jailSpawns[arrayIndex];

        if (point == null) 
            return Vector3.zero;

        return point.position;
    }
}