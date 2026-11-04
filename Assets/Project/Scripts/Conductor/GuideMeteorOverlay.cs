using UnityEngine;

/// <summary>Camera-space first-beat cue drawn after the world blackout.</summary>
public class GuideMeteorOverlay : MonoBehaviour
{
    [Header("Screen Position (0-1)")]
    [Range(0.1f, 0.9f)] [SerializeField] private float centerX = 0.53f;
    [Range(0.05f, 0.5f)] [SerializeField] private float centerY = 0.23f;
    [Min(3f)] [SerializeField] private float coreSizePixels = 22f;
    [Min(0f)] [SerializeField] private float trailLengthPixels = 86f;
    [Min(0.1f)] [SerializeField] private float cueLeadSeconds = 0.65f;
    [Tooltip("Strike duration at the reference BPM. Actual duration scales with the current bar length.")]
    [Min(0.1f)] [SerializeField] private float streakSeconds = 0.46f;
    [Min(1f)] [SerializeField] private float referenceBeatsPerMinute = 60f;
    [Range(0.05f, 1f)] [SerializeField] private float trailHistoryFraction = 0.3f;
    [Header("Animated Path")]
    [SerializeField] private Animator pathAnimator;
    [Tooltip("Local animation units to normalized screen coordinates. Edit Cue/Strike clips to shape the path.")]
    [SerializeField] private Vector2 pathScreenScale = Vector2.one;

    [Header("Visibility")]
    [SerializeField] private Color idleColor = new Color(0.75f, 0.91f, 1f, 1f);
    [SerializeField] private Color cueColor = new Color(1f, 1f, 1f, 1f);
    [Range(0f, 1f)] [SerializeField] private float idleScale = 0.9f;
    [Min(1f)] [SerializeField] private float cueScale = 1.7f;

    private Texture2D whitePixel;
    private FirstBeatTimeline timeline;
    private float cueTime = -100f;
    private float strikeTime = -100f;
    private float activeStreakSeconds = 0.46f;
    private float cueClipSeconds = 0.65f;
    private float strikeClipSeconds = 0.46f;
    private readonly Vector2[] trailPositions = new Vector2[32];
    private readonly float[] trailTimes = new float[32];
    private int trailCount;
    private int trailNext;
    private bool paused;
    private float pausedAt;
    private float playingPathSpeed = 1f;

    public Vector2 NormalizedPosition => new Vector2(centerX, centerY);
    public void SetPaused(bool value)
    {
        if (value == paused) return;
        if (value)
        {
            pausedAt = Time.unscaledTime;
            paused = true;
            if (pathAnimator != null) pathAnimator.speed = 0f;
        }
        else
        {
            float elapsed = Time.unscaledTime - pausedAt;
            cueTime += elapsed;
            strikeTime += elapsed;
            for (int i = 0; i < trailCount; i++) trailTimes[i] += elapsed;
            paused = false;
            if (pathAnimator != null) pathAnimator.speed = playingPathSpeed;
        }
    }

