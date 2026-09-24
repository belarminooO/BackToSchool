using System.Collections;
using PurrNet;
using PurrNet.Logging;
using PurrNet.Transports;
using UnityEngine;

#if UTP_LOBBYRELAY
using PurrNet.UTP;
using Unity.Services.Relay.Models;
#endif

namespace PurrLobby
{

    public class ConnectionStarter : MonoBehaviour
    {
        public static bool IsClientConnectionInProgress { get; private set; }
        public static bool IsHostStartingUp { get; private set; } 

        private NetworkManager _networkManager;
        private LobbyDataHolder _lobbyDataHolder;

        private bool _hasStarted = false;
        private bool _useUnityRelay = false;

        private void Awake()
        {

            if (!TryGetComponent(out _networkManager))
            {

                _networkManager = FindFirstObjectByType<NetworkManager>();
                if (!_networkManager)
                {
                    PurrLogger.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
                    return;
                }

            }

            _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (!_lobbyDataHolder)
            {
                PurrLogger.LogError($"Failed to get {nameof(LobbyDataHolder)} component.", this);
                return;
            }

            if (_networkManager == null)
            {
                PurrLogger.LogError("NetworkManager could not be found!", this);
            }

#if UTP_LOBBYRELAY

            _useUnityRelay = IsUsingUTPTransport();

            if (_useUnityRelay)
            {

                PurrLogger.Log("Unity Relay detected - configuring relay data before NetworkManager starts", this);
                _networkManager.enabled = false;
                _ = ConfigureRelayData();
                return;
            }
#endif

        }

#if UTP_LOBBYRELAY
        private bool IsUsingUTPTransport()
        {
            if (_networkManager.transport is UTPTransport)
                return true;

            if (_networkManager.transport is CompositeTransport composite)
            {
                foreach (var transport in composite.transports)
                {
                    if (transport is UTPTransport)
                        return true;
                }
            }

            return false;
        }
#endif

        private void Start()
        {

            if (_useUnityRelay)
                return;

            StartNetworkStandard();
        }

        private void StartNetworkStandard()
        {

            if (_networkManager.serverState != ConnectionState.Disconnected || 
                _networkManager.clientState != ConnectionState.Disconnected)
            {
                PurrLogger.Log("Network is already running. Skipping duplicate start.", this);
                _hasStarted = true;
                return;
            }

            if (!_networkManager)
            {
                PurrLogger.LogError($"Failed to start connection. {nameof(NetworkManager)} is null!", this);
                return;
            }

            if (!_lobbyDataHolder)
            {
                PurrLogger.LogError($"Failed to start connection. {nameof(LobbyDataHolder)} is null!", this);
                return;
            }

            if (!_lobbyDataHolder.CurrentLobby.IsValid)
            {
                PurrLogger.LogError($"Failed to start connection. Lobby is invalid!", this);
                return;
            }

            if (_networkManager.transport is PurrTransport)
            {
                (_networkManager.transport as PurrTransport).roomName = _lobbyDataHolder.CurrentLobby.LobbyId;
            }

#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
            TryApplySteamHostAddress();
#endif

            _hasStarted = true;

            if (_lobbyDataHolder.CurrentLobby.IsOwner)
            {
                StartCoroutine(StartHost()); 
            }
            else
            {
                StartCoroutine(StartClient());
            }

        }

#if UTP_LOBBYRELAY

        private async System.Threading.Tasks.Task ConfigureRelayData()
        {
            if (!_networkManager)
            {
                PurrLogger.LogError($"Failed to configure relay. {nameof(NetworkManager)} is null!", this);
                return;
            }

            PurrLogger.Log("NetworkManager found", this);

            if (!_lobbyDataHolder)
            {
                PurrLogger.LogError($"Failed to configure relay. {nameof(LobbyDataHolder)} is null!", this);
                return;
            }

            PurrLogger.Log($"LobbyDataHolder found. Lobby IsValid: {_lobbyDataHolder.CurrentLobby.IsValid}", this);

            if (!_lobbyDataHolder.CurrentLobby.IsValid)
            {
                PurrLogger.LogError($"Failed to configure relay. Lobby is invalid!", this);
                return;
            }

            PurrLogger.Log($"Checking transport type: {_networkManager.transport?.GetType().Name ?? "NULL"}", this);

            UTPTransport utpTransport = null;

            if(_networkManager.transport is UTPTransport) {
                utpTransport = _networkManager.transport as UTPTransport;
            }
            else if(_networkManager.transport is CompositeTransport) {
                var composite = _networkManager.transport as CompositeTransport;
                foreach(var transport in composite.transports) {
                    if(transport is UTPTransport) {
                        utpTransport = transport as UTPTransport;
                        PurrLogger.Log("Found UTPTransport inside CompositeTransport", this);
                        break;
                    }
                }
            }

            if(utpTransport != null) {
                var lobby = _lobbyDataHolder.CurrentLobby;

                PurrLogger.Log($"Configuring UTP Relay: IsOwner={lobby.IsOwner}, ServerObject={(lobby.ServerObject != null ? "EXISTS" : "NULL")}, JoinCode={(lobby.Properties.ContainsKey("JoinCode") ? lobby.Properties["JoinCode"] : "MISSING")}", this);

                if(lobby.ServerObject == null && lobby.IsOwner) {
                    PurrLogger.LogError("Cannot configure UTP server: Relay ServerObject is null! Make sure SetAllReadyAsync() was called on the lobby provider.", this);
                    return;
                }

                if(!lobby.Properties.ContainsKey("JoinCode") || 
                   string.IsNullOrEmpty(lobby.Properties["JoinCode"])) {
                    PurrLogger.LogError("Cannot configure UTP client: JoinCode is missing! Make sure SetAllReadyAsync() was called on the lobby provider.", this);
                    return;
                }

                if(lobby.IsOwner) {

                    PurrLogger.Log("Initializing UTP Relay Server (for host)...", this);
                    bool serverInit = utpTransport.InitializeRelayServer((Allocation)lobby.ServerObject);
                    PurrLogger.Log($"Relay Server initialized: {serverInit}", this);

                    PurrLogger.Log("Initializing UTP Relay Client for host (required for P2P mode)...", this);

                    try {
                        bool clientInit = await utpTransport.InitializeRelayClient(lobby.Properties["JoinCode"]);
                        PurrLogger.Log($"Relay Client initialized for host: {clientInit}", this);
                    }
                    catch (System.Exception ex) {
                        PurrLogger.LogError($"Failed to initialize relay client for host: {ex.Message}", this);
                    }
                }
                else {

                    PurrLogger.Log($"Initializing UTP Relay Client with JoinCode: {lobby.Properties["JoinCode"]}", this);

                    try {
                        bool clientInit = await utpTransport.InitializeRelayClient(lobby.Properties["JoinCode"]);
                        PurrLogger.Log($"Relay Client initialized: {clientInit}", this);
                    }
                    catch (System.Exception ex) {
                        PurrLogger.LogError($"Failed to initialize relay client: {ex.Message}", this);
                    }
                }

                StartNetworkAfterRelayConfig();
            }
        }

