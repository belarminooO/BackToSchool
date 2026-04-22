using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    public class SettingsView : View
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (AudioManager.Instance != null)
            {
                masterSlider.value = AudioManager.Instance.GetVolume(AudioManager.MASTER_KEY);
                musicSlider.value = AudioManager.Instance.GetVolume(AudioManager.MUSIC_KEY);
                sfxSlider.value = AudioManager.Instance.GetVolume(AudioManager.SFX_KEY);
            }

            masterSlider.onValueChanged.AddListener(SetMasterVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);

            if (backButton != null)
            {
                backButton.onClick.AddListener(() => FindObjectOfType<ViewManager>().OnLeaveSettingsClicked());
            }
        }

        private void SetMasterVolume(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetVolume(AudioManager.MASTER_KEY, value);
        }

        private void SetMusicVolume(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetVolume(AudioManager.MUSIC_KEY, value);
        }

        private void SetSFXVolume(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetVolume(AudioManager.SFX_KEY, value);
        }
    }
}
