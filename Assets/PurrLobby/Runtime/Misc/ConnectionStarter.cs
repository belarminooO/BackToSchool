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
    /// <summary>
    /// Manages network connection initialization for different lobby providers.
    /// 
    /// For Unity Lobby with Relay (UTP_LOBBYRELAY):
    /// - Async relay configuration with proper timing
    /// - NetworkManager disabled until relay is ready
    /// - Host initializes both server AND client relay data for P2P mode
    /// 
    /// For Steam/Other providers:
    /// - Simple synchronous initialization
    /// - Standard NetworkManager startup flow
    /// </summary>
    public class ConnectionStarter : MonoBehaviour
    {
        public static bool IsClientConnectionInProgress { get; private set; }
        public static bool IsHostStartingUp { get; private set; } 

        private NetworkManager _networkManager;
        private LobbyDataHolder _lobbyDataHolder;

        // Prevents duplicate network starts if scene loads multiple times
        private bool _hasStarted = false;
        private bool _useUnityRelay = false;

        private void Awake()
        {

            if (!TryGetComponent(out _networkManager))
            {
                // --------------------------
                _networkManager = FindFirstObjectByType<NetworkManager>();
                if (!_networkManager)
                {
                    PurrLogger.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
                    return;
                }
                // --------------------------
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
            // Check if we're using UTP with relay - requires async configuration
            _useUnityRelay = IsUsingUTPTransport();
            
            if (_useUnityRelay)
            {
                // CRITICAL FIX for Unity Relay: Disable NetworkManager to prevent auto-start
                // Relay data must be configured asynchronously before NetworkManager starts
                PurrLogger.Log("Unity Relay detected - configuring relay data before NetworkManager starts", this);
                _networkManager.enabled = false;
                _ = ConfigureRelayData();
                return;
            }
#endif

            // For non-relay providers (Steam, etc.), use standard synchronous flow
            // NetworkManager will start normally in its Start() method
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
            // Skip Start() if using Unity Relay - ConfigureRelayData() handles startup
            if (_useUnityRelay)
                return;

            // Standard startup for Steam and other non-relay providers
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

            // ----------------------------------------------
// #if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
//             if (_networkManager.transport != null && _networkManager.transport.GetType().Name == "SteamTransport")
//             {
//                 if (ulong.TryParse(_lobbyDataHolder.CurrentLobby.LobbyId, out ulong parsedLobbyId))
//                 {
//                     Steamworks.CSteamID lobbyId = new Steamworks.CSteamID(parsedLobbyId);
//                     Steamworks.CSteamID ownerId = Steamworks.SteamMatchmaking.GetLobbyOwner(lobbyId);
//                     var addressProperty = _networkManager.transport.GetType().GetProperty("address");
//                     if (addressProperty != null && addressProperty.CanWrite)
//                     {
//                         addressProperty.SetValue(_networkManager.transport, ownerId.m_SteamID.ToString());
//                     }
//                 }
//             }
// #endif

#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
            TryApplySteamHostAddress();
#endif
            // ----------------------------------------------

            // Pode-se usar StartHost()
            // if(_lobbyDataHolder.CurrentLobby.IsOwner)
            //     _networkManager.StartServer();
            // StartCoroutine(StartClient());

            // --------------------------
            _hasStarted = true;

            if (_lobbyDataHolder.CurrentLobby.IsOwner)
            {
                StartCoroutine(StartHost()); 
            }
            else
            {
                StartCoroutine(StartClient());
            }
            // --------------------------

        }

#if UTP_LOBBYRELAY
        /// <summary>
        /// Configures Unity Relay data asynchronously for UTP transport.
        /// CRITICAL: Must complete before NetworkManager starts to avoid connection failures.
        /// </summary>
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
            
            // Find UTPTransport (either direct or inside CompositeTransport)
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
                
                // Validate relay data is available before initializing
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
                    // HOST INITIALIZATION: Initialize both server and client relay data
                    // Server data: Allows remote clients to connect through relay
                    // Client data: Allows host's client to connect to its own server in P2P mode
                    
                    PurrLogger.Log("Initializing UTP Relay Server (for host)...", this);
                    bool serverInit = utpTransport.InitializeRelayServer((Allocation)lobby.ServerObject);
                    PurrLogger.Log($"Relay Server initialized: {serverInit}", this);
                    
                    // CRITICAL FIX: Host needs client relay data too for P2P mode
                    // Without this, host's client gets "Relay data is required for P2P connection" error
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
                    // REMOTE CLIENT INITIALIZATION: Only needs client relay data
                    PurrLogger.Log($"Initializing UTP Relay Client with JoinCode: {lobby.Properties["JoinCode"]}", this);
                    
                    try {
                        bool clientInit = await utpTransport.InitializeRelayClient(lobby.Properties["JoinCode"]);
                        PurrLogger.Log($"Relay Client initialized: {clientInit}", this);
                    }
                    catch (System.Exception ex) {
                        PurrLogger.LogError($"Failed to initialize relay client: {ex.Message}", this);
                    }
                }
                
                // Relay configuration complete - now safe to start NetworkManager
                StartNetworkAfterRelayConfig();
            }
        }

        private void StartNetworkAfterRelayConfig()
        {
            // Prevent duplicate starts from multiple scene loads or concurrent calls
            if (_hasStarted)
            {
                PurrLogger.LogWarning("StartNetworkAfterRelayConfig() already called - ignoring duplicate", this);
                return;
            }
            
            if (!_networkManager || !_lobbyDataHolder || !_lobbyDataHolder.CurrentLobby.IsValid)
                return;

            _hasStarted = true;
            
            // CRITICAL FIX: Re-enable NetworkManager now that relay is fully configured
            // This allows NetworkManager.Start() to run with properly initialized relay data
            _networkManager.enabled = true;

            if(_lobbyDataHolder.CurrentLobby.IsOwner)
            {
                // HOST: Start as host with P2P mode enabled
                // - Server listens through relay for remote clients
                // - Host's client uses P2P mode for in-process communication with its own server
                // - P2P mode must stay ENABLED for host
                PurrLogger.Log("Starting as Host", this);
                _networkManager.StartHost();
            }
            else
            {
                // REMOTE CLIENT: Connect to host through relay
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
            int maxAttempts = 8; // ← increased from 5

            // Increased initial wait to give host's Steam SDR session more time to activate
            yield return new WaitForSeconds(3f); // ← was 1.5f

            while (attempts < maxAttempts)
            {
                _networkManager.StartClient();

                // CRITICAL FIX: Wait for a short grace period before calculating timeouts.
                // PurrNet / SteamTransport may not reliably update 'clientState' to 'Connecting' 
                // on the exact same frame that StartClient() returns execution.
                float gracePeriod = 0f;
                while (_networkManager.clientState == ConnectionState.Disconnected && gracePeriod < 2f)
                {
                    yield return null; // Wait for the next Unity frame
                    gracePeriod += Time.deltaTime;
                }

                float timeoutCounter = 0f;
                float maxTimeout = 30f; // ← slightly increased from 25f for internet play

                // Now it's safe to loop while 'Connecting'
                while (_networkManager.clientState == ConnectionState.Connecting 
                    && timeoutCounter < maxTimeout)
                {
                    yield return new WaitForSeconds(0.5f);
                    timeoutCounter += 0.5f;
                }

                // Did the connection successfully establish?
                if (_networkManager.clientState == ConnectionState.Connected)
                {
                    Debug.Log($"[ConnectionStarter] Connected! clientState = {_networkManager.clientState}");
                    IsClientConnectionInProgress = false;
                    yield break;
                }

                // If not, it failed or timed out.
                attempts++;
                PurrLogger.Log($"[ConnectionStarter] Attempt {attempts}/{maxAttempts} failed. Current state: {_networkManager.clientState}. Retrying...", this);
                _networkManager.StopClient();

                // ← Increased cooldown to give Steam SDR more time to reset cleanly
                yield return new WaitForSeconds(6f); // ← was 4f

                // Re-apply the host ID captured before leaving the lobby scene.
#if STEAMWORKS_NET || STEAMWORKS_NET_PACKAGE
                TryApplySteamHostAddress();
#endif
            }

            PurrLogger.LogError("[ConnectionStarter] Client failed to connect after all retry attempts. Returning to lobby.", this);
            IsClientConnectionInProgress = false;
            _networkManager.StopClient();
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbySample");
        }


        // Reset the _hasStarted flag when the ConnectionStarter is destroyed
        // private void OnDestroy()
        // {
        //     _hasStarted = false;
        // }
    }
}
