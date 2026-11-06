using UnityEngine;

/// <summary>Drives one recording from consecutive conductor downstrokes.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GestureDrivenMusic : MonoBehaviour
{
    [SerializeField] private DownstrokeInput input;
    [SerializeField] private AudioSource recordingSource;
    [SerializeField] private FirstBeatTimeline timeline;
    [SerializeField] private ConductorTuning tuning;
    [SerializeField] private ConductorDemo conductor;
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
    private bool menuPaused;
    private int lastVolumeScore = -1;
    private float volumeTransitionStart;
    private float volumeAtTransitionStart;
    private float volumeAtTransitionEnd;

    private bool TimelineMode => tuning != null && tuning.musicMode == ConductorTuning.MusicMode.Timeline;
    public float PlaybackSpeed => TimelineMode ? (recordingSource != null && recordingSource.isPlaying ? 1f : 0f)
        : paused ? 0f : recordingSource.pitch;
    public bool IsPaused => paused;

private void Awake()
    {
        if (recordingSource == null) recordingSource = GetComponent<AudioSource>();
        if (timeline == null) timeline = GetComponent<FirstBeatTimeline>();
        if (tuning == null && timeline != null) tuning = timeline.Tuning;
        if (conductor == null) conductor = FindFirstObjectByType<ConductorDemo>();
        if (input == null) input = FindFirstObjectByType<DownstrokeInput>();
        playingVolume = tuning != null ? tuning.recordingVolume : recordingSource.volume;
        recordingSource.playOnAwake = false;
    }

private void OnEnable()
    {
        if (input == null) input = FindFirstObjectByType<DownstrokeInput>();
        if (input != null) input.Downstroke += OnDownstroke;
    }

private void Start()
    {
        if (TimelineMode || menuPaused) return;
        recordingSource.Stop();
        recordingSource.pitch = 1f;
        recordingSource.volume = 0f;
        paused = true;
    }

private void OnDisable()
    {
        if (input != null) input.Downstroke -= OnDownstroke;
        if (!TimelineMode && recordingSource != null)
        {
            recordingSource.Pause();
            recordingSource.volume = playingVolume;
        }
        if (!TimelineMode) paused = true;
        fadingOut = false;
    }

    private void OnValidate()
    {
        minimumPlaybackSpeed = Mathf.Min(minimumPlaybackSpeed, maximumPlaybackSpeed);
        minimumBeatInterval = Mathf.Max(0.05f, minimumBeatInterval);
    }

private void Update()
    {
        if (TimelineMode)
        {
            if (recordingSource == null || timeline == null || timeline.IsPaused || menuPaused) return;
            int score = conductor != null ? conductor.LightScore : 0;
            if (score != lastVolumeScore)
            {
                volumeAtTransitionStart = recordingSource.volume;
                volumeAtTransitionEnd = tuning.recordingVolume *
                    Mathf.Clamp01((float)score / Mathf.Max(1, tuning.maximumLightScore));
                volumeTransitionStart = Time.time;
                lastVolumeScore = score;
            }
            float progress = Mathf.Clamp01((Time.time - volumeTransitionStart) /
                Mathf.Max(0.01f, tuning.scoreVolumeTransitionSeconds));
            float shaped = tuning.scoreVolumeCurve != null
                ? Mathf.Clamp01(tuning.scoreVolumeCurve.Evaluate(progress)) : progress;
            recordingSource.volume = Mathf.Lerp(volumeAtTransitionStart, volumeAtTransitionEnd, shaped);
            return;
        }
        if (paused) return;
        float stopAfter = tuning != null ? tuning.gestureStopAfterSeconds : stopAfterSeconds;
        if (!fadingOut && AudioSettings.dspTime - lastStrokeTime >= stopAfter)
            Pause();

        float targetVolume = fadingOut ? 0f : playingVolume;
        float seconds = fadingOut
            ? (tuning != null ? tuning.musicFadeOutSeconds : fadeOutSeconds)
            : (tuning != null ? tuning.musicFadeInSeconds : fadeInSeconds);
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
                (tuning != null ? tuning.speedSmoothing : speedSmoothing) * Time.unscaledDeltaTime);
    }

private void OnDownstroke(double dspTime)
    {
        if (TimelineMode || input == null || recordingSource.clip == null || menuPaused) return;
        playingVolume = tuning != null ? tuning.recordingVolume : playingVolume;
        float stopAfter = tuning != null ? tuning.gestureStopAfterSeconds : stopAfterSeconds;
        double interval = dspTime - lastStrokeTime;
        if (interval > 0d && interval <= stopAfter)
            targetSpeed = Mathf.Clamp(
                (tuning != null ? tuning.gestureReferenceBeatInterval : referenceBeatInterval) /
                Mathf.Max((float)interval, tuning != null ? tuning.minimumBeatInterval : minimumBeatInterval),
                tuning != null ? tuning.minimumPlaybackSpeed : minimumPlaybackSpeed,
                tuning != null ? tuning.maximumPlaybackSpeed : maximumPlaybackSpeed);
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
        if ((tuning != null ? tuning.musicFadeOutSeconds : fadeOutSeconds) <= 0f)
        {
            recordingSource.volume = 0f;
            recordingSource.Pause();
            paused = true;
            fadingOut = false;
        }
    }

    public void SetMenuPaused(bool value) { menuPaused = value; }
}
