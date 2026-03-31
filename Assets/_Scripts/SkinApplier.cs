using UnityEngine;
using PurrNet;
using System.Reflection;

public class SkinApplier : MonoBehaviour
{
    private void Awake()
    {
        if (LobbyCharacterDisplay.AssignedPrefab == null) return;

        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null) return;

        typeof(PlayerSpawner)
            .GetField("_playerPrefab", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(spawner, LobbyCharacterDisplay.AssignedPrefab);
    }
}
