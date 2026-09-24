using PurrNet;
using PurrNet.Transports;
using PurrNet.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PurrLobby
{

    public class SceneSwitcher : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [PurrScene, SerializeField] private string nextScene;
        [Tooltip("Automatically switch scene when OnAllReady event fires (recommended for Unity Relay)")]
        [SerializeField] private bool subscribeToOnAllReady = true;

        private bool _hasAlreadySwitched = false;

        private void Start()
        {
            if (subscribeToOnAllReady && lobbyManager != null)
            {

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

        public void SwitchScene()
        {
            if (_hasAlreadySwitched) return;

            var lobby = lobbyManager.CurrentLobby;
            if (IsSteamProvider() && !lobby.IsOwner)
            {
                return;
            }

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

            else if (NetworkManager.main == null || (!NetworkManager.main.isServer && !NetworkManager.main.isClient && NetworkManager.main.clientState == ConnectionState.Disconnected))
            {
                _hasAlreadySwitched = true;
                SceneManager.LoadSceneAsync(nextScene);
            }
        }

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
