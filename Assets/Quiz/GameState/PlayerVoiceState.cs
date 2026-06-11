using UnityEngine;
using PurrNet;
using MetaVoiceChat.AudioFilters;

public class PlayerVoiceState : NetworkBehaviour
{
    [Header("Settings")]
    public float talkingThreshold = -38f;
    public float stopTalkingThreshold = -43f;
    public float talkingStartDelay = 0.2f;
    public float talkingHangTime = 0.5f;

    public SyncVar<bool> isTalking = new SyncVar<bool>(false, ownerAuth: true);

    private NoiseGate _noiseGate;
    private float _timeAboveThreshold;
    private float _timeBelowThreshold;

    private void Start()
    {
        _noiseGate = GetComponentInChildren<NoiseGate>(true);

        if (_noiseGate == null)
            Debug.LogWarning("[PlayerVoiceState] Own NoiseGate missing - voice detection disabled.");
        else
            Debug.Log($"[PlayerVoiceState] Found NoiseGate: {_noiseGate.gameObject.name}");
    }

    private void Update()
    {
        if (!isOwner)
            return;

        if (_noiseGate == null || !_noiseGate.HasFreshSamples)
        {
            SetTalking(false);
            return;
        }

        if (!isTalking.value)
        {
            _timeAboveThreshold = _noiseGate.currentVolumeDB > talkingThreshold
                ? _timeAboveThreshold + Time.deltaTime
                : 0f;

            if (_timeAboveThreshold >= talkingStartDelay)
                SetTalking(true);

            return;
        }

        _timeBelowThreshold = _noiseGate.currentVolumeDB < stopTalkingThreshold
            ? _timeBelowThreshold + Time.deltaTime
            : 0f;

        if (_timeBelowThreshold >= talkingHangTime)
            SetTalking(false);
    }

    private void SetTalking(bool value)
    {
        if (isTalking.value == value)
            return;

        isTalking.value = value;
        _timeAboveThreshold = 0f;
        _timeBelowThreshold = 0f;
        Debug.Log($"[PlayerVoiceState] Talking={value} (dB: {(_noiseGate != null ? _noiseGate.currentVolumeDB : -80f):F1})");
    }
}
