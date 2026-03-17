using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips")]
    public AudioClip engineIdle;
    public AudioClip engineLoop;
    public AudioClip tyreScreech;
    public AudioClip crashSound;
    public AudioClip backgroundMusic;

    [Header("Volume")]
    [Range(0f, 1f)] public float engineVolume = 0.3f;
    [Range(0f, 1f)] public float screechVolume = 0.8f;
    [Range(0f, 1f)] public float crashVolume = 1.0f;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    [Header("Engine Pitch")]
    public float minPitch = 0.4f;
    public float maxPitch = 1.8f;
    public float maxSpeedForPitch = 20f;
    public float pitchSmoothTime = 0.1f;

    [Tooltip("Speed (m/s) below which idle sound plays.")]
    public float idleSpeedThreshold = 1f;

    // ── Audio Sources ─────────────────────────────────────────────────────────
    private AudioSource idleSource;
    private AudioSource engineSource;
    private AudioSource screechSource;
    private AudioSource sfxSource;
    private AudioSource musicSource;

    // ── State ─────────────────────────────────────────────────────────────────
    private float currentPitch = 0.4f;
    private float pitchVelocity = 0f;
    private bool screechPlaying = false;
    private bool isIdle = true;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        idleSource = CreateSource(engineIdle, engineVolume, true);
        engineSource = CreateSource(engineLoop, 0f, true);
        screechSource = CreateSource(tyreScreech, 0f, true);
        sfxSource = CreateSource(null, 1f, false);
        musicSource = CreateSource(backgroundMusic, musicVolume, true);

        // Start idle and music
        idleSource.Play();
        engineSource.Play();
        screechSource.Play();
        musicSource.Play();
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void UpdateEngine(float speed)
    {
        bool shouldIdle = speed <= idleSpeedThreshold;

        if (shouldIdle && !isIdle)
        {
            // Transition to idle
            isIdle = true;
            idleSource.volume = engineVolume;
            engineSource.volume = 0f;
        }
        else if (!shouldIdle && isIdle)
        {
            // Transition to moving
            isIdle = false;
            idleSource.volume = 0f;
            engineSource.volume = engineVolume;
        }

        if (!shouldIdle)
        {
            float targetPitch = Mathf.Lerp(minPitch, maxPitch,
                                Mathf.Clamp01(speed / maxSpeedForPitch));
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch,
                           ref pitchVelocity, pitchSmoothTime);
            engineSource.pitch = currentPitch;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void UpdateScreech(bool shouldScreech)
    {
        if (screechSource == null) return;

        if (shouldScreech && !screechPlaying)
        {
            screechSource.volume = screechVolume;
            screechPlaying = true;
        }
        else if (!shouldScreech && screechPlaying)
        {
            screechSource.volume = Mathf.MoveTowards(
                screechSource.volume, 0f, Time.deltaTime * 3f);
            if (screechSource.volume <= 0f)
                screechPlaying = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void PlayCrash()
    {
        if (sfxSource == null || crashSound == null) return;
        sfxSource.PlayOneShot(crashSound, crashVolume);
    }

    public void StopMusic() => musicSource?.Stop();
    public void PauseMusic() => musicSource?.Pause();
    public void ResumeMusic() => musicSource?.UnPause();

    // ─────────────────────────────────────────────────────────────────────────
    AudioSource CreateSource(AudioClip clip, float volume, bool loop)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        return src;
    }
}