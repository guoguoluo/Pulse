using System;
using UnityEngine;

/// <summary>One authored set of bar-one timestamps drives the audio, cue and judgement.</summary>
public class FirstBeatTimeline : MonoBehaviour
{
    [SerializeField] private float leadInSeconds = 1.5f;
    [Header("Single Recording")]
    [SerializeField] private AudioClip singleRecording;
    [SerializeField] private AudioSource recordingSource;
    [Tooltip("Enable only after the first-beat timestamps are manually marked against this recording.")]
    [SerializeField] private bool recordingBeatMarkersVerified;
    [Header("Demo Guide Beat (not aligned to recording)")]
    [Tooltip("Approximate quarter-note tempo for the four-beat guide; adjust by listening to your recording.")]
    [Range(30f, 180f)] [SerializeField] private float guideBeatsPerMinute = 60f;
    [Range(1, 12)] [SerializeField] private int beatsPerBar = 4;
    [Tooltip("Shift the guide's first downbeat relative to the start of the scene.")]
    [Min(0f)] [SerializeField] private float guideLeadInSeconds = 1.5f;

    public bool HasVerifiedBeatMarkers => singleRecording == null || recordingBeatMarkersVerified;
    public bool HasSingleRecording => singleRecording != null;
    [SerializeField] private float[] firstBeatSeconds = { 0f, 2.8f, 5.48f, 8.24f, 10.89f, 13.63f, 16.31f, 19.08f };
    [SerializeField] private float loopLengthSeconds = 21.8f;
    [SerializeField] private AudioSource pianoSource;
    [SerializeField] private AudioSource stringsSource;
    [SerializeField] private AudioSource windsSource;
    [SerializeField] private AudioClip pianoPlaceholder;
    [SerializeField] private AudioClip stringsPlaceholder;
    [SerializeField] private AudioClip windsPlaceholder;
    [SerializeField] private float cueLeadSeconds = 0.65f;

    private double startDspTime;
    private double pausedAtDspTime;
    private bool isPaused;
    public bool IsPaused => isPaused;
    private int lastGuideCue = -1;
    private int lastGuideBeat = -1;
    public bool UsesDemoGuide => singleRecording != null && !recordingBeatMarkersVerified;
    public float GuideBarSeconds => beatsPerBar * 60f / Mathf.Max(guideBeatsPerMinute, 1f);
    public float GuideBeatsPerMinute => guideBeatsPerMinute;

    private int lastCue = -1;
    private int lastBeat = -1;
    private int unlockedLayer;

    public event Action<int> Cue;
    public event Action<int> FirstBeat;
    public double StartDspTime => startDspTime;
    public int BeatCount => firstBeatSeconds != null ? firstBeatSeconds.Length : 0;
    public float CueLeadSeconds => cueLeadSeconds;
    public float GetBarSeconds(int beatId)
    {
        if (UsesDemoGuide) return GuideBarSeconds;
        if (BeatCount < 2) return loopLengthSeconds;
        int index = Mathf.Abs(beatId) % BeatCount;
        if (index < BeatCount - 1) return firstBeatSeconds[index + 1] - firstBeatSeconds[index];
        return singleRecording != null
            ? firstBeatSeconds[index] - firstBeatSeconds[index - 1]
            : loopLengthSeconds - firstBeatSeconds[index] + firstBeatSeconds[0];
    }
    public float[] FirstBeatOffsets => firstBeatSeconds;
    public float LoopLengthSeconds => loopLengthSeconds;
    public int UnlockedLayer => unlockedLayer;
    public bool HasLicensedClips => pianoPlaceholder != null || stringsPlaceholder != null || windsPlaceholder != null;
    public void SetPaused(bool paused)
    {
        if (paused == isPaused) return;
        if (paused)
        {
            pausedAtDspTime = AudioSettings.dspTime;
            isPaused = true;
        }
        else
        {
            startDspTime += AudioSettings.dspTime - pausedAtDspTime;
            isPaused = false;
        }
    }

