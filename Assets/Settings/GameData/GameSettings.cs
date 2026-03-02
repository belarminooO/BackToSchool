using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class GameSettings : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("UI Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider micSlider; // MetaVoiceChat
    [SerializeField] private Slider voiceSlider; // MetaVoiceChat

    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject defaultLobbyUI;
    void Start()
    {
        masterSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 1f);
        micSlider.value = PlayerPrefs.GetFloat("MicVol", 1f);
        voiceSlider.value = PlayerPrefs.GetFloat("VoiceChatVol", 1f);

        masterSlider.onValueChanged.AddListener(v => UpdateVolume("MasterVol", v));
        musicSlider.onValueChanged.AddListener(v => UpdateVolume("MusicVol", v));
        sfxSlider.onValueChanged.AddListener(v => UpdateVolume("SFXVol", v));
        micSlider.onValueChanged.AddListener(v => UpdateVolume("MicVol", v));
        voiceSlider.onValueChanged.AddListener(v => UpdateVolume("VoiceChatVol", v));

        UpdateVolume("MasterVol", masterSlider.value);
        UpdateVolume("MusicVol", musicSlider.value);
        UpdateVolume("SFXVol", sfxSlider.value);
        UpdateVolume("MicVol", voiceSlider.value);
        UpdateVolume("VoiceChatVol", voiceSlider.value);
    }
    private void UpdateVolume(string param, float linearValue)
    {
        PlayerPrefs.SetFloat(param, linearValue);
        PlayerPrefs.Save();

        float db = Mathf.Log10(Mathf.Clamp(linearValue, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat(param, db);
    }
    public void OpenSettings()
    {
        defaultLobbyUI.SetActive(false);
        settingsPanel.SetActive(true);
    }
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        defaultLobbyUI.SetActive(true);
    }
}
