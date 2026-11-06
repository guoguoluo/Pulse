using UnityEngine;

[CreateAssetMenu(menuName = "Pulse/Conductor Tuning", fileName = "ConductorTuning")]
public class ConductorTuning : ScriptableObject
{
    public enum MusicMode { Timeline, GestureDriven }

    [Header("Recording and First Beats")]
    [Tooltip("Use authored firstBeatSeconds only after marking first beats against this exact recording.")]
    public bool recordingBeatMarkersVerified;
    [Range(30f, 180f)] public float previewBeatsPerMinute = 60f;
    [Range(1, 12)] public int beatsPerBar = 4;
    [Min(0f)] public float previewFirstBeatOffsetSeconds;
    [Tooltip("First beats in seconds from the beginning of the MP3. Strictly increasing.")]
    public float[] firstBeatSeconds = new float[0];
    [Range(-0.5f, 0.5f)] public float judgementOffsetSeconds;
    [Tooltip("Additional delay from mouse motion to a detected beat action; positive values score it earlier.")]
    [Range(0f, 0.3f)] public float inputLatencyCompensationSeconds = 0.08f;
    [Min(0.05f)] public float meteorCueLeadSeconds = 0.65f;
    [Min(0.05f)] public float meteorStrikeSeconds = 0.46f;
    [Tooltip("Fraction of the strike animation elapsed at the judged first beat (0=start, 1=end).")]
    [Range(0f, 1f)] public float meteorBeatPhase = 0.5f;

    [Header("Judgement")]
    [Min(0f)] public float perfectWindowSeconds = 0.08f;
    [Min(0f)] public float greatWindowSeconds = 0.17f;
    [Min(0f)] public float goodWindowSeconds = 0.3f;
    [Min(0f)] public float idleGraceSeconds = 12f;
    [Min(0f)] public float maxAssistWindowSeconds = 0.48f;
    [Min(1)] public int pathSegments = 6;

    [Header("Mouse Beat Motion (any direction)")]
    [Tooltip("Higher values make movement speed respond faster.")]
    [Min(0.1f)] public float motionSmoothing = 12f;
    [Tooltip("Filtered mouse speed in pixels per second required for a scored beat action.")]
    [Min(0f)] public float beatMotionTriggerSpeed = 260f;
    [Tooltip("Speed below which the next distinct action can be armed.")]
    [Min(0f)] public float beatMotionResetSpeed = 95f;
    [Min(0f)] public float beatMotionCooldownSeconds = 0.38f;
    [Tooltip("Total mouse travel within a short burst required for a scored beat action.")]
    [Min(0f)] public float beatMotionMinimumPixels = 18f;
    [Tooltip("Minimum mouse travel per frame for a faint Motion response.")]
    [Min(0f)] public float motionThresholdPixels = 1.5f;
    [Min(0f)] public float motionFeedbackCooldownSeconds = 0.12f;

