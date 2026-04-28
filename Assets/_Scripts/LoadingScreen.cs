using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using PurrLobby;
using PurrNet.Logging;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private GameObject loading1;
    public static LoadingScreen Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        if (loading1 != null)
            loading1.SetActive(true);
    }

    public bool IsLoading => loading1 != null && loading1.activeSelf;

    public void Hide()
    {
        if (loading1 != null)
            loading1.SetActive(false);
    }

    // private void Update()
    // {
    //     if (loading1 == null)
    //     {
    //         return;
    //     }

    //     var manager = NetworkManager.main;
    //     if (manager == null || loading1 == null)
    //     {
    //         return;
    //     }
            
    //     bool show = ConnectionStarter.IsClientConnectionInProgress 
    //              || ConnectionStarter.IsHostStartingUp
    //              || (!manager.isServer && manager.clientState == ConnectionState.Connecting);
        
    //     if (loading1.activeSelf != show) {
    //         loading1.SetActive(show);
    //     }
    // }
}