using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ConductorBlackout : MonoBehaviour
{
    [Header("Initial Darkness")]
    [Range(0f, 1f)] [SerializeField] private float initialDarkness = 0.995f;
    [Range(0f, 1f)] [SerializeField] private float darknessRemovedPerPathSegment = 0.11f;

    [Header("Motion - Faint Response")]
    [Range(0f, 1f)] [SerializeField] private float motionDarkness = 0.92f;
    [Min(0f)] [SerializeField] private float motionHoldSeconds = 0.08f;

    [Header("Off Beat")]
    [Range(0f, 1f)] [SerializeField] private float offBeatDarkness = 0.75f;
    [Min(0f)] [SerializeField] private float offBeatHoldSeconds = 0.35f;

    [Header("Good")]
    [Range(0f, 1f)] [SerializeField] private float goodDarkness = 0.55f;
    [Min(0f)] [SerializeField] private float goodHoldSeconds = 0.65f;

    [Header("Great")]
    [Range(0f, 1f)] [SerializeField] private float greatDarkness = 0.35f;
    [Min(0f)] [SerializeField] private float greatHoldSeconds = 0.9f;

    [Header("Perfect")]
    [Range(0f, 1f)] [SerializeField] private float perfectDarkness = 0.15f;
    [Min(0f)] [SerializeField] private float perfectHoldSeconds = 1.2f;

    [Header("Shared Light Envelope")]
    [Min(0.01f)] [SerializeField] private float brightenSeconds = 0.55f;
    [SerializeField] private AnimationCurve brightenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Min(0.01f)] [SerializeField] private float fadeBackSeconds = 2.5f;
    [SerializeField] private Shader blackoutShader;

    private Material material;
    private float currentDarkness;
    private float eventStartTime = -100f;
    private float eventStartDarkness;
    private float eventTargetDarkness;
    private float eventHoldSeconds;
    private int permanentSegments;
    private bool hasMoved;

    public bool HasRevealed => hasMoved;
    public float CurrentDarkness => currentDarkness;
    public float CurrentVisibility => 1f - currentDarkness;

    private void Awake()
    {
        currentDarkness = initialDarkness;
        if (blackoutShader == null) blackoutShader = Shader.Find("Pulse/ConductorBlackout");
        if (blackoutShader != null) material = new Material(blackoutShader);
    }

    public void ShowRating(ConductorGestureRating rating)
    {
        hasMoved = true;
        float target = rating == ConductorGestureRating.Perfect ? perfectDarkness
            : rating == ConductorGestureRating.Great ? greatDarkness
            : rating == ConductorGestureRating.Good ? goodDarkness
            : rating == ConductorGestureRating.OffBeat ? offBeatDarkness : motionDarkness;
        float hold = rating == ConductorGestureRating.Perfect ? perfectHoldSeconds
            : rating == ConductorGestureRating.Great ? greatHoldSeconds
            : rating == ConductorGestureRating.Good ? goodHoldSeconds
            : rating == ConductorGestureRating.OffBeat ? offBeatHoldSeconds : motionHoldSeconds;
        float baseline = BaselineDarkness;
        float currentTarget = EventDarkness(Time.time, baseline);
        if (rating == ConductorGestureRating.Motion && currentTarget < target)
            return;
        eventStartDarkness = currentDarkness;
        eventTargetDarkness = Mathf.Min(target, baseline);
        eventHoldSeconds = hold;
        eventStartTime = Time.time;
    }

    public void SetPermanentSegments(int segments)
    {
        permanentSegments = Mathf.Max(permanentSegments, segments);
    }

    private float BaselineDarkness => Mathf.Clamp01(initialDarkness - permanentSegments * darknessRemovedPerPathSegment);

private float EventDarkness(float now, float baseline)
    {
        float elapsed = now - eventStartTime;
        if (elapsed < 0f) return baseline;
        float envelope = ConductorLightEnvelope.Evaluate(elapsed, brightenSeconds,
            eventHoldSeconds, fadeBackSeconds, brightenCurve);
        if (elapsed < brightenSeconds)
            return Mathf.Lerp(eventStartDarkness, eventTargetDarkness, envelope);
        return Mathf.Lerp(baseline, eventTargetDarkness, envelope);
    }

    private void Update()
    {
        float baseline = BaselineDarkness;
        currentDarkness = Mathf.Min(baseline, EventDarkness(Time.time, baseline));
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