        private void StartNetworkAfterRelayConfig()
        {

            if (_hasStarted)
            {
                PurrLogger.LogWarning("StartNetworkAfterRelayConfig() already called - ignoring duplicate", this);
                return;
            }

            if (!_networkManager || !_lobbyDataHolder || !_lobbyDataHolder.CurrentLobby.IsValid)
                return;

            _hasStarted = true;

            _networkManager.enabled = true;

            if(_lobbyDataHolder.CurrentLobby.IsOwner)
            {

                PurrLogger.Log("Starting as Host", this);
                _networkManager.StartHost();
            }
            else
            {

                PurrLogger.Log("Starting as Client", this);
                StartCoroutine(StartClient());
            }
        }
#endif

#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
        private bool TryApplySteamHostAddress()
        {
            string hostId = LobbyDataHolder.CapturedHostSteamId;

            if (string.IsNullOrWhiteSpace(hostId) || hostId == "0")
            {
                PurrLogger.LogError("[ConnectionStarter] Captured Steam host ID is missing.", this);
                return false;
            }

            bool applied = false;

            void TrySetAddress(object transport)
            {
                if (transport is PurrNet.Steam.SteamTransport steamTransport)
                {
                    steamTransport.address = hostId;
                    applied = true;
                }
            }

            TrySetAddress(_networkManager.transport);
            if (_networkManager.transport is CompositeTransport composite)
            {
                foreach (var transport in composite.transports)
                {
                    TrySetAddress(transport);
                }
            }

            if (applied)
            {
                PurrLogger.Log($"[ConnectionStarter] Applied captured Steam host ID: {hostId}", this);
            }
            else
            {
                PurrLogger.LogError("[ConnectionStarter] Steam transport was not found.", this);
            }

            return applied;
        }
#endif

        private IEnumerator StartHost()
        {
            IsHostStartingUp = true;
            _networkManager.StartHost();
            float elapsed = 0f;

            while (_networkManager.serverState != ConnectionState.Connected && elapsed < 10f)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            IsHostStartingUp = false;
            if (_networkManager.serverState != ConnectionState.Connected)
                PurrLogger.LogError("[ConnectionStarter] Host failed to reach Connected state within 10s.", this);
        }

        private IEnumerator StartClient()
        {
            IsClientConnectionInProgress = true;

            int attempts = 0;
            int maxAttempts = 8; 

            yield return new WaitForSeconds(3f); 

            while (attempts < maxAttempts)
            {
                _networkManager.StartClient();

                float gracePeriod = 0f;
                while (_networkManager.clientState == ConnectionState.Disconnected && gracePeriod < 2f)
                {
                    yield return null; 
                    gracePeriod += Time.deltaTime;
                }

                float timeoutCounter = 0f;
                float maxTimeout = 30f; 

                while (_networkManager.clientState == ConnectionState.Connecting 
                    && timeoutCounter < maxTimeout)
                {
                    yield return new WaitForSeconds(0.5f);
                    timeoutCounter += 0.5f;
                }

                if (_networkManager.clientState == ConnectionState.Connected)
                {

                    IsClientConnectionInProgress = false;
                    yield break;
                }

                attempts++;
                PurrLogger.Log($"[ConnectionStarter] Attempt {attempts}/{maxAttempts} failed. Current state: {_networkManager.clientState}. Retrying...", this);
                _networkManager.StopClient();

                yield return new WaitForSeconds(6f); 

#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
                TryApplySteamHostAddress();
#endif
            }

            PurrLogger.LogError("[ConnectionStarter] Client failed to connect after all retry attempts. Returning to lobby.", this);
            IsClientConnectionInProgress = false;
            _networkManager.StopClient();
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbySample");
        }

    }
}
