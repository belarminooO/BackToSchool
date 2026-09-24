using System.Collections;
using UnityEngine;
using PurrNet;
using PurrLobby;

public class DevDirectConnect : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;

    private IEnumerator Start()
    {
        var lobbyHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (lobbyHolder != null && lobbyHolder.CurrentLobby.IsValid)
        {
            Debug.Log("[DEV] Lobby active - skipping DevDirectConnect (Steam flow active)");
            yield break;
        }

        bool isClone = false;

#if UNITY_EDITOR
        isClone = ParrelSync.ClonesManager.IsClone();
#endif

        if (!isClone)
        {
            Debug.Log("[DEV] Starting as HOST");
            networkManager.StartHost();
        }
        else
        {
            Debug.Log("[DEV] Waiting for host then starting as CLIENT");
            yield return new WaitForSeconds(1.5f);
            networkManager.StartClient();
        }
    }
}