    private void Awake()
    {
        whitePixel = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        whitePixel.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float dx = (x + 0.5f - 16f) / 16f;
                float dy = (y + 0.5f - 16f) / 16f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1f - Mathf.SmoothStep(0.7f, 1f, distance);
                whitePixel.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        whitePixel.Apply();
        timeline = FindFirstObjectByType<FirstBeatTimeline>();
        if (timeline != null) cueLeadSeconds = timeline.CueLeadSeconds;
        if (pathAnimator != null && pathAnimator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in pathAnimator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == "GuideMeteor_Cue") cueClipSeconds = clip.length;
                if (clip.name == "GuideMeteor_Strike") strikeClipSeconds = clip.length;
            }
        }
    }

    private void OnEnable()
    {
        if (timeline == null) timeline = FindFirstObjectByType<FirstBeatTimeline>();
        if (timeline != null)
        {
            timeline.Cue += OnCue;
            timeline.FirstBeat += OnFirstBeat;
        }
    }

    private void OnDisable()
    {
        if (timeline != null)
        {
            timeline.Cue -= OnCue;
            timeline.FirstBeat -= OnFirstBeat;
        }
    }

    private void OnCue(int id)
    {
        cueTime = Time.unscaledTime;
        PlayPath("Cue", cueLeadSeconds);
    }

    private void OnFirstBeat(int id)
    {
        strikeTime = Time.unscaledTime;
        float referenceBar = 4f * 60f / referenceBeatsPerMinute;
        float currentBar = timeline != null ? timeline.GetBarSeconds(id) : referenceBar;
        activeStreakSeconds = timeline != null
            ? Mathf.Clamp(streakSeconds * currentBar / referenceBar, 0.05f, Mathf.Max(0.05f, currentBar - 0.01f))
            : streakSeconds;
        trailCount = 0;
        trailNext = 0;
        PlayPath("Strike", activeStreakSeconds);
    }

    private void PlayPath(string state, float duration)
    {
        if (pathAnimator == null) return;
        pathAnimator.gameObject.SetActive(true);
        playingPathSpeed = (state == "Cue" ? cueClipSeconds : strikeClipSeconds) / duration;
        pathAnimator.speed = playingPathSpeed;
        pathAnimator.Play(state, 0, 0f);
        pathAnimator.Update(0f);
    }

    private void LateUpdate()
    {
        if (paused || pathAnimator == null || Time.unscaledTime - strikeTime >= activeStreakSeconds) return;
        Vector3 local = pathAnimator.transform.localPosition;
        trailPositions[trailNext] = new Vector2(
            Screen.width * (centerX + local.x * pathScreenScale.x),
            Screen.height * (centerY - local.y * pathScreenScale.y));
        trailTimes[trailNext] = Time.unscaledTime;
        trailNext = (trailNext + 1) % trailPositions.Length;
        trailCount = Mathf.Min(trailCount + 1, trailPositions.Length);
    }

    private void OnGUI()
    {
        if (whitePixel == null || Event.current.type != EventType.Repaint || timeline == null) return;
        float now = paused ? pausedAt : Time.unscaledTime;
        float preBeat = Mathf.Clamp01((now - cueTime) / cueLeadSeconds);
        float streak = Mathf.Clamp01((now - strikeTime) / activeStreakSeconds);
        bool charging = preBeat < 1f;
        bool moving = streak < 1f;
        if (!charging && !moving) return;

        float scale = moving ? cueScale : Mathf.Lerp(idleScale, cueScale, preBeat);
        Color tint = Color.Lerp(idleColor, cueColor, moving ? 1f - streak * 0.45f : preBeat);
        float pixels = coreSizePixels * scale;
        Vector3 path = pathAnimator != null ? pathAnimator.transform.localPosition : Vector3.zero;
        float x = Screen.width * (centerX + path.x * pathScreenScale.x);
        float y = Screen.height * (centerY - path.y * pathScreenScale.y);
        if (pathAnimator == null)
        {
            x += moving ? streak * trailLengthPixels * 0.3f : 0f;
            y += moving ? streak * trailLengthPixels : 0f;
        }

        GUI.depth = -1000;
        DrawDisc(x, y, pixels * 2.2f, new Color(tint.r, tint.g, tint.b, tint.a * 0.34f));
        DrawDisc(x, y, pixels, tint);
        if (moving)
        {
            if (pathAnimator == null)
            {
                for (int i = 1; i <= 8; i++)
                {
                    float tail = i / 8f;
                    DrawDisc(x - tail * trailLengthPixels * 0.3f, y - tail * trailLengthPixels,
                        Mathf.Max(2f, pixels * (1f - tail) * 0.65f),
                        new Color(tint.r, tint.g, tint.b, tint.a * (1f - tail) * (1f - streak)));
                }
            }
            else
            {
                float historySeconds = activeStreakSeconds * trailHistoryFraction;
                for (int i = 1; i < trailCount; i++)
                {
                    int index = (trailNext - 1 - i + trailPositions.Length) % trailPositions.Length;
                    float age = now - trailTimes[index];
                    if (age > historySeconds) break;
                    float tail = age / historySeconds;
                    DrawDisc(trailPositions[index].x, trailPositions[index].y,
                        Mathf.Max(2f, pixels * (1f - tail) * 0.65f),
                        new Color(tint.r, tint.g, tint.b, tint.a * (1f - tail) * (1f - streak)));
                }
            }
        }
    }

    private void DrawDisc(float x, float y, float size, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), whitePixel);
        GUI.color = Color.white;
    }

    private void OnDestroy()
    {
        if (whitePixel != null) Destroy(whitePixel);
    }
}
