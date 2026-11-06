using System;
using UnityEngine;

/// <summary>One authored set of bar-one timestamps drives the audio, cue and judgement.</summary>
public class FirstBeatTimeline : MonoBehaviour
{
    [Header("Shared Tuning")]
    [SerializeField] private ConductorTuning tuning;
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

    public ConductorTuning Tuning => tuning;
    public AudioSource RecordingSource => recordingSource;
    public bool HasVerifiedBeatMarkers => singleRecording == null ||
        (tuning != null ? tuning.recordingBeatMarkersVerified && MarkersValid : recordingBeatMarkersVerified);
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
    private bool transportStarted;
    public bool IsPaused => isPaused;
    private int lastGuideCue = -1;
    private int lastGuideStrike = -1;
    private int lastGuideBeat = -1;
    public bool UsesDemoGuide => singleRecording != null && !HasVerifiedBeatMarkers;
    public float GuideBarSeconds => (tuning != null ? tuning.beatsPerBar : beatsPerBar) *
        60f / Mathf.Max(tuning != null ? tuning.previewBeatsPerMinute : guideBeatsPerMinute, 1f);
    public float GuideBeatsPerMinute => tuning != null ? tuning.previewBeatsPerMinute : guideBeatsPerMinute;
    private float[] BeatOffsets => tuning != null ? tuning.firstBeatSeconds : firstBeatSeconds;
    private bool MarkersValid
    {
        get
        {
            if (BeatCount == 0) return false;
            for (int i = 0; i < BeatCount; i++)
                if (BeatOffsets[i] < 0f || BeatOffsets[i] >= singleRecording.length ||
                    (i > 0 && BeatOffsets[i] <= BeatOffsets[i - 1])) return false;
            return true;
        }
    }
    private double DemoFirstBeat => startDspTime +
        (tuning != null ? tuning.previewFirstBeatOffsetSeconds : guideLeadInSeconds - leadInSeconds);
    private float JudgementOffset => tuning != null ? tuning.judgementOffsetSeconds : 0f;
    private float InputLatency => tuning != null ? tuning.inputLatencyCompensationSeconds : 0f;

    private int lastCue = -1;
    private int lastStrike = -1;
    private int lastBeat = -1;
    private int unlockedLayer;

