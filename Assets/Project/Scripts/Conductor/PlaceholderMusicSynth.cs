using UnityEngine;

/// <summary>Clearly synthetic, original demo tones until licensed synchronized stems are supplied.</summary>
[RequireComponent(typeof(FirstBeatTimeline))]
public class PlaceholderMusicSynth : MonoBehaviour
{
    [SerializeField] private bool enablePlaceholder = true;
    [SerializeField] private float masterLevel = 0.09f;
    [SerializeField] private float[] chord = { 220f, 261.63f, 329.63f, 392f };

    private FirstBeatTimeline timeline;
    private volatile int unlockedLayer;
    private double sampleRate;
    private double startTime;
    private float[] beatOffsets;
    private float loopLength;

private void Awake()
    {
        timeline = GetComponent<FirstBeatTimeline>();
        sampleRate = AudioSettings.outputSampleRate;
        beatOffsets = timeline.FirstBeatOffsets;
        loopLength = timeline.LoopLengthSeconds;
        enablePlaceholder = enablePlaceholder && !timeline.HasLicensedClips && !timeline.HasSingleRecording;
    }

private void Start()
    {
        startTime = timeline.StartDspTime;
        if (!enablePlaceholder && !timeline.HasSingleRecording) GetComponent<AudioSource>().Stop();
    }

    private void Update() { unlockedLayer = timeline.UnlockedLayer; }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!enablePlaceholder || startTime <= 0d || loopLength <= 0f) return;
        double now = AudioSettings.dspTime;
        for (int frame = 0; frame < data.Length / channels; frame++)
        {
            double time = now + frame / sampleRate - startTime;
            if (time < 0d) continue;
            double barTime = time % loopLength;
            double age = 100d;
            for (int i = 0; i < beatOffsets.Length; i++)
            {
                double delta = barTime - beatOffsets[i];
                if (delta >= 0d && delta < age) age = delta;
            }
            float tone = 0f;
            if (age < 0.55d)
            {
                float envelope = Mathf.Exp((float)(-age * 8d));
                tone += Mathf.Sin((float)(time * chord[0] * 6.2831853d)) * envelope;
            }
            if (unlockedLayer >= 1) tone += Mathf.Sin((float)(time * chord[1] * 6.2831853d)) * 0.14f;
            if (unlockedLayer >= 2) tone += Mathf.Sin((float)(time * chord[2] * 6.2831853d)) * 0.1f;
            float sample = Mathf.Clamp(tone * masterLevel, -0.5f, 0.5f);
            for (int channel = 0; channel < channels; channel++) data[frame * channels + channel] += sample;
        }
    }
}
