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

    [Header("Persistent Light Path")]
    [Min(1)] [SerializeField] private int pathSegments = 6;

    [Header("Staircase Raise Conditions")]
    [Tooltip("Horizontal distance in world units from StaircaseRoot. Set 0 to require standing at the stairs.")]
    [Min(0f)] [SerializeField] private float staircaseTriggerDistance = 50f;
    [Tooltip("Motion=1, OffBeat=2, Good=3, Great=4, Perfect=5. Only matched first-beat strokes can count.")]
    [SerializeField] private ConductorGestureRating staircaseMinimumRating = ConductorGestureRating.Good;
    [Tooltip("Consecutive qualifying first beats. A missed beat, off-beat stroke, low rating, or leaving range resets only this streak.")]
    [Min(1)] [SerializeField] private int staircaseRequiredHits = 3;

    private ConductorBlackout blackout;
    private int progress;
    private int staircaseHits;
    private int lastHitBeat = -1;
    private int lastStaircaseBeat = -1;
    private int pendingBeat = -1;
    private double pendingBeatDeadline;
    private bool pendingBeatQualified;
    private bool pendingBeatIsInRange;
    private double lastProgressTime;
    private bool completed;

    public int Progress => progress;
    public int StaircaseHits => staircaseHits;
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
        staircaseRequiredHits = Mathf.Max(1, staircaseRequiredHits);
        if (staircaseMinimumRating < ConductorGestureRating.Good)
            staircaseMinimumRating = ConductorGestureRating.Good;
    }

    private void Start() { lastProgressTime = AudioSettings.dspTime; }

    private void Update()
    {
        if (timeline != null && timeline.IsPaused) return;
        if (completed) return;
        double idle = AudioSettings.dspTime - lastProgressTime;
        feedback?.SetAssistance(Mathf.Clamp01((float)((idle - idleGraceSeconds) / Mathf.Max(0.1f, idleGraceSeconds))));
        if (!IsNearStaircase())
        {
            staircaseHits = 0;
            lastStaircaseBeat = -1;
        }
        if (pendingBeat >= 0 && AudioSettings.dspTime > pendingBeatDeadline)
        {
            if (!pendingBeatQualified && pendingBeatIsInRange && lastHitBeat != pendingBeat)
            {
                staircaseHits = 0;
                lastStaircaseBeat = -1;
            }
            pendingBeat = -1;
        }
    }

    private void OnMotion()
    {
        blackout?.ShowRating(ConductorGestureRating.Motion);
        feedback?.ShowRating(ConductorGestureRating.Motion);
        LastRating = ConductorGestureRating.Motion;
    }

    private void OnCue(int id) { feedback?.CueMeteor(); }
    private void OnFirstBeat(int id)
    {
        feedback?.StrikeMeteor();
        if (completed) return;
        if (pendingBeat >= 0 && pendingBeat != id && !pendingBeatQualified && pendingBeatIsInRange
            && lastHitBeat != pendingBeat)
        {
            staircaseHits = 0;
            lastStaircaseBeat = -1;
        }
        pendingBeat = id;
        pendingBeatQualified = id == lastHitBeat;
        pendingBeatIsInRange = IsNearStaircase();
        pendingBeatDeadline = AudioSettings.dspTime + maxAssistWindow;
    }

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
            if (rating >= staircaseMinimumRating && IsNearStaircase())
            {
                staircaseHits = lastStaircaseBeat < 0 || beatId == lastStaircaseBeat + 1
                    ? Mathf.Min(staircaseRequiredHits, staircaseHits + 1) : 1;
                lastStaircaseBeat = beatId;
                if (pendingBeat == beatId) pendingBeatQualified = true;
            }
            else
            {
                staircaseHits = 0;
                lastStaircaseBeat = -1;
            }
            if (staircaseHits >= staircaseRequiredHits) Complete();
        }
        else if (!hit)
        {
            staircaseHits = 0;
            lastStaircaseBeat = -1;
        }
        LastRating = rating;
        blackout?.ShowRating(rating);
        feedback?.ShowRating(rating);
    }

    private bool IsNearStaircase()
    {
        if (player == null || staircase == null) return false;
        Vector3 delta = player.position - staircase.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= staircaseTriggerDistance * staircaseTriggerDistance;
    }

    private void Complete()
    {
        completed = true;
        feedback?.Complete();
        staircase?.Raise();
    }
}
