using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace PurrLobby
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioMixer audioMixer;

        public const string MASTER_KEY = "MasterVolume";
        public const string MUSIC_KEY = "MusicVolume";
        public const string SFX_KEY = "SFXVolume";
        public const string VOICE_KEY = "VoiceVolume";

        [SerializeField] private AudioClip buttonClickSound;
        private AudioSource sfxSource;
        private AudioSource musicSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                musicSource = GetComponent<AudioSource>();
                
                // Add an AudioSource specifically for UI SFX if none exists
                sfxSource = gameObject.AddComponent<AudioSource>();
                
                // Try to find the SFX group in the mixer
                if (audioMixer != null)
                {
                    AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("SFX");
                    if (groups.Length > 0)
                        sfxSource.outputAudioMixerGroup = groups[0];
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Aplicar volumes com um pequeno atraso para garantir que o Mixer esta pronto
            StartCoroutine(ApplyInitialVolumes());
        }

        private System.Collections.IEnumerator ApplyInitialVolumes()
        {
            yield return null; // Espera um frame
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
            if (scene.name == "Quiz AI")
            {
                if (musicSource != null && musicSource.isPlaying)
                {
                    musicSource.Stop();
                }
            }
            else
            {
                if (musicSource != null && !musicSource.isPlaying)
                {
                    musicSource.Play();
                }
            }

            // Garante que o chat de voz esta no mixer
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

        // Metodo para garantir que a voz passa pelo mixer usando Reflexao para evitar erros de compilacao
        public void RouteVoiceChat()
        {
            if (audioMixer == null) return;

            AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("Voice Chat Volume");
            if (groups.Length == 0) return;

            AudioMixerGroup voiceGroup = groups[0];
            
            // Procurar por todos os componentes na cena
            var allComponents = FindObjectsOfType<Component>(true);
            foreach (var comp in allComponents)
            {
                // Verificar se o nome do tipo contem VcAudioSourceOutput
                if (comp.GetType().Name == "VcAudioSourceOutput")
                {
                    // Usar reflexao para obter o campo audioSource
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
