using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace PurrLobby
{
    // Arquivo de backup da versao com musicas separadas por fase.
    public class AudiomanagerComSonsDasFases : MonoBehaviour
    {
        public static AudiomanagerComSonsDasFases Instance { get; private set; }

        private const string QuizSceneName = "Quiz AI";
        private const string ObstacleRaceStateName = "ObstacleRaceStateNode";

        [SerializeField] private AudioMixer audioMixer;

        public const string MASTER_KEY = "MasterVolume";
        public const string MUSIC_KEY = "MusicVolume";
        public const string SFX_KEY = "SFXVolume";
        public const string VOICE_KEY = "VoiceVolume";

        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip quizMusicClip;
        [SerializeField] private AudioClip obstacleRaceMusicClip;
        private AudioSource sfxSource;
        private AudioSource musicSource;
        private AudioClip lobbyMusicClip;
        private string lastSceneName = string.Empty;
        private string lastQuizStateName = string.Empty;
        private System.Type quizGameManagerType;
        private System.Reflection.PropertyInfo quizGameManagerInstanceProperty;
        private System.Reflection.FieldInfo currentStateField;
        private System.Reflection.PropertyInfo currentStateValueProperty;
        private System.Reflection.FieldInfo currentStateValueField;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                musicSource = GetComponent<AudioSource>();
                lobbyMusicClip = musicSource != null ? musicSource.clip : null;
                if (musicSource != null)
                {
                    musicSource.loop = true;
                }

                sfxSource = gameObject.AddComponent<AudioSource>();

                if (audioMixer != null)
                {
                    AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("SFX");
                    if (groups.Length > 0)
                    {
                        sfxSource.outputAudioMixerGroup = groups[0];
                    }
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(ApplyInitialVolumes());
        }

        private void Update()
        {
            RefreshMusicForCurrentContext();
        }

        private System.Collections.IEnumerator ApplyInitialVolumes()
        {
            yield return null;
            LoadVolumeSettings();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshMusicForCurrentContext(forceRefresh: true, sceneNameOverride: scene.name);
            RouteVoiceChat();
        }

        public void PlayButtonClick()
        {
            if (sfxSource != null && buttonClickSound != null)
            {
                sfxSource.PlayOneShot(buttonClickSound);
            }
        }

        private void LoadVolumeSettings()
        {
            float masterVolume = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
            float musicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
            float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 1f);
            float voiceVolume = PlayerPrefs.GetFloat(VOICE_KEY, 1f);

            SetVolume(MASTER_KEY, masterVolume);
            SetVolume(MUSIC_KEY, musicVolume);
            SetVolume(SFX_KEY, sfxVolume);
            SetVolume(VOICE_KEY, voiceVolume);
        }

        public void SetVolume(string parameterName, float sliderValue)
        {
            if (audioMixer != null)
            {
                float volume = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
                audioMixer.SetFloat(parameterName, volume);
                PlayerPrefs.SetFloat(parameterName, sliderValue);
            }
        }

        public float GetVolume(string parameterName)
        {
            return PlayerPrefs.GetFloat(parameterName, 1f);
        }

        private void RefreshMusicForCurrentContext(bool forceRefresh = false, string sceneNameOverride = null)
        {
            if (musicSource == null)
            {
                return;
            }

            string sceneName = string.IsNullOrEmpty(sceneNameOverride)
                ? SceneManager.GetActiveScene().name
                : sceneNameOverride;
            string stateName = sceneName == QuizSceneName ? TryGetCurrentQuizStateName() : string.Empty;

            if (!forceRefresh && sceneName == lastSceneName && stateName == lastQuizStateName)
            {
                return;
            }

            lastSceneName = sceneName;
            lastQuizStateName = stateName;

            AudioClip targetClip = ResolveMusicClip(sceneName, stateName);
            if (targetClip == null)
            {
                return;
            }

            if (musicSource.clip != targetClip)
            {
                musicSource.Stop();
                musicSource.clip = targetClip;
            }

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        private AudioClip ResolveMusicClip(string sceneName, string stateName)
        {
            if (sceneName != QuizSceneName)
            {
                return lobbyMusicClip != null ? lobbyMusicClip : musicSource.clip;
            }

            if (stateName == ObstacleRaceStateName && obstacleRaceMusicClip != null)
            {
                return obstacleRaceMusicClip;
            }

            if (quizMusicClip != null)
            {
                return quizMusicClip;
            }

            return lobbyMusicClip != null ? lobbyMusicClip : musicSource.clip;
        }

        private string TryGetCurrentQuizStateName()
        {
            if (!TryCacheQuizStateReflection())
            {
                return string.Empty;
            }

            object managerInstance = quizGameManagerInstanceProperty.GetValue(null);
            if (managerInstance == null)
            {
                return string.Empty;
            }

            object syncVar = currentStateField.GetValue(managerInstance);
            if (syncVar == null)
            {
                return string.Empty;
            }

            CacheCurrentStateAccessors(syncVar.GetType());

            if (currentStateValueProperty != null)
            {
                return currentStateValueProperty.GetValue(syncVar) as string ?? string.Empty;
            }

            if (currentStateValueField != null)
            {
                return currentStateValueField.GetValue(syncVar) as string ?? string.Empty;
            }

            return string.Empty;
        }

        private bool TryCacheQuizStateReflection()
        {
            if (quizGameManagerType == null)
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    quizGameManagerType = assembly.GetType("QuizGameManager");
                    if (quizGameManagerType != null)
                    {
                        break;
                    }
                }
            }

            if (quizGameManagerType == null)
            {
                return false;
            }

            if (quizGameManagerInstanceProperty == null)
            {
                quizGameManagerInstanceProperty = quizGameManagerType.GetProperty(
                    "instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            }

            if (currentStateField == null)
            {
                currentStateField = quizGameManagerType.GetField(
                    "currentStateName",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            }

            return quizGameManagerInstanceProperty != null && currentStateField != null;
        }

        private void CacheCurrentStateAccessors(System.Type syncVarType)
        {
            if (syncVarType == null || currentStateValueProperty != null || currentStateValueField != null)
            {
                return;
            }

            currentStateValueProperty = syncVarType.GetProperty(
                "value",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            currentStateValueField = syncVarType.GetField(
                "value",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        }

        public void RouteVoiceChat()
        {
            if (audioMixer == null) return;

            AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("Voice Chat Volume");
            if (groups.Length == 0) return;

            AudioMixerGroup voiceGroup = groups[0];

            var allComponents = FindObjectsOfType<Component>(true);
            foreach (var comp in allComponents)
            {
                if (comp.GetType().Name == "VcAudioSourceOutput")
                {
                    var field = comp.GetType().GetField("audioSource");
                    if (field != null)
                    {
                        var source = field.GetValue(comp) as AudioSource;
                        if (source != null)
                        {
                            source.outputAudioMixerGroup = voiceGroup;
                        }
                    }
                }
            }
        }
    }
}