    [Header("Temporary Score (100 = full light and volume)")]
    [Tooltip("The visible light and audible volume cap here; stored points may exceed it.")]
    [Min(1)] public int maximumLightScore = 100;
    [Min(0)] public int motionPoints = 5;
    [Tooltip("Maximum active points contributed by Motion; beat ratings are unaffected.")]
    [Min(0)] public int motionScoreCap = 10;
    [Tooltip("Minimum time between Motion score awards.")]
    [Min(0.01f)] public float motionAwardIntervalSeconds = 0.45f;
    [Min(0)] public int goodPoints = 10;
    [Min(0)] public int greatPoints = 20;
    [Min(0)] public int perfectPoints = 50;
    [Tooltip("Each award expires independently after its rating's duration.")]
    [Min(0.01f)] public float motionDurationSeconds = 3f;
    [Min(0.01f)] public float goodDurationSeconds = 4f;
    [Min(0.01f)] public float greatDurationSeconds = 4.5f;
    [Min(0.01f)] public float perfectDurationSeconds = 5f;
    [Tooltip("At 100 points, 0 means no fullscreen blackout and 1 means fully black.")]
    [Range(0f, 1f)] public float fullScoreDarkness = 0f;
    [Min(0f)] public float fullScoreBrightness = 3.2f;
    [Tooltip("Maps score fraction to visual light fraction only. Music volume remains independent.")]
    public AnimationCurve lightScoreCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.6f, 0.28f), new Keyframe(1f, 1f));

    public float LightFraction(int score, int maximumScore)
    {
        float normalized = Mathf.Clamp01((float)score / Mathf.Max(1, maximumScore));
        return Mathf.Clamp01(lightScoreCurve != null ? lightScoreCurve.Evaluate(normalized) : normalized);
    }

    [Header("Shared Light Envelope")]
    [Range(0f, 1f)] public float initialDarkness = 1f;
    [Min(0.01f)] public float brightenSeconds = 0.55f;
    public AnimationCurve brightenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Plant Emission and Point Light")]
    [Min(0f)] public float glowPerLightStrength = 0.82f;
    [Min(0f)] public float clusterLightPerStrength = 0.42f;
    [Range(0f, 1f)] public float ambientGlowAtFullVisibility = 0.7f;
    [Min(0f)] public float clusterLightPerVisibility = 0.8f;

    [Header("Music")]
    public MusicMode musicMode = MusicMode.Timeline;
    [Tooltip("In Timeline mode, 0 score is silent and Maximum Light Score reaches Recording Volume.")]
    [Range(0f, 1f)] public float recordingVolume = 1f;
    [Tooltip("Seconds to move from the current volume to the new score target.")]
    [Min(0.01f)] public float scoreVolumeTransitionSeconds = 0.8f;
    [Tooltip("Progress of each volume transition, from 0 to 1.")]
    public AnimationCurve scoreVolumeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Header("Gesture-Driven Music (experimental, not beat-synchronized)")]
    [Min(0f)] public float musicFadeInSeconds = 0.35f;
    [Min(0f)] public float musicFadeOutSeconds = 0.55f;
    [Min(0.1f)] public float gestureReferenceBeatInterval = 0.7f;
    [Min(0.05f)] public float minimumBeatInterval = 0.25f;
    [Range(0.1f, 3f)] public float minimumPlaybackSpeed = 0.5f;
    [Range(0.1f, 3f)] public float maximumPlaybackSpeed = 1.6f;
    [Min(0.1f)] public float gestureStopAfterSeconds = 1.2f;
    [Min(0f)] public float speedSmoothing = 8f;

    [Header("Staircase")]
    [Min(0f)] public float staircaseTriggerDistance = 50f;
    public ConductorGestureRating staircaseMinimumRating = ConductorGestureRating.Good;
    [Min(1)] public int staircaseRequiredHits = 3;

    [System.Serializable]
    public class FragmentPuzzle
    {
        [Range(2, 6)] public int chapter = 2;
        public JournalFragmentSite.PuzzleMode mode = JournalFragmentSite.PuzzleMode.Accumulate;
        public bool requireBeatPuzzle = true;
        [Min(0f)] public float interactionDistance = 5f;
        [Min(1)] public int requiredHits = 3;
        public ConductorGestureRating minimumRating = ConductorGestureRating.Good;
    }

    [Header("Journal Fragment Puzzles (Chapters 2-6)")]
    [Tooltip("One entry per journal chapter. Completion triggers that site's Animator Raise parameter.")]
    public FragmentPuzzle[] fragmentPuzzles = new FragmentPuzzle[0];

    public FragmentPuzzle GetFragmentPuzzle(int chapterIndex)
    {
        if (fragmentPuzzles == null) return null;
        for (int i = 0; i < fragmentPuzzles.Length; i++)
            if (fragmentPuzzles[i] != null && fragmentPuzzles[i].chapter == chapterIndex + 1)
                return fragmentPuzzles[i];
        return null;
    }

    public int GetPoints(ConductorGestureRating rating)
    {
        switch (rating)
        {
            case ConductorGestureRating.Perfect: return perfectPoints;
            case ConductorGestureRating.Great: return greatPoints;
            case ConductorGestureRating.Good: return goodPoints;
            default: return motionPoints;
        }
    }

    public float GetDuration(ConductorGestureRating rating)
    {
        switch (rating)
        {
            case ConductorGestureRating.Perfect: return perfectDurationSeconds;
            case ConductorGestureRating.Great: return greatDurationSeconds;
            case ConductorGestureRating.Good: return goodDurationSeconds;
            default: return motionDurationSeconds;
        }
    }

    private void OnValidate()
    {
        greatWindowSeconds = Mathf.Max(greatWindowSeconds, perfectWindowSeconds);
        goodWindowSeconds = Mathf.Max(goodWindowSeconds, greatWindowSeconds);
        maxAssistWindowSeconds = Mathf.Max(maxAssistWindowSeconds, goodWindowSeconds);
        maximumLightScore = Mathf.Max(1, maximumLightScore);
        motionScoreCap = Mathf.Max(0, motionScoreCap);
        motionAwardIntervalSeconds = Mathf.Max(0.01f, motionAwardIntervalSeconds);
        staircaseRequiredHits = Mathf.Max(1, staircaseRequiredHits);
        if (staircaseMinimumRating < ConductorGestureRating.Good)
            staircaseMinimumRating = ConductorGestureRating.Good;
        if (fragmentPuzzles == null) return;
        bool[] seenChapters = new bool[7];
        foreach (var puzzle in fragmentPuzzles)
        {
            if (puzzle == null) continue;
            puzzle.chapter = Mathf.Clamp(puzzle.chapter, 2, 6);
            if (seenChapters[puzzle.chapter])
                Debug.LogWarning($"Duplicate fragment puzzle chapter {puzzle.chapter} in {name}.", this);
            seenChapters[puzzle.chapter] = true;
            puzzle.requiredHits = Mathf.Max(1, puzzle.requiredHits);
            puzzle.interactionDistance = Mathf.Max(0f, puzzle.interactionDistance);
            if (puzzle.minimumRating < ConductorGestureRating.Good)
                puzzle.minimumRating = ConductorGestureRating.Good;
        }
        for (int chapter = 2; chapter <= 6; chapter++)
            if (!seenChapters[chapter])
                Debug.LogWarning($"Fragment puzzle chapter {chapter} is missing from {name}.", this);
    }
}
