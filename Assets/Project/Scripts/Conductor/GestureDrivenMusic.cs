using UnityEngine;

/// <summary>Drives one recording from consecutive conductor downstrokes.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GestureDrivenMusic : MonoBehaviour
{
    [SerializeField] private DownstrokeInput input;
    [SerializeField] private AudioSource recordingSource;
    [Tooltip("Downstrokes this far apart play the recording at normal speed.")]
    [Min(0.1f)] [SerializeField] private float referenceBeatInterval = 0.7f;
    [Tooltip("Minimum interval used when estimating tempo; avoids extreme speed from accidental double strokes.")]
    [Min(0.05f)] [SerializeField] private float minimumBeatInterval = 0.25f;
    [Range(0.1f, 3f)] [SerializeField] private float minimumPlaybackSpeed = 0.5f;
    [Range(0.1f, 3f)] [SerializeField] private float maximumPlaybackSpeed = 1.6f;
    [Tooltip("Silence and pause after this many seconds without another downstroke.")]
    [Min(0.1f)] [SerializeField] private float stopAfterSeconds = 1.2f;
    [Tooltip("How quickly the playback speed follows the measured beat interval.")]
    [Min(0f)] [SerializeField] private float speedSmoothing = 8f;

    [Header("Volume Envelope")]
    [Tooltip("Seconds to reach the recording volume after a downstroke.")]
    [Min(0f)] [SerializeField] private float fadeInSeconds = 0.35f;
    [Tooltip("Seconds to fade to silence after releasing the baton or stopping strokes.")]
    [Min(0f)] [SerializeField] private float fadeOutSeconds = 0.55f;

    private float playingVolume = 1f;
    private bool fadingOut;
    private double lastStrokeTime = double.NegativeInfinity;
    private float targetSpeed = 1f;
    private bool started;
    private bool paused = true;

    public float PlaybackSpeed => paused ? 0f : recordingSource.pitch;
    public bool IsPaused => paused;

private void Awake()
    {
        if (recordingSource == null) recordingSource = GetComponent<AudioSource>();
        if (input == null) input = FindFirstObjectByType<DownstrokeInput>();
        playingVolume = recordingSource.volume;
        recordingSource.playOnAwake = false;
    }

private void OnEnable()
    {
        if (input == null) input = FindFirstObjectByType<DownstrokeInput>();
        if (input != null) input.Downstroke += OnDownstroke;
    }

private void Start()
    {
        recordingSource.Stop();
        recordingSource.pitch = 1f;
        recordingSource.volume = 0f;
        paused = true;
    }

private void OnDisable()
    {
        if (input != null) input.Downstroke -= OnDownstroke;
        if (recordingSource != null)
        {
            recordingSource.Pause();
            recordingSource.volume = playingVolume;
        }
        paused = true;
        fadingOut = false;
    }

    private void OnValidate()
    {
        minimumPlaybackSpeed = Mathf.Min(minimumPlaybackSpeed, maximumPlaybackSpeed);
        minimumBeatInterval = Mathf.Max(0.05f, minimumBeatInterval);
    }

private void Update()
    {
        if (paused) return;
        if (!fadingOut && AudioSettings.dspTime - lastStrokeTime >= stopAfterSeconds)
            Pause();

        float targetVolume = fadingOut ? 0f : playingVolume;
        float seconds = fadingOut ? fadeOutSeconds : fadeInSeconds;
        recordingSource.volume = seconds <= 0f ? targetVolume
            : Mathf.MoveTowards(recordingSource.volume, targetVolume,
                playingVolume * Time.unscaledDeltaTime / seconds);
        if (fadingOut && recordingSource.volume <= 0.0001f)
        {
            recordingSource.volume = 0f;
            recordingSource.Pause();
            paused = true;
            fadingOut = false;
            return;
        }
        if (!fadingOut)
            recordingSource.pitch = Mathf.MoveTowards(recordingSource.pitch, targetSpeed,
                speedSmoothing * Time.unscaledDeltaTime);
    }

private void OnDownstroke(double dspTime)
    {
        if (input == null || recordingSource.clip == null) return;
        double interval = dspTime - lastStrokeTime;
        if (interval > 0d && interval <= stopAfterSeconds)
            targetSpeed = Mathf.Clamp(referenceBeatInterval / Mathf.Max((float)interval, minimumBeatInterval),
                minimumPlaybackSpeed, maximumPlaybackSpeed);
        else
            targetSpeed = 1f;
        lastStrokeTime = dspTime;
        if (!started)
        {
            recordingSource.pitch = targetSpeed;
            recordingSource.volume = 0f;
            recordingSource.Play();
            started = true;
        }
        else if (paused)
        {
            recordingSource.pitch = targetSpeed;
            recordingSource.volume = 0f;
            recordingSource.UnPause();
        }
        fadingOut = false;
        paused = false;
    }



private void Pause()
    {
        if (paused || fadingOut) return;
        fadingOut = true;
        lastStrokeTime = double.NegativeInfinity;
        if (fadeOutSeconds <= 0f)
        {
            recordingSource.volume = 0f;
            recordingSource.Pause();
            paused = true;
            fadingOut = false;
        }
    }
}
