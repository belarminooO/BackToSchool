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

            yield break;
        }

        bool isClone = false;

#if UNITY_EDITOR
        isClone = ParrelSync.ClonesManager.IsClone();
#endif

        if (!isClone)
        {

            networkManager.StartHost();
        }
        else
        {

            yield return new WaitForSeconds(1.5f);
            networkManager.StartClient();
        }
    }
}