    public event Action<int> Cue;
    public event Action<int> MeteorStrike;
    public event Action<int> FirstBeat;
    public double StartDspTime => startDspTime;
    public int BeatCount => BeatOffsets != null ? BeatOffsets.Length : 0;
    public float CueLeadSeconds => tuning != null ? tuning.meteorCueLeadSeconds : cueLeadSeconds;
    public float MeteorStrikeLeadSeconds
    {
        get
        {
            float referenceBar = 4f * 60f / Mathf.Max(1f, GuideBeatsPerMinute);
            float duration = tuning != null ? tuning.meteorStrikeSeconds : 0.46f;
            return duration * (tuning != null ? tuning.meteorBeatPhase : 0.5f) *
                GuideBarSeconds / referenceBar;
        }
    }
    public float GetBarSeconds(int beatId)
    {
        if (UsesDemoGuide) return GuideBarSeconds;
        if (BeatCount < 2) return loopLengthSeconds;
        int index = Mathf.Abs(beatId) % BeatCount;
        if (index < BeatCount - 1) return BeatOffsets[index + 1] - BeatOffsets[index];
        return singleRecording != null
            ? BeatOffsets[index] - BeatOffsets[index - 1]
            : loopLengthSeconds - BeatOffsets[index] + BeatOffsets[0];
    }
    public float[] FirstBeatOffsets => BeatOffsets;
    public float RecordingLengthSeconds => singleRecording != null ? singleRecording.length : 0f;
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
            if (!transportStarted)
            {
                startDspTime = AudioSettings.dspTime + Mathf.Max(0.5f, leadInSeconds);
                transportStarted = true;
                if (singleRecording != null && recordingSource != null &&
                    (tuning == null || tuning.musicMode == ConductorTuning.MusicMode.Timeline))
                {
                    recordingSource.Stop();
                    recordingSource.pitch = 1f;
                    recordingSource.volume = 0f;
                    recordingSource.PlayScheduled(startDspTime);
                }
                else if (singleRecording == null)
                {
                    Configure(pianoSource, pianoPlaceholder, true);
                    Configure(stringsSource, stringsPlaceholder, false);
                    Configure(windsSource, windsPlaceholder, false);
                }
            }
            else
                startDspTime += AudioSettings.dspTime - pausedAtDspTime;
            isPaused = false;
        }
    }

    public double RecordingPositionSeconds =>
        (isPaused ? pausedAtDspTime : AudioSettings.dspTime) - startDspTime;

    private void OnValidate()
    {
        float[] offsets = BeatOffsets;
        if (offsets == null || offsets.Length == 0)
            return;
        for (int i = 1; i < offsets.Length; i++)
            if (offsets[i] <= offsets[i - 1])
                Debug.LogWarning("First-beat times must be strictly increasing.", this);
        if (singleRecording == null && loopLengthSeconds <= offsets[offsets.Length - 1])
            Debug.LogWarning("Loop length must extend beyond the final first beat.", this);
    }

    private void Awake()
    {
        if (recordingSource == null) recordingSource = GetComponent<AudioSource>();
        if (singleRecording != null && recordingSource != null)
        {
            recordingSource.playOnAwake = false;
            recordingSource.clip = singleRecording;
            recordingSource.volume = 0f;
        }
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
                recordingSource.volume = 0f;
            }
            return;
        }
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
        if (!transportStarted)
        {
            if (isPaused) return;
            SetPaused(true);
            SetPaused(false);
        }
        if (isPaused) return;
        if (UsesDemoGuide)
        {
            UpdateDemoGuide();
            return;
        }
        if (BeatCount == 0) return;
        if (singleRecording == null && loopLengthSeconds <= BeatOffsets[BeatCount - 1]) return;
        double now = AudioSettings.dspTime;
        if (now < startDspTime - Math.Max(CueLeadSeconds, MeteorStrikeLeadSeconds)) return;
        if (singleRecording != null && now > startDspTime + singleRecording.length) return;
        long loop = singleRecording != null ? 0 : (long)Math.Floor(Math.Max(0d, now - startDspTime) / loopLengthSeconds);
        for (long cycle = singleRecording != null ? 0 : Math.Max(0, loop - 1); cycle <= (singleRecording != null ? 0 : loop + 1); cycle++)
        {
            for (int i = 0; i < BeatCount; i++)
            {
                double beat = startDspTime + cycle * loopLengthSeconds + BeatOffsets[i];
                int id = (int)(cycle * BeatCount + i);
                if (now >= beat - CueLeadSeconds && now < beat && id > lastCue)
                {
                    lastCue = id;
                    Cue?.Invoke(id);
                }
                if (now >= beat - MeteorStrikeLeadSeconds && now < beat && id > lastStrike)
                {
                    lastStrike = id;
                    MeteorStrike?.Invoke(id);
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
        double first = DemoFirstBeat;
        double bar = GuideBarSeconds;
        if (now < first - Math.Max(CueLeadSeconds, MeteorStrikeLeadSeconds) ||
            (singleRecording != null && now > startDspTime + singleRecording.length)) return;
        int current = Mathf.Max(0, (int)Math.Floor((now - first) / bar));
        for (int i = Math.Max(0, current - 1); i <= current + 1; i++)
        {
            double beat = first + i * bar;
            if (now >= beat - CueLeadSeconds && now < beat && i > lastGuideCue)
            {
                lastGuideCue = i;
                Cue?.Invoke(i);
            }
            if (now >= beat - MeteorStrikeLeadSeconds && now < beat && i > lastGuideStrike)
            {
                lastGuideStrike = i;
                MeteorStrike?.Invoke(i);
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
        dspTime -= InputLatency;
        beatId = -1;
        offsetSeconds = float.PositiveInfinity;
        if (isPaused) return false;
        if (UsesDemoGuide)
        {
            if (singleRecording != null && dspTime > startDspTime + singleRecording.length) return false;
            double first = DemoFirstBeat + JudgementOffset;
            if (dspTime < first - window) return false;
            double cycle = Math.Max(0d, Math.Floor((dspTime - first) / GuideBarSeconds + 0.5d));
            beatId = (int)cycle;
            offsetSeconds = (float)(dspTime - first - cycle * GuideBarSeconds);
            return Mathf.Abs(offsetSeconds) <= window;
        }
        if (BeatCount == 0 || dspTime < startDspTime - window) return false;
        if (singleRecording != null && dspTime > startDspTime + singleRecording.length) return false;
        if (singleRecording == null && loopLengthSeconds <= BeatOffsets[BeatCount - 1]) return false;
        long loop = singleRecording != null ? 0 : (long)Math.Floor((dspTime - startDspTime) / loopLengthSeconds);
        double nearest = double.MaxValue;
        for (long c = singleRecording != null ? 0 : Math.Max(0, loop - 1); c <= (singleRecording != null ? 0 : Math.Max(0, loop + 1)); c++)
            for (int i = 0; i < BeatCount; i++)
            {
                double signed = dspTime - (startDspTime + c * loopLengthSeconds + BeatOffsets[i] + JudgementOffset);
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
