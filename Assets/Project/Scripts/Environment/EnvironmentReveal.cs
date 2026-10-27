using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// EnvironmentReveal — makes the world "solidify" when node triggers.
///
/// Effect stages (in order):
///   1. Dissolve transition: materials from transparent/holographic to solid
///   2. Post-processing: URP Volume color grading shifts from cold to warm
///   3. Light pulse: directional light pulses with current pulse BPM
///   4. Audio warmth: mixer parameter transitions to warm/rich
///
/// Attached to "EnvironmentReveal" GameObject in scene.
/// </summary>
public class EnvironmentReveal : MonoBehaviour
{
    [Header("Target Objects")]
    [Tooltip("All mesh renderers to dissolve-transition")]
    [SerializeField] private MeshRenderer[] targetRenderers;

    [Header("Post-Processing (URP Volume)")]
    [Tooltip("Global URP Volume with Color Adjustments override")]
    [SerializeField] private Volume globalVolume;
    [Tooltip("Initial post-exposure (cold/dim)")]
    [SerializeField] private float coldPostExposure = -0.5f;
    [Tooltip("Final post-exposure (warm/bright)")]
    [SerializeField] private float warmPostExposure = 1.5f;

    [Header("Material Transition")]
    [SerializeField] private float dissolveDuration = 8f;
    [Tooltip("Start offset: -1 = fully virtual, 1 = solid")]
    [SerializeField] private float dissolveStartOffset = -1f;
    private static readonly int DissolveProgress = Shader.PropertyToID("_DissolveProgress");

    [Header("Light Pulse")]
    [SerializeField] private Light globalLight;
    [Tooltip("Peak intensity multiplier during pulse")]
    [SerializeField] private float pulseIntensityMultiplier = 1.5f;
    [Tooltip("Color temp curve: start -> end (Kelvin)")]
    [SerializeField] private AnimationCurve colorTempCurve = AnimationCurve.Linear(0f, 7000f, 1f, 3200f);
    [SerializeField] private float colorTransitionDuration = 10f;

    [Header("Audio Transition")]
    [Tooltip("Mixer param name for master warmth")]
    [SerializeField] private string audioWarmthParam = "MasterWarmth";
    [SerializeField] private float audioTransitionDelay = 2f;

    [Header("Sequence Timing")]
    [SerializeField] private float stageDelay = 0.5f;

    private bool isRevealing;
    private float startTime;
    private float originalLightIntensity;
    private float originalLightTemp;
    private ColorAdjustments colorAdjustments;

    private void Start()
    {
        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out colorAdjustments);
        }

        if (globalLight != null)
        {
            originalLightIntensity = globalLight.intensity;
            originalLightTemp = globalLight.useColorTemperature
                ? globalLight.colorTemperature : 6500f;
            if (!globalLight.useColorTemperature)
                globalLight.useColorTemperature = true;
        }
    }

    [ContextMenu("Trigger Reveal")]
    public void TriggerReveal()
    {
        if (isRevealing) return;
        StartCoroutine(RevealSequence());
    }

    private IEnumerator RevealSequence()
    {
        isRevealing = true;
        startTime = Time.time;
        PulseProfile profile = FindFirstObjectByType<PulseMatcher>()?.ActiveProfile;

        yield return StartCoroutine(DissolveTransition());
        yield return new WaitForSeconds(stageDelay);

        StartCoroutine(LightPulseCoroutine(profile));
        yield return StartCoroutine(ColorTempTransition());
        yield return StartCoroutine(PostExposureTransition());
        yield return new WaitForSeconds(audioTransitionDelay);

        yield return StartCoroutine(AudioWarmthTransition());

        Debug.Log("[EnvironmentReveal] Full reveal sequence complete.");
    }

    private IEnumerator DissolveTransition()
    {
        float elapsed = 0f;
        float startVal = dissolveStartOffset;
        float endVal = 1f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dissolveDuration;
            float val = Mathf.Lerp(startVal, endVal, t);

            foreach (var renderer in targetRenderers)
            {
                if (renderer == null) continue;
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(mpb);
                mpb.SetFloat(DissolveProgress, val);
                renderer.SetPropertyBlock(mpb);
            }

            yield return null;
        }
    }

    private IEnumerator LightPulseCoroutine(PulseProfile profile)
    {
        if (globalLight == null) yield break;
        float interval = profile != null ? profile.BeatInterval : 60f / 72f;

        while (true)
        {
            float pulseVal = Mathf.PingPong((Time.time - startTime) / interval, 1f);
            pulseVal = Mathf.SmoothStep(0f, 1f, pulseVal);
            float intensity = originalLightIntensity * (1f + pulseVal * (pulseIntensityMultiplier - 1f));
            globalLight.intensity = intensity;
            yield return null;
        }
    }

    private IEnumerator ColorTempTransition()
    {
        if (globalLight == null) yield break;
        float elapsed = 0f;
        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / colorTransitionDuration;
            globalLight.colorTemperature = colorTempCurve.Evaluate(t);
            yield return null;
        }
    }

    private IEnumerator PostExposureTransition()
    {
        if (colorAdjustments == null) yield break;
        float elapsed = 0f;
        float dur = colorTransitionDuration;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            colorAdjustments.postExposure.value = Mathf.Lerp(coldPostExposure, warmPostExposure, t);
            yield return null;
        }
        colorAdjustments.postExposure.value = warmPostExposure;
    }

    private IEnumerator AudioWarmthTransition()
    {
        float elapsed = 0f;
        float duration = 6f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            AudioManager.Instance?.SetMixerParameter(audioWarmthParam, t);
            yield return null;
        }
        AudioManager.Instance?.SetMixerParameter(audioWarmthParam, 1f);
    }
}
