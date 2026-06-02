using PurrNet;
using PurrNet.Transports;
using PurrNet.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PurrLobby
{
    /// <summary>
    /// Handles scene transitions from lobby to game scene.
    /// 
    /// Can be used in two ways:
    /// 1. Automatic: Set subscribeToOnAllReady = true to auto-switch when all players ready
    /// 2. Manual: Call SwitchScene() from a button or script
    /// 
    /// For Unity Lobby with Relay: Ensures relay is allocated before scene switch
    /// For Steam/Other providers: Standard scene transition after lobby setup
    /// </summary>
    public class SceneSwitcher : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [PurrScene, SerializeField] private string nextScene;
        [Tooltip("Automatically switch scene when OnAllReady event fires (recommended for Unity Relay)")]
        [SerializeField] private bool subscribeToOnAllReady = true;

        // Prevents duplicate scene transitions
        private bool _hasAlreadySwitched = false;

        private void Start()
        {
            if (subscribeToOnAllReady && lobbyManager != null)
            {
                // Subscribe to OnAllReady event - fires after SetAllReadyAsync() completes
                // For Unity Relay: This ensures relay allocation is done before scene switch
                // For other providers: This ensures lobby setup is complete
                lobbyManager.OnAllReady.AddListener(SwitchScene);
            }

            if (lobbyManager != null)
            {
                lobbyManager.OnRoomUpdated.AddListener(OnRoomUpdated);
            }
        }

        private void OnDestroy()
        {
            if (lobbyManager != null)
            {
                lobbyManager.OnAllReady.RemoveListener(SwitchScene);
                lobbyManager.OnRoomUpdated.RemoveListener(OnRoomUpdated);
            }
        }

        /// <summary>
        /// Switches to the game scene.
        /// Safe to call multiple times - will only switch once.
        /// </summary>
        // public void SwitchScene()
        // {
        //     // Prevent duplicate scene switches
        //     if (_hasAlreadySwitched)
        //     {
        //         PurrLogger.LogWarning("SwitchScene already called - ignoring duplicate", this);
        //         return;
        //     }
            
        //     _hasAlreadySwitched = true;
            
        //     if (string.IsNullOrEmpty(nextScene))
        //     {
        //         PurrLogger.LogError("Next scene name is not set!", this);
        //         return;
        //     }

        //     PurrLogger.Log($"Switching to scene: {nextScene}", this);
            
        //     // Mark lobby as started to prevent new players from joining
        //     if (lobbyManager != null)
        //     {
        //         lobbyManager.SetLobbyStarted();
        //     }

        //     // ---------------------------------------- //

        //     if (NetworkManager.main != null && NetworkManager.main.isServer)
        //     {
        //         _hasAlreadySwitched = true;
        //         PurrLogger.Log($"[Server] Host is loading networked scene: {nextScene}", this);
        //         NetworkManager.main.sceneModule.LoadSceneAsync(nextScene);
        //     }
        //     else if (NetworkManager.main == null || (!NetworkManager.main.isServer && !NetworkManager.main.isClient))
        //     {
        //         _hasAlreadySwitched = true;
        //         SceneManager.LoadSceneAsync(nextScene);
        //     }


        //     // ----------------------------------------- //

        //     // Load game scene - ConnectionStarter in new scene will handle network initialization
        //     //SceneManager.LoadSceneAsync(nextScene);
        // }

        public void SwitchScene()
        {
            if (_hasAlreadySwitched) return;

            var lobby = lobbyManager.CurrentLobby;
            if (IsSteamProvider() && !lobby.IsOwner)
            {
                return;
            }

            // If we are a client but still connecting, WAIT.
            // The SceneModule will automatically take us to the new scene once connected.
            if (NetworkManager.main != null && NetworkManager.main.clientState == ConnectionState.Connecting)
            {
                PurrLogger.Log("Client is still connecting... waiting for network to handle scene switch.", this);
                return;
            }

            if (IsSteamProvider())
            {
                lobbyManager.SetLobbyStarted();
            }

            LoadGameScene();
        }

        private void OnRoomUpdated(Lobby lobby)
        {
            if (!IsSteamProvider() || lobby.IsOwner || !HasStarted(lobby))
            {
                return;
            }

            LoadGameScene();
        }

        private bool IsSteamProvider()
        {
            return lobbyManager?.CurrentProvider?.GetType().Name == "SteamLobbyProvider";
        }

        private static bool HasStarted(Lobby lobby)
        {
            return lobby.Properties != null &&
                   lobby.Properties.TryGetValue("Started", out var value) &&
                   bool.TryParse(value, out var started) &&
                   started;
        }

        private void LoadGameScene()
        {
            if (_hasAlreadySwitched) return;

            CaptureExpectedPlayerCount();

            if (NetworkManager.main != null && NetworkManager.main.isServer)
            {
                _hasAlreadySwitched = true;
                PurrLogger.Log($"[Server] Host is loading networked scene: {nextScene}", this);
                NetworkManager.main.sceneModule.LoadSceneAsync(nextScene);
            }
            // Only load offline if we are NOT trying to be a client/server
            else if (NetworkManager.main == null || (!NetworkManager.main.isServer && !NetworkManager.main.isClient && NetworkManager.main.clientState == ConnectionState.Disconnected))
            {
                _hasAlreadySwitched = true;
                SceneManager.LoadSceneAsync(nextScene);
            }
        }

        // Snapshot the lobby size right before the scene change so the game scene has a
        // count that survives any Steam disconnect event that might fire during the switch.
        private void CaptureExpectedPlayerCount()
        {
            if (lobbyManager == null) return;
            var lobby = lobbyManager.CurrentLobby;
            if (!lobby.IsValid || lobby.Members == null || lobby.Members.Count <= 0) return;

            LobbyDataHolder.CapturedExpectedPlayerCount = lobby.Members.Count;
            PurrLogger.Log($"[SceneSwitcher] Captured expected player count: {lobby.Members.Count}", this);

#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
            if (ulong.TryParse(lobby.LobbyId, out var rawLobbyId))
            {
                var lobbyId = new Steamworks.CSteamID(rawLobbyId);
                var ownerId = Steamworks.SteamMatchmaking.GetLobbyOwner(lobbyId);

                if (ownerId != Steamworks.CSteamID.Nil)
                {
                    LobbyDataHolder.CapturedHostSteamId = ownerId.m_SteamID.ToString();
                    PurrLogger.Log($"[SceneSwitcher] Captured host Steam ID: {LobbyDataHolder.CapturedHostSteamId}", this);
                }
            }
#endif
        }
    }
}
