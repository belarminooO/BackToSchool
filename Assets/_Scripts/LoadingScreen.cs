using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using PurrLobby;
using PurrNet.Logging;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private GameObject loading1;

    void Awake()
    {
        if (loading1 == null)
        {
            PurrLogger.LogError("LoadingScreen: loading1 is not assigned.", this);
            return;
        }
        loading1.SetActive(false);
    }

    private void Update()
    {
        if (loading1 == null)
        {
            return;
        }

        var manager = NetworkManager.main;
        if (manager == null || loading1 == null)
        {
            return;
        }
            
        bool show = ConnectionStarter.IsClientConnectionInProgress || (!manager.isServer && manager.clientState == ConnectionState.Connecting);
        if (loading1.activeSelf != show) {
            loading1.SetActive(show);
        }
    }
}