using System.Collections.Generic;
using UnityEngine;
using PurrLobby;
using TMPro;
#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
using Steamworks;
#endif

public class LobbyCharacterDisplay : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private List<GameObject> characterPrefabs = new();
    [SerializeField] private List<Transform> spawnPoints = new();

    public static GameObject AssignedPrefab { get; private set; }

    private readonly List<GameObject> _spawned = new();
    private readonly Dictionary<string, int> _assigned = new();

    private void Start()
    {
        if (lobbyManager == null)
        {
            return;
        }

        lobbyManager.OnRoomJoined.AddListener(Refresh);
        lobbyManager.OnRoomUpdated.AddListener(Refresh);
        lobbyManager.OnRoomLeft.AddListener(ClearAll);
    }

    private void OnDestroy()
    {
        if (lobbyManager == null)
        {
            return;
        }

        lobbyManager.OnRoomJoined.RemoveListener(Refresh);
        lobbyManager.OnRoomUpdated.RemoveListener(Refresh);
        lobbyManager.OnRoomLeft.RemoveListener(ClearAll);
    }

    private void Refresh(Lobby lobby)
    {
        if (!lobby.IsValid || lobby.Members == null)
        {
            return;
        }

        ClearSpawnedCharacters();
        if (characterPrefabs.Count == 0)
        {
            return;
        }

        string myId = GetLocalPlayerId();
        int memberCount = Mathf.Min(lobby.Members.Count, spawnPoints.Count);

        for (int i = 0; i < memberCount; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {

                continue;
            }

            LobbyUser member = lobby.Members[i];
            int prefabIndex = GetCharacterIndex(member.Id, lobby.LobbyId);

            if (member.Id == myId)
            {
                AssignedPrefab = characterPrefabs[prefabIndex];
                SkinSelection.Instance?.Set(prefabIndex);
            }

            GameObject go = Instantiate(characterPrefabs[prefabIndex], spawnPoint.position, spawnPoint.rotation);

            TMP_Text label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = GetName(member);
            }

            _spawned.Add(go);
        }
    }

    private int GetCharacterIndex(string playerId, string lobbyId)
    {
        if (_assigned.TryGetValue(playerId, out int index))
        {
            return index;
        }

        index = Mathf.Abs((playerId + lobbyId).GetHashCode()) % characterPrefabs.Count;
        _assigned[playerId] = index;
        return index;
    }

    private string GetLocalPlayerId()
    {
#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
        return SteamUser.GetSteamID().m_SteamID.ToString();
#else
        return string.Empty;
#endif
    }

    private string GetName(LobbyUser user)
    {
#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
        if (ulong.TryParse(user.Id, out ulong steamId))
        {
            string name = SteamFriends.GetFriendPersonaName(new CSteamID(steamId));
            if (!string.IsNullOrEmpty(name)) return name;
        }
#endif
        return !string.IsNullOrEmpty(user.DisplayName) ? user.DisplayName : "Player";
    }

    private void ClearSpawnedCharacters()
    {
        foreach (var go in _spawned)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }

        _spawned.Clear();
    }

    private void ClearAll()
    {
        ClearSpawnedCharacters();
        _assigned.Clear();
        AssignedPrefab = null;
    }

    private void LateUpdate()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        foreach (var go in _spawned)
        {
            if (go == null)
            {
                continue;
            }

            Canvas tag = go.GetComponentInChildren<Canvas>();
            if (tag != null)
            {
                tag.transform.rotation = Quaternion.LookRotation(tag.transform.position - mainCamera.transform.position);
            }
        }
    }
}