    private void OnValidate()
    {
        if (firstBeatSeconds == null || firstBeatSeconds.Length == 0)
            return;
        for (int i = 1; i < firstBeatSeconds.Length; i++)
            if (firstBeatSeconds[i] <= firstBeatSeconds[i - 1])
                Debug.LogWarning("First-beat times must be strictly increasing.", this);
        if (loopLengthSeconds <= firstBeatSeconds[firstBeatSeconds.Length - 1])
            Debug.LogWarning("Loop length must extend beyond the final first beat.", this);
    }

private void Start()
    {
        startDspTime = AudioSettings.dspTime + Mathf.Max(0.5f, leadInSeconds);
        if (singleRecording != null)
        {
            if (recordingSource == null) recordingSource = GetComponent<AudioSource>();
            if (recordingSource != null)
            {
                recordingSource.Stop();
                recordingSource.clip = singleRecording;
                recordingSource.playOnAwake = false;
                recordingSource.loop = false;
                recordingSource.spatialBlend = 0f;
            }
            return;
        }
        Configure(pianoSource, pianoPlaceholder, true);
        Configure(stringsSource, stringsPlaceholder, false);
        Configure(windsSource, windsPlaceholder, false);
    }

private void Configure(AudioSource source, AudioClip clip, bool audible)
    {
        if (source == null || clip == null) return;
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = true;
        source.volume = audible ? 1f : 0f;
        source.PlayScheduled(startDspTime);
        if (Mathf.Abs(clip.length - loopLengthSeconds) > 0.1f)
            Debug.LogWarning($"Clip {clip.name} length differs from authored loop; replace or retime it before release.", this);
    }

private void Update()
    {
        if (isPaused) return;
        if (UsesDemoGuide)
        {
            UpdateDemoGuide();
            return;
        }
        if (BeatCount == 0) return;
        if (singleRecording == null && loopLengthSeconds <= firstBeatSeconds[BeatCount - 1]) return;
        double now = AudioSettings.dspTime;
        if (now < startDspTime - cueLeadSeconds) return;
        if (singleRecording != null && now > startDspTime + singleRecording.length) return;
        long loop = singleRecording != null ? 0 : (long)Math.Floor(Math.Max(0d, now - startDspTime) / loopLengthSeconds);
        for (long cycle = singleRecording != null ? 0 : Math.Max(0, loop - 1); cycle <= (singleRecording != null ? 0 : loop + 1); cycle++)
        {
            for (int i = 0; i < BeatCount; i++)
            {
                double beat = startDspTime + cycle * loopLengthSeconds + firstBeatSeconds[i];
                int id = (int)(cycle * BeatCount + i);
                if (now >= beat - cueLeadSeconds && now < beat && id > lastCue)
                {
                    lastCue = id;
                    Cue?.Invoke(id);
                }
                if (now >= beat && id > lastBeat)
                {
                    lastBeat = id;
                    FirstBeat?.Invoke(id);
                }
            }
        }
    }

    private void UpdateDemoGuide()
    {
        double now = AudioSettings.dspTime;
        double first = startDspTime - leadInSeconds + guideLeadInSeconds;
        double bar = GuideBarSeconds;
        if (now < first - cueLeadSeconds) return;
        int current = Mathf.Max(0, (int)Math.Floor((now - first) / bar));
        for (int i = Math.Max(0, current - 1); i <= current + 1; i++)
        {
            double beat = first + i * bar;
            if (now >= beat - cueLeadSeconds && i > lastGuideCue)
            {
                lastGuideCue = i;
                Cue?.Invoke(i);
            }
            if (now >= beat && i > lastGuideBeat)
            {
                lastGuideBeat = i;
                FirstBeat?.Invoke(i);
            }
        }
    }

public bool TryMatch(double dspTime, float window, out int beatId)
    {
        return TryMatch(dspTime, window, out beatId, out _);
    }

    public bool TryMatch(double dspTime, float window, out int beatId, out float offsetSeconds)
    {
        beatId = -1;
        offsetSeconds = float.PositiveInfinity;
        if (isPaused) return false;
        if (UsesDemoGuide)
        {
            double first = startDspTime - leadInSeconds + guideLeadInSeconds;
            if (dspTime < first - window) return false;
            double cycle = Math.Max(0d, Math.Round((dspTime - first) / GuideBarSeconds));
            beatId = (int)cycle;
            offsetSeconds = (float)(dspTime - first - cycle * GuideBarSeconds);
            return Mathf.Abs(offsetSeconds) <= window;
        }
        if (BeatCount == 0 || dspTime < startDspTime) return false;
        if (singleRecording != null && dspTime > startDspTime + singleRecording.length) return false;
        if (singleRecording == null && loopLengthSeconds <= firstBeatSeconds[BeatCount - 1]) return false;
        long loop = singleRecording != null ? 0 : (long)Math.Floor((dspTime - startDspTime) / loopLengthSeconds);
        double nearest = double.MaxValue;
        for (long c = singleRecording != null ? 0 : Math.Max(0, loop - 1); c <= (singleRecording != null ? 0 : Math.Max(0, loop + 1)); c++)
            for (int i = 0; i < BeatCount; i++)
            {
                double signed = dspTime - (startDspTime + c * loopLengthSeconds + firstBeatSeconds[i]);
                if (Math.Abs(signed) < nearest)
                {
                    nearest = Math.Abs(signed);
                    offsetSeconds = (float)signed;
                    beatId = (int)(c * BeatCount + i);
                }
            }
        return nearest <= window;
    }


public void UnlockLayer(int layer)
    {
        unlockedLayer = Mathf.Max(unlockedLayer, layer);
        if (singleRecording != null) return;
        if (stringsSource != null && stringsSource.clip != null) stringsSource.volume = unlockedLayer >= 1 ? 1f : 0f;
        if (windsSource != null && windsSource.clip != null) windsSource.volume = unlockedLayer >= 2 ? 1f : 0f;
    }
}
