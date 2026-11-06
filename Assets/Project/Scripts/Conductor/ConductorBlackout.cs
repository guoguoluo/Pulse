using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ConductorBlackout : MonoBehaviour
{
    [SerializeField] private ConductorTuning tuning;
    [Header("Score Mapping Fallback")]
    [Range(0f, 1f)] [SerializeField] private float initialDarkness = 1f;
    [Range(0f, 1f)] [SerializeField] private float fullScoreDarkness;

    [Header("Shared Light Envelope")]
    [Min(0.01f)] [SerializeField] private float brightenSeconds = 0.55f;
    [SerializeField] private AnimationCurve brightenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Shader blackoutShader;

    private Material material;
    private float currentDarkness;
    private float transitionStartTime;
    private float transitionStartDarkness;
    private float targetDarkness;
    private bool hasMoved;

    public bool HasRevealed => hasMoved;
    public float CurrentDarkness => currentDarkness;
    public float CurrentVisibility => 1f - currentDarkness;

    private void Awake()
    {
        if (tuning == null) { var timeline = FindFirstObjectByType<FirstBeatTimeline>(); if (timeline != null) tuning = timeline.Tuning; }
        currentDarkness = InitialDarkness;
        targetDarkness = currentDarkness;
        if (blackoutShader == null) blackoutShader = Shader.Find("Pulse/ConductorBlackout");
        if (blackoutShader != null) material = new Material(blackoutShader);
    }

    public void SetLightScore(int score, int maximumScore)
    {
        float normalized = tuning != null ? tuning.LightFraction(score, maximumScore)
            : Mathf.Clamp01((float)score / Mathf.Max(1, maximumScore));
        float nextTarget = Mathf.Lerp(InitialDarkness,
            tuning != null ? tuning.fullScoreDarkness : fullScoreDarkness, normalized);
        if (Mathf.Approximately(targetDarkness, nextTarget)) return;
        transitionStartDarkness = currentDarkness;
        targetDarkness = nextTarget;
        transitionStartTime = Time.time;
        hasMoved |= score > 0;
    }

    private float InitialDarkness => tuning != null ? tuning.initialDarkness : initialDarkness;
    private float BrightenSeconds => tuning != null ? tuning.brightenSeconds : brightenSeconds;
    private AnimationCurve BrightenCurve => tuning != null ? tuning.brightenCurve : brightenCurve;

    private void Update()
    {
        float duration = BrightenSeconds;
        float progress = Mathf.Clamp01((Time.time - transitionStartTime) / Mathf.Max(0.01f, duration));
        float shaped = BrightenCurve != null ? BrightenCurve.Evaluate(progress) : progress;
        currentDarkness = Mathf.Lerp(transitionStartDarkness, targetDarkness, shaped);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        material.SetFloat("_Darkness", currentDarkness);
        Graphics.Blit(source, destination, material);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
