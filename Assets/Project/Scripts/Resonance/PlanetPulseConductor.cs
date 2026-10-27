using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Plays the reference pulse and exposes the current target rhythm as absolute times.
/// Designers author the rhythm as beat offsets from the loop start.
/// </summary>
public class PlanetPulseConductor : MonoBehaviour
{
    [Header("Rhythm")]
    [SerializeField] private float bpm = 72f;
    [SerializeField] private float loopLeadIn = 1f;
    [SerializeField] private List<float> beatPattern = new List<float> { 0f, 1f, 2.5f, 3f };
    [SerializeField] private float loopExtraBeatPadding = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource pulseSource;
    [SerializeField] private AudioClip pulseClip;
    [SerializeField] private AudioMixerGroup pulseMixerGroup;
    [SerializeField] private bool playOnStart = true;

    [Header("Feedback")]
    [SerializeField] private Light pulseLight;
    [SerializeField] private float pulseLightBaseIntensity = 0.8f;
    [SerializeField] private float pulseLightPeakIntensity = 2f;
    [SerializeField] private float pulseLightDuration = 0.18f;

    private float loopStartTime;
    private bool started;

    public float Bpm => bpm;
    public float BeatDuration => 60f / bpm;
    public IReadOnlyList<float> BeatPattern => beatPattern;

    private void Start()
    {
        if (playOnStart)
            BeginPulse();
    }

    public void BeginPulse()
    {
        if (started)
            return;

        started = true;
        loopStartTime = Time.time + loopLeadIn;

        if (pulseSource != null)
        {
            pulseSource.clip = pulseClip;
            if (pulseMixerGroup != null)
                pulseSource.outputAudioMixerGroup = pulseMixerGroup;
        }

        StartCoroutine(PulseLoop());
    }

    public List<float> GetCurrentPatternTimes()
    {
        List<float> times = new List<float>(beatPattern.Count);
        float loopLength = GetLoopLength();
        float now = Time.time;
        float activeLoopStart = loopStartTime;

        if (started && loopLength > 0.0001f && now > loopStartTime)
        {
            float elapsed = now - loopStartTime;
            float loopsPassed = Mathf.Floor(elapsed / loopLength);
            activeLoopStart = loopStartTime + loopsPassed * loopLength;
        }

        for (int i = 0; i < beatPattern.Count; i++)
            times.Add(activeLoopStart + beatPattern[i] * BeatDuration);

        return times;
    }

    private float GetLoopLength()
    {
        if (beatPattern == null || beatPattern.Count == 0)
            return BeatDuration;

        return (beatPattern[beatPattern.Count - 1] + loopExtraBeatPadding) * BeatDuration;
    }

    private IEnumerator PulseLoop()
    {
        while (Time.time < loopStartTime)
            yield return null;

        while (true)
        {
            float currentLoopStart = loopStartTime;
            for (int i = 0; i < beatPattern.Count; i++)
            {
                float targetTime = currentLoopStart + beatPattern[i] * BeatDuration;
                while (Time.time < targetTime)
                    yield return null;

                if (pulseSource != null && pulseClip != null)
                    pulseSource.PlayOneShot(pulseClip);

                if (pulseLight != null)
                    StartCoroutine(PulseLightRoutine());
            }

            loopStartTime += GetLoopLength();
        }
    }

    private IEnumerator PulseLightRoutine()
    {
        pulseLight.intensity = pulseLightPeakIntensity;
        float elapsed = 0f;
        while (elapsed < pulseLightDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseLightDuration;
            pulseLight.intensity = Mathf.Lerp(pulseLightPeakIntensity, pulseLightBaseIntensity, t);
            yield return null;
        }

        pulseLight.intensity = pulseLightBaseIntensity;
    }
}
