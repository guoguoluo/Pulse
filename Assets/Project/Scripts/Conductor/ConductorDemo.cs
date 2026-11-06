using System.Collections.Generic;
using UnityEngine;

/// <summary>Timed score controls light and volume; first-beat path progress remains persistent.</summary>
[DisallowMultipleComponent]
public class ConductorDemo : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DownstrokeInput input;
    [SerializeField] private FirstBeatTimeline timeline;
    [SerializeField] private ConductorWorldFeedback feedback;
    [SerializeField] private RisingStaircase staircase;
    [SerializeField] private Transform player;
    [SerializeField] private ConductorTuning tuning;

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
    [Tooltip("Good, Great or Perfect. Only beat-matched mouse actions can count.")]
    [SerializeField] private ConductorGestureRating staircaseMinimumRating = ConductorGestureRating.Good;
    [Tooltip("Consecutive qualifying first beats. A missed beat, off-beat action, low rating, or leaving range resets only this streak.")]
    [Min(1)] [SerializeField] private int staircaseRequiredHits = 3;

    private ConductorBlackout blackout;
    private GameMenuController gameMenu;
    private struct ScoreAward
    {
        public int points;
        public double expiresAt;
        public ConductorGestureRating rating;
    }
    private readonly List<ScoreAward> scoreAwards = new List<ScoreAward>();
    private int lightScore;
    private double lastMotionAwardTime = double.NegativeInfinity;
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
    public int LightScore => lightScore;
    public int ActiveAwardCount(ConductorGestureRating rating)
    {
        int count = 0;
        double now = double.IsNaN(pausedAt) ? AudioSettings.dspTime : pausedAt;
        for (int i = 0; i < scoreAwards.Count; i++)
            if (scoreAwards[i].rating == rating && scoreAwards[i].expiresAt > now)
                count++;
        return count;
    }
    public int StaircaseHits => staircaseHits;
    public bool Completed => completed;
    public ConductorGestureRating LastRating { get; private set; }
    public float LastBeatOffsetSeconds { get; private set; }

    private void Awake()
    {
        if (input == null) input = FindFirstObjectByType<DownstrokeInput>();
        if (timeline == null) timeline = FindFirstObjectByType<FirstBeatTimeline>();
        if (tuning == null && timeline != null) tuning = timeline.Tuning;
        if (feedback == null) feedback = FindFirstObjectByType<ConductorWorldFeedback>();
        if (staircase == null) staircase = FindFirstObjectByType<RisingStaircase>();
        if (player == null) { var controller = FindFirstObjectByType<PlayerController>(); if (controller != null) player = controller.transform; }
        blackout = FindFirstObjectByType<ConductorBlackout>();
        gameMenu = FindFirstObjectByType<GameMenuController>();
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
    private double pausedAt = double.NaN;

    public void SetScoringPaused(bool paused)
    {
        if (paused)
        {
            if (double.IsNaN(pausedAt)) pausedAt = AudioSettings.dspTime;
            return;
        }

        if (double.IsNaN(pausedAt)) return;
        double duration = AudioSettings.dspTime - pausedAt;
        lastProgressTime += duration;
        pendingBeatDeadline += duration;
        if (!double.IsNegativeInfinity(lastMotionAwardTime)) lastMotionAwardTime += duration;
        for (int i = 0; i < scoreAwards.Count; i++)
        {
            ScoreAward award = scoreAwards[i];
            award.expiresAt += duration;
            scoreAwards[i] = award;
        }
        pausedAt = double.NaN;
    }
    private float PerfectWindow => tuning != null ? tuning.perfectWindowSeconds : perfectWindowSeconds;
    private float GreatWindow => tuning != null ? tuning.greatWindowSeconds : greatWindowSeconds;
    private float GoodWindow => tuning != null ? tuning.goodWindowSeconds : hitWindowSeconds;
    private float IdleGrace => tuning != null ? tuning.idleGraceSeconds : idleGraceSeconds;
    private float AssistWindow => tuning != null ? tuning.maxAssistWindowSeconds : maxAssistWindow;
    private int PathSegments => tuning != null ? tuning.pathSegments : pathSegments;
    private float StairDistance => tuning != null ? tuning.staircaseTriggerDistance : staircaseTriggerDistance;
    private ConductorGestureRating StairRating => tuning != null ? tuning.staircaseMinimumRating : staircaseMinimumRating;
    private int StairHits => tuning != null ? tuning.staircaseRequiredHits : staircaseRequiredHits;
    private int MaxLightScore => tuning != null ? tuning.maximumLightScore : 100;

    private void ApplyLightScore()
    {
        blackout?.SetLightScore(lightScore, MaxLightScore);
        feedback?.SetLightScore(lightScore, MaxLightScore);
    }

    private void AddLightScore(ConductorGestureRating rating)
    {
        double now = AudioSettings.dspTime;
        bool motion = rating == ConductorGestureRating.Motion;
        int motionTotal = 0;
        if (motion)
        {
            float interval = tuning != null ? tuning.motionAwardIntervalSeconds : 0.45f;
            if (now - lastMotionAwardTime < interval) return;
            for (int i = 0; i < scoreAwards.Count; i++)
                if (scoreAwards[i].rating == ConductorGestureRating.Motion && scoreAwards[i].expiresAt > now)
                    motionTotal += scoreAwards[i].points;
            if (motionTotal >= (tuning != null ? tuning.motionScoreCap : 10)) return;
        }
        int points = tuning != null ? tuning.GetPoints(rating)
            : rating == ConductorGestureRating.Perfect ? 50
            : rating == ConductorGestureRating.Great ? 20
            : rating == ConductorGestureRating.Good ? 10 : 5;
        float duration = tuning != null ? tuning.GetDuration(rating)
            : rating == ConductorGestureRating.Perfect ? 5f
            : rating == ConductorGestureRating.Great ? 4.5f
            : rating == ConductorGestureRating.Good ? 4f : 3f;
        if (motion)
            points = Mathf.Min(points, (tuning != null ? tuning.motionScoreCap : 10) - motionTotal);
        if (points > 0)
        {
            scoreAwards.Add(new ScoreAward
            {
                points = points,
                expiresAt = now + Mathf.Max(0.01f, duration),
                rating = rating
            });
            if (motion) lastMotionAwardTime = now;
            RefreshLightScore(now);
        }
        LastRating = rating;
    }

    private void RefreshLightScore(double now)
    {
        long total = 0;
        for (int i = scoreAwards.Count - 1; i >= 0; i--)
        {
            if (now >= scoreAwards[i].expiresAt)
            {
                scoreAwards.RemoveAt(i);
                continue;
            }
            total += scoreAwards[i].points;
        }
        int current = (int)System.Math.Min(total, int.MaxValue);
        if (lightScore == current) return;
        lightScore = current;
        ApplyLightScore();
    }

    private void Update()
    {
        if (timeline != null && timeline.IsPaused)
        {
            SetScoringPaused(true);
            return;
        }
        SetScoringPaused(false);
        RefreshLightScore(AudioSettings.dspTime);
        double idle = AudioSettings.dspTime - lastProgressTime;
        feedback?.SetAssistance(Mathf.Clamp01((float)((idle - IdleGrace) / Mathf.Max(0.1f, IdleGrace))));
        if (!completed && !IsNearStaircase())
        {
            staircaseHits = 0;
            lastStaircaseBeat = -1;
        }
        if (!completed && pendingBeat >= 0 && AudioSettings.dspTime > pendingBeatDeadline)
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
        if (!IsGameplayActive || (timeline != null && timeline.IsPaused)) return;
        AddLightScore(ConductorGestureRating.Motion);
    }

    private void OnCue(int id) { feedback?.CueMeteor(); }
    private void OnFirstBeat(int id)
    {
        feedback?.StrikeMeteor();
        if (pendingBeat >= 0 && pendingBeat != id && !pendingBeatQualified && pendingBeatIsInRange
            && lastHitBeat != pendingBeat)
        {
            staircaseHits = 0;
            lastStaircaseBeat = -1;
        }
        pendingBeat = id;
        pendingBeatQualified = id == lastHitBeat;
        pendingBeatIsInRange = !completed && IsNearStaircase();
        pendingBeatDeadline = AudioSettings.dspTime + AssistWindow;
    }

    private void OnDownstroke(double dspTime)
    {
        if (!IsGameplayActive || (timeline != null && timeline.IsPaused)) return;
        if (timeline == null) return;

        float correctedTime = (float)(dspTime - (tuning != null ? tuning.inputLatencyCompensationSeconds : 0f));
        float assist = Mathf.Clamp01((float)((correctedTime - lastProgressTime - IdleGrace) / Mathf.Max(0.1f, IdleGrace)));
        float window = Mathf.Lerp(GoodWindow, AssistWindow, assist);
        bool hit = timeline.TryMatch(dspTime, window, out int beatId, out float offset);
        LastBeatOffsetSeconds = offset;
        if (hit && beatId != lastHitBeat)
        {
            float deviation = Mathf.Abs(offset);
            ConductorGestureRating rating = deviation <= PerfectWindow ? ConductorGestureRating.Perfect
                : deviation <= GreatWindow ? ConductorGestureRating.Great : ConductorGestureRating.Good;
            AddLightScore(rating);
            lastHitBeat = beatId;
            lastProgressTime = correctedTime;
            progress = Mathf.Min(PathSegments, progress + 1);
            feedback?.AdvancePath(progress, PathSegments);
            if (progress >= 2) timeline.UnlockLayer(1);
            if (progress >= 4) timeline.UnlockLayer(2);
            if (!completed && rating >= StairRating && IsNearStaircase())
            {
                staircaseHits = lastStaircaseBeat < 0 || beatId == lastStaircaseBeat + 1
                    ? Mathf.Min(StairHits, staircaseHits + 1) : 1;
                lastStaircaseBeat = beatId;
                if (pendingBeat == beatId) pendingBeatQualified = true;
            }
            else
            {
                staircaseHits = 0;
                lastStaircaseBeat = -1;
            }
            if (staircaseHits >= StairHits) Complete();
        }
        else if (!completed && !hit && (pendingBeat < 0 || dspTime > pendingBeatDeadline))
        {
            staircaseHits = 0;
            lastStaircaseBeat = -1;
        }
    }

    private bool IsNearStaircase()
    {
        if (player == null || staircase == null) return false;
        Vector3 delta = player.position - staircase.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= StairDistance * StairDistance;
    }

    private bool IsGameplayActive => gameMenu == null || gameMenu.IsGameplayActive;

    private void Complete()
    {
        completed = true;
        feedback?.Complete();
        staircase?.Raise();
    }
}
