using System.Collections.Generic;
using UnityEngine;

public class GeneratedQuizSfx : MonoBehaviour
{
    public const string ResourceRoot = "GeneratedGameSfx";
    public const string AnswerButtonPress = "answer_button_press";
    public const string CorrectAnswer = "correct_answer";
    public const string IntervalBell = "interval_bell";
    public const string PlayerFootstep01 = "player_footstep_01";
    public const string PlayerFootstep02 = "player_footstep_02";
    public const string QuestionStart = "question_start";
    public const string QuestionTimeWarning = "question_time_warning";
    public const string VigiaAlert = "vigia_alert";
    public const string VigiaRunFootstep01 = "vigia_run_footstep_01";
    public const string VigiaRunFootstep02 = "vigia_run_footstep_02";
    public const string VigiaWalkFootstep01 = "vigia_walk_footstep_01";
    public const string VigiaWalkFootstep02 = "vigia_walk_footstep_02";
    public const string WrongAnswer = "wrong_answer";

    private static GeneratedQuizSfx _instance;

    private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();
    private readonly HashSet<string> _missingClipWarnings = new HashSet<string>();

    private AudioSource _uiSource;

    public static GeneratedQuizSfx Instance
    {
        get
        {
            EnsureExists();
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (_instance != null)
            return;

        _instance = FindFirstObjectByType<GeneratedQuizSfx>();
        if (_instance == null)
        {
            GameObject go = new GameObject("Generated Quiz SFX");
            _instance = go.AddComponent<GeneratedQuizSfx>();
        }

        _instance.Initialize();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        Initialize();
    }

    private void Initialize()
    {
        DontDestroyOnLoad(gameObject);

        if (_uiSource == null)
        {
            _uiSource = gameObject.GetComponent<AudioSource>();
            if (_uiSource == null)
                _uiSource = gameObject.AddComponent<AudioSource>();

            _uiSource.playOnAwake = false;
            _uiSource.loop = false;
            _uiSource.spatialBlend = 0f;
            _uiSource.dopplerLevel = 0f;
            _uiSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    public void ConfigureWorldSource(AudioSource source, float minDistance, float maxDistance)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
    }

    public bool TryPlayOnSource(AudioSource source, string clipName, float volumeScale = 1f, float pitch = 1f)
    {
        if (source == null)
            return false;

        AudioClip clip = GetClip(clipName);
        if (clip == null)
            return false;

        source.pitch = pitch;
        source.PlayOneShot(clip, GetSfxVolume(volumeScale));
        return true;
    }

    public void PlayUi(string clipName, float volumeScale = 1f, float pitch = 1f)
    {
        TryPlayOnSource(_uiSource, clipName, volumeScale, pitch);
    }

    public void PlayAt(
        string clipName,
        Vector3 position,
        float volumeScale = 1f,
        float minDistance = 1f,
        float maxDistance = 16f,
        float pitch = 1f)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null)
            return;

        GameObject oneShot = new GameObject($"SFX {clipName}");
        oneShot.transform.position = position;

        AudioSource source = oneShot.AddComponent<AudioSource>();
        ConfigureWorldSource(source, minDistance, maxDistance);
        source.clip = clip;
        source.volume = GetSfxVolume(volumeScale);
        source.pitch = pitch;
        source.Play();

        Destroy(oneShot, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.25f);
    }

    public AudioClip GetClip(string clipName)
    {
        if (_clipCache.TryGetValue(clipName, out AudioClip cachedClip))
            return cachedClip;

        AudioClip clip = Resources.Load<AudioClip>($"{ResourceRoot}/{clipName}");
        if (clip == null)
        {
            if (_missingClipWarnings.Add(clipName))
            {
                Debug.LogWarning($"[GeneratedQuizSfx] Missing clip at Resources/{ResourceRoot}/{clipName}.wav");
            }
            return null;
        }

        _clipCache[clipName] = clip;
        return clip;
    }

    public string GetPlayerFootstepClip(int clipIndex)
    {
        return clipIndex % 2 == 0 ? PlayerFootstep01 : PlayerFootstep02;
    }

    public string GetVigiaFootstepClip(int clipIndex, bool isInvestigating)
    {
        if (isInvestigating)
            return clipIndex % 2 == 0 ? VigiaRunFootstep01 : VigiaRunFootstep02;

        return clipIndex % 2 == 0 ? VigiaWalkFootstep01 : VigiaWalkFootstep02;
    }

    public static void PlayAnswerResult(bool correctAnswer)
    {
        Instance.PlayUi(
            correctAnswer ? CorrectAnswer : WrongAnswer,
            1.20f,
            Random.Range(0.98f, 1.02f));
    }

    public static void PlayButtonPress()
    {
        Instance.PlayUi(AnswerButtonPress, 0.45f, Random.Range(0.96f, 1.04f));
    }

    public static void PlayQuestionStart()
    {
        Instance.PlayUi(QuestionStart, 0.80f);
    }

    public static void PlayQuestionTimeWarning()
    {
        Instance.PlayUi(QuestionTimeWarning, 0.82f);
    }

    public static void PlayIntervalBell()
    {
        Instance.PlayUi(IntervalBell, 0.85f);
    }

    public static void PlayVigiaAlert(Vector3 position)
    {
        Instance.PlayAt(VigiaAlert, position, 0.9f, 2f, 18f, Random.Range(0.98f, 1.02f));
    }

    private float GetSfxVolume(float volumeScale)
    {
        float masterVolume = PlayerPrefs.GetFloat(PurrLobby.AudioManager.MASTER_KEY, 1f);
        float sfxVolume = PlayerPrefs.GetFloat(PurrLobby.AudioManager.SFX_KEY, 1f);
        return volumeScale * masterVolume * sfxVolume;
    }
}
