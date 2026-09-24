using UnityEngine;
using PurrNet;
using System.Reflection;

public class SkinApplier : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log($"[SkinApplier] AssignedPrefab = {LobbyCharacterDisplay.AssignedPrefab?.name ?? "NULL"}");

        if (LobbyCharacterDisplay.AssignedPrefab == null)
        {
            Debug.LogError("[SkinApplier] AssignedPrefab is null — PlayerSpawner will use its Inspector prefab. If that's also null, nobody spawns.");
            return;
        }

        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[SkinApplier] PlayerSpawner not found in scene!");
            return;
        }

        var field = typeof(PlayerSpawner).GetField("_playerPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogError("[SkinApplier] Reflection failed: _playerPrefab field not found on PlayerSpawner.");
            return;
        }

        field.SetValue(spawner, LobbyCharacterDisplay.AssignedPrefab);
        Debug.Log($"[SkinApplier] Set PlayerSpawner._playerPrefab to {LobbyCharacterDisplay.AssignedPrefab.name}");
    }

}
