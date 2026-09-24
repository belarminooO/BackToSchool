using System;
using UnityEngine;

namespace PurrLobby
{
    public class LobbyDataHolder : MonoBehaviour
    {
        [SerializeField] private Lobby serializedLobby;
        public Lobby CurrentLobby { get; private set; }

        // Frozen at SceneSwitcher.SwitchScene() time (i.e. when OnAllReady fires and we know
        // everyone is provably present). The live Members list may shrink after that point
        // because Steam fires a phantom disconnect when one peer stops running its callbacks
        // during the scene switch — we can't trust the live count once we leave the lobby scene.
        public static int? CapturedExpectedPlayerCount;
        public static string CapturedHostSteamId;

        public static void ResetCapturedGameStartData()
        {
            CapturedExpectedPlayerCount = null;
            CapturedHostSteamId = null;
        }

        public void SetCurrentLobby(Lobby newLobby)
        {
            CurrentLobby = newLobby;
            serializedLobby = newLobby;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
