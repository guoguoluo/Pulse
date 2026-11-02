using UnityEngine;

/// <summary>Persistent, non-punitive progression from downstrokes to the existing staircase.</summary>
[DisallowMultipleComponent]
public class ConductorDemo : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DownstrokeInput input;
    [SerializeField] private FirstBeatTimeline timeline;
    [SerializeField] private ConductorWorldFeedback feedback;
    [SerializeField] private RisingStaircase staircase;
    [SerializeField] private Transform player;
    [SerializeField] private Transform resonanceNode;

    [Header("First-Beat Judgement (seconds)")]
    [Tooltip("Early/late tolerance for Perfect.")]
    [Min(0f)] [SerializeField] private float perfectWindowSeconds = 0.08f;
    [Tooltip("Early/late tolerance for Great.")]
    [Min(0f)] [SerializeField] private float greatWindowSeconds = 0.17f;
    [Tooltip("Early/late tolerance for Good, and the normal progress window.")]
    [Min(0f)] [SerializeField] private float hitWindowSeconds = 0.3f;
    [Tooltip("Seconds without progress before assistance begins.")]
    [Min(0f)] [SerializeField] private float idleGraceSeconds = 12f;
    [Tooltip("Maximum Good tolerance after prolonged inactivity.")]
    [Min(0f)] [SerializeField] private float maxAssistWindow = 0.48f;

    [Header("Progression")]
    [Min(1)] [SerializeField] private int pathSegments = 6;
    [Min(0f)] [SerializeField] private float nodeRadius = 4f;

    private ConductorBlackout blackout;
    private int progress;
    private int lastHitBeat = -1;
    private double lastProgressTime;
    private bool completed;

    public int Progress => progress;
    public bool Completed => completed;
    public ConductorGestureRating LastRating { get; private set; }
    public float LastBeatOffsetSeconds { get; private set; }

    private void Awake()
    {
        if (input == null) input = FindFirstObjectByType<DownstrokeInput>();
        if (timeline == null) timeline = FindFirstObjectByType<FirstBeatTimeline>();
        if (feedback == null) feedback = FindFirstObjectByType<ConductorWorldFeedback>();
        if (staircase == null) staircase = FindFirstObjectByType<RisingStaircase>();
        if (player == null) { var controller = FindFirstObjectByType<PlayerController>(); if (controller != null) player = controller.transform; }
        if (resonanceNode == null && staircase != null) resonanceNode = staircase.transform;
        blackout = FindFirstObjectByType<ConductorBlackout>();
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.Motion += OnMotion;
            input.Downstroke += OnDownstroke;
        }
        if (timeline != null)
        {
            timeline.Cue += OnCue;
            timeline.FirstBeat += OnFirstBeat;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.Motion -= OnMotion;
            input.Downstroke -= OnDownstroke;
        }
        if (timeline != null)
        {
            timeline.Cue -= OnCue;
            timeline.FirstBeat -= OnFirstBeat;
        }
    }

    private void OnValidate()
    {
        greatWindowSeconds = Mathf.Max(greatWindowSeconds, perfectWindowSeconds);
        hitWindowSeconds = Mathf.Max(hitWindowSeconds, greatWindowSeconds);
        maxAssistWindow = Mathf.Max(maxAssistWindow, hitWindowSeconds);
    }

    private void Start() { lastProgressTime = AudioSettings.dspTime; }

    private void Update()
    {
        if (completed) return;
        double idle = AudioSettings.dspTime - lastProgressTime;
        feedback?.SetAssistance(Mathf.Clamp01((float)((idle - idleGraceSeconds) / Mathf.Max(0.1f, idleGraceSeconds))));
        if (progress >= pathSegments && player != null && resonanceNode != null)
        {
            Vector3 a = player.position; Vector3 b = resonanceNode.position; a.y = b.y = 0f;
            if (Vector3.Distance(a, b) <= nodeRadius) Complete();
        }
    }

private void OnMotion()
    {
        blackout?.ShowRating(ConductorGestureRating.Motion);
        feedback?.ShowRating(ConductorGestureRating.Motion);
        LastRating = ConductorGestureRating.Motion;
    }

    private void OnCue(int id) { feedback?.CueMeteor(); }
    private void OnFirstBeat(int id) { feedback?.StrikeMeteor(); }

private void OnDownstroke(double dspTime)
    {
        if (completed || timeline == null)
        {
            LastRating = ConductorGestureRating.OffBeat;
            blackout?.ShowRating(LastRating);
            feedback?.ShowRating(LastRating);
            return;
        }

        float assist = Mathf.Clamp01((float)((dspTime - lastProgressTime - idleGraceSeconds) / Mathf.Max(0.1f, idleGraceSeconds)));
        float window = Mathf.Lerp(hitWindowSeconds, maxAssistWindow, assist);
        bool hit = timeline.TryMatch(dspTime, window, out int beatId, out float offset);
        LastBeatOffsetSeconds = offset;
        ConductorGestureRating rating = ConductorGestureRating.OffBeat;
        if (hit && beatId != lastHitBeat)
        {
            float deviation = Mathf.Abs(offset);
            rating = deviation <= perfectWindowSeconds ? ConductorGestureRating.Perfect
                : deviation <= greatWindowSeconds ? ConductorGestureRating.Great : ConductorGestureRating.Good;
            lastHitBeat = beatId;
            lastProgressTime = dspTime;
            progress = Mathf.Min(pathSegments, progress + 1);
            feedback?.AdvancePath(progress, pathSegments);
            blackout?.SetPermanentSegments(progress);
            if (progress >= 2) timeline.UnlockLayer(1);
            if (progress >= 4) timeline.UnlockLayer(2);
        }
        LastRating = rating;
        blackout?.ShowRating(rating);
        feedback?.ShowRating(rating);
    }

    private void Complete()
    {
        completed = true;
        feedback?.Complete();
        staircase?.Raise();
    }
}
