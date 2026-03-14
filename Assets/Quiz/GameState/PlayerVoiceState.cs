using UnityEngine;
using PurrNet;
using MetaVoiceChat.AudioFilters;

public class PlayerVoiceState : NetworkBehaviour
{
    [Header("Settings")]

    public float talkingThreshold = -45f; // Same as Vigia's noiseThreshold
    public float talkingHangTime = 0.5f;

    public SyncVar<bool> isTalking = new SyncVar<bool>(false, ownerAuth: true);

    private NoiseGate _noiseGate;
    private float _timeSinceLastSpoke = 0f;

    private void Start()
    {
        _noiseGate = GetComponentInChildren<NoiseGate>(true);

        if (_noiseGate == null)
            _noiseGate = Object.FindFirstObjectByType<NoiseGate>();

        if (_noiseGate == null)
            Debug.LogWarning("[PlayerVoiceState] No NoiseGate found in scene — voice detection disabled.");
        else
            Debug.Log($"[PlayerVoiceState] Found NoiseGate: {_noiseGate.gameObject.name}");
    }

    private void Update()
    {
        if (!isOwner || _noiseGate == null)
            return;

        bool currentlyDetecting = _noiseGate.currentVolumeDB > talkingThreshold;
        if (currentlyDetecting)
        {
            _timeSinceLastSpoke = 0f;
            if (!isTalking.value)
            {
                isTalking.value = true;
                Debug.Log($"[PlayerVoiceState] Started talking (dB: {_noiseGate.currentVolumeDB:F1})");
            }
        }
        else
        {
            _timeSinceLastSpoke += Time.deltaTime;
            if (_timeSinceLastSpoke > talkingHangTime && isTalking.value)
            {
                isTalking.value = false;
                Debug.Log($"[PlayerVoiceState] Stopped talking after hang time.");
            }
        }
    }
}