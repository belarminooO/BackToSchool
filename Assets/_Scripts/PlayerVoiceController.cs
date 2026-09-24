using UnityEngine;
using UnityEngine.InputSystem;
using MetaVoiceChat;
using SmnStyleHardline.Demo;

public class PlayerVoiceController : MonoBehaviour
{
    [Header("Voice Chat")]
    public MetaVc voiceChat;

    [Header("Input Action")]
    public InputActionReference muteMicAction;

    [Header("UI")]
    public GameObject muteMicIndicator;
    public GameObject micActivatedIndicator;

    private void OnEnable()
    {
        if (muteMicAction != null && muteMicAction.action != null)
        {
            muteMicAction.action.Enable();
            muteMicAction.action.performed += OnMutedPerformed;
            Debug.Log("[PlayerVoiceController] Subscribed to MuteMic action performed event.");
        
        }

        if (voiceChat != null)
        {
            voiceChat.isSpeaking.OnValueChanged += OnSpeaking;
            Debug.Log("[PlayerVoiceController] Subscribed to MetaVc isSpeaking event.");
        }

    }

    private void OnDisable()
    {
        if (muteMicAction != null && muteMicAction.action != null)
        {
            muteMicAction.action.performed -= OnMutedPerformed;
        }

        if (voiceChat != null)
        {
            voiceChat.isSpeaking.OnValueChanged -= OnSpeaking;
        }
    }

    private void Start()
    {
        Debug.Log("[PlayerVoiceController] Started.");
        if (micActivatedIndicator != null)
        {
            micActivatedIndicator.SetActive(false);
        }
        UpdateUI();

        PurrLobby.AudioManager.Instance?.RouteVoiceChat();
    }
    
    private void OnMutedPerformed(InputAction.CallbackContext context)
    {
        if (voiceChat != null)
        {
            voiceChat.isInputMuted.Value = !voiceChat.isInputMuted.Value;
            Debug.Log($"[PlayerVoiceController] InputAction performed! Toggled Mute to: {voiceChat.isInputMuted.Value}. Calling UpdateUI().");
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        bool isMuted = voiceChat != null && voiceChat.isInputMuted.Value;
        bool isSpeaking = voiceChat != null && voiceChat.isSpeaking.Value;

        if (muteMicIndicator != null)
        {
            muteMicIndicator.SetActive(isMuted);
        }


        if (micActivatedIndicator != null && voiceChat != null)
        {
            if (isMuted)
            {
                micActivatedIndicator.SetActive(false);
            }
            else
            {
                micActivatedIndicator.SetActive(isSpeaking);
            }       
        }

        // If the mic is muted, hide indicator
        //if (voiceChat != null && voiceChat.isInputMuted.Value && micActivatedIndicator != null)
        //{
        //    micActivatedIndicator.SetActive(false);
        //}
    }

    private void OnSpeaking(bool isSpeaking)
    {
        if (micActivatedIndicator != null && voiceChat != null)
        {
            bool isMuted = voiceChat.isInputMuted.Value;
            Debug.Log($"[PlayerVoiceController] OnSpeaking() Event Fired! -> isSpeaking: {isSpeaking} | Muted: {isMuted}");
            micActivatedIndicator.SetActive(isSpeaking && !isMuted);
        }
    }
}

