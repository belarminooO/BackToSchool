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
        lobbyManager.OnRoomJoined.AddListener(Refresh);
        lobbyManager.OnRoomUpdated.AddListener(Refresh);
        lobbyManager.OnRoomLeft.AddListener(ClearAll);
    }

    private void OnDestroy()
    {
        lobbyManager.OnRoomJoined.RemoveListener(Refresh);
        lobbyManager.OnRoomUpdated.RemoveListener(Refresh);
        lobbyManager.OnRoomLeft.RemoveListener(ClearAll);
    }

    private void Refresh(Lobby lobby)
    {
        if (this == null) return;
        if (!lobby.IsValid || lobby.Members == null) return;

        foreach (var go in _spawned) if (go) Destroy(go);
        _spawned.Clear();

#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
        string myId = SteamUser.GetSteamID().m_SteamID.ToString();
#else
        string myId = "";
#endif

        for (int i = 0; i < lobby.Members.Count && i < spawnPoints.Count && characterPrefabs.Count > 0; i++)
        {

            if (spawnPoints[i] == null)
            {
                Debug.LogWarning($"[LobbyCharacterDisplay] spawnPoints[{i}] is null or destroyed — skipping.");
                continue;
            }

            string id = lobby.Members[i].Id;

            if (!_assigned.ContainsKey(id))
                _assigned[id] = Mathf.Abs((id + lobby.LobbyId).GetHashCode()) % characterPrefabs.Count;

            int idx = _assigned[id];

            Debug.Log($"[LobbyCharacterDisplay] Player {id} → skin {idx} (total prefabs: {characterPrefabs.Count})");

            if (id == myId)
            {
                AssignedPrefab = characterPrefabs[idx];
                SkinSelection.Instance?.Set(idx);
                Debug.Log($"[SkinSelection] Set index={idx} for myId={myId} | Instance={(SkinSelection.Instance == null ? "NULL — add SkinSelection to lobby scene!" : "OK")}");
            }

            GameObject go = Instantiate(characterPrefabs[idx], spawnPoints[i].position, spawnPoints[i].rotation);

            TMP_Text label = go.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = GetName(lobby.Members[i]);

            _spawned.Add(go);
        }
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

    private void ClearAll()
    {
        if (this == null) return;
        foreach (var go in _spawned) if (go) Destroy(go);
        _spawned.Clear();
        _assigned.Clear();
        AssignedPrefab = null;
    }

    private void LateUpdate()
    {
        if (Camera.main == null) return;
        foreach (var go in _spawned)
        {
            if (go == null) {
                continue;
            }
            Canvas tag = go.GetComponentInChildren<Canvas>();
            if (tag)
            {
                tag.transform.rotation = Quaternion.LookRotation(tag.transform.position - Camera.main.transform.position); 
            }
        }
    }
}
