using UnityEngine;
using UnityEngine.UI;
using PurrLobby;

public class GameSettings : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup menuCanvas;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider voiceSlider;

    private bool isMenuOpen = false;

    void Start()
    {

        isMenuOpen = false;
        UpdateMenuState();

        if (AudioManager.Instance != null)
        {
            float m = AudioManager.Instance.GetVolume(AudioManager.MASTER_KEY);
            float mu = AudioManager.Instance.GetVolume(AudioManager.MUSIC_KEY);
            float s = AudioManager.Instance.GetVolume(AudioManager.SFX_KEY);

            masterSlider.value = m;
            musicSlider.value = mu;
            sfxSlider.value = s;

            AudioManager.Instance.SetVolume(AudioManager.MASTER_KEY, m);
            AudioManager.Instance.SetVolume(AudioManager.MUSIC_KEY, mu);
            AudioManager.Instance.SetVolume(AudioManager.SFX_KEY, s);

            if (voiceSlider != null)
            {
                float v = AudioManager.Instance.GetVolume(AudioManager.VOICE_KEY);
                voiceSlider.value = v;
                AudioManager.Instance.SetVolume(AudioManager.VOICE_KEY, v);
            }
        }

        masterSlider.onValueChanged.AddListener(v => UpdateVolume(AudioManager.MASTER_KEY, v));
        musicSlider.onValueChanged.AddListener(v => UpdateVolume(AudioManager.MUSIC_KEY, v));
        sfxSlider.onValueChanged.AddListener(v => UpdateVolume(AudioManager.SFX_KEY, v));
        if (voiceSlider != null)
            voiceSlider.onValueChanged.AddListener(v => UpdateVolume(AudioManager.VOICE_KEY, v));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            if (LoadingScreen.Instance != null && LoadingScreen.Instance.IsLoading)
                return;

            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        UpdateMenuState();
    }

    private void UpdateMenuState()
    {
        if (menuCanvas != null)
        {
            menuCanvas.alpha = isMenuOpen ? 1 : 0;
            menuCanvas.interactable = isMenuOpen;
            menuCanvas.blocksRaycasts = isMenuOpen;
        }

        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void UpdateVolume(string param, float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(param, value);
        }
    }
}
