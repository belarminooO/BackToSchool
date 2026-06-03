using UnityEngine;
using PurrNet;

[DisallowMultipleComponent]
public class GeneratedFootstepEmitter : MonoBehaviour
{
    private const float PlayerTopSpeed = 8f;
    private const float VigiaTopSpeed = 4.5f;

    [SerializeField] private bool useVigiaProfile;
    [SerializeField] private float minSpeedToPlay = 1.2f;
    [SerializeField] private float baseStepInterval = 0.46f;
    [SerializeField] private float fastStepInterval = 0.30f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 11f;
    [SerializeField] private float worldVolumeScale = 0.45f;
    [SerializeField] private float selfVolumeScale = 0.35f;
    [SerializeField] private float pitchJitter = 0.06f;
    [SerializeField] private bool playLocalOwner2D;
    [SerializeField] private bool _vigiaInvestigating;

    private AudioSource _worldSource;
    private AudioSource _localSource;
    private NetworkIdentity _networkIdentity;
    private float _stepTimer;
    private int _stepIndex;

    public void ConfigureForPlayer()
    {
        useVigiaProfile = false;
        minSpeedToPlay = 1.2f;
        baseStepInterval = 0.46f;
        fastStepInterval = 0.30f;
        minDistance = 1f;
        maxDistance = 11f;
        worldVolumeScale = 0.52f;
        selfVolumeScale = 0.38f;
        pitchJitter = 0.06f;
        playLocalOwner2D = true;
        EnsureSource();
    }

    public void ConfigureForVigia()
    {
        useVigiaProfile = true;
        minSpeedToPlay = 0.45f;
        baseStepInterval = 0.62f;
        fastStepInterval = 0.34f;
        minDistance = 1.2f;
        maxDistance = 14f;
        worldVolumeScale = 0.58f;
        selfVolumeScale = 0f;
        pitchJitter = 0.04f;
        playLocalOwner2D = false;
        EnsureSource();
    }

    public void SetVigiaMovementMode(bool isInvestigating)
    {
        _vigiaInvestigating = isInvestigating;
    }

    public void Tick(bool isGrounded, float speed, float deltaTime)
    {
        EnsureSource();

        if (!isGrounded || speed < minSpeedToPlay)
        {
            _stepTimer = 0f;
            return;
        }

        float topSpeed = useVigiaProfile ? VigiaTopSpeed : PlayerTopSpeed;
        float normalizedSpeed = Mathf.InverseLerp(minSpeedToPlay, topSpeed, speed);
        float currentInterval = Mathf.Lerp(baseStepInterval, fastStepInterval, normalizedSpeed);

        _stepTimer += deltaTime;
        if (_stepTimer < currentInterval)
            return;

        _stepTimer = 0f;

        string clipName = useVigiaProfile
            ? GeneratedQuizSfx.Instance.GetVigiaFootstepClip(_stepIndex, _vigiaInvestigating)
            : GeneratedQuizSfx.Instance.GetPlayerFootstepClip(_stepIndex);

        _stepIndex++;

        float pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        float currentWorldVolume = Mathf.Lerp(worldVolumeScale * 0.75f, worldVolumeScale, normalizedSpeed);
        float currentSelfVolume = Mathf.Lerp(selfVolumeScale * 0.75f, selfVolumeScale, normalizedSpeed);
        bool isLocalOwner = IsLocalOwner();

        if (!isLocalOwner || !playLocalOwner2D)
        {
            GeneratedQuizSfx.Instance.TryPlayOnSource(_worldSource, clipName, currentWorldVolume, pitch);
        }

        if (isLocalOwner && playLocalOwner2D)
        {
            GeneratedQuizSfx.Instance.TryPlayOnSource(_localSource, clipName, currentSelfVolume, pitch);
        }
    }

    public void ResetCycle()
    {
        _stepTimer = 0f;
    }

    private void EnsureSource()
    {
        GeneratedQuizSfx.EnsureExists();

        if (_networkIdentity == null)
            _networkIdentity = GetComponent<NetworkIdentity>();

        if (_worldSource == null)
        {
            _worldSource = gameObject.AddComponent<AudioSource>();
        }
        GeneratedQuizSfx.Instance.ConfigureWorldSource(_worldSource, minDistance, maxDistance);

        if (playLocalOwner2D)
        {
            if (_localSource == null)
            {
                _localSource = gameObject.AddComponent<AudioSource>();
            }

            _localSource.playOnAwake = false;
            _localSource.loop = false;
            _localSource.spatialBlend = 0f;
            _localSource.dopplerLevel = 0f;
            _localSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    private bool IsLocalOwner()
    {
        return _networkIdentity != null && _networkIdentity.isOwner;
    }
}
