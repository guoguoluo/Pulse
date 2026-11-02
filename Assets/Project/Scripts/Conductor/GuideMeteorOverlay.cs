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
    [Min(0.1f)] [SerializeField] private float streakSeconds = 0.46f;

    [Header("Visibility")]
    [SerializeField] private Color idleColor = new Color(0.75f, 0.91f, 1f, 1f);
    [SerializeField] private Color cueColor = new Color(1f, 1f, 1f, 1f);
    [Range(0f, 1f)] [SerializeField] private float idleScale = 0.9f;
    [Min(1f)] [SerializeField] private float cueScale = 1.7f;

    private Texture2D whitePixel;
    private FirstBeatTimeline timeline;
    private float cueTime = -100f;
    private float strikeTime = -100f;

    public Vector2 NormalizedPosition => new Vector2(centerX, centerY);

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

    private void OnCue(int id) { cueTime = Time.unscaledTime; }
    private void OnFirstBeat(int id) { strikeTime = Time.unscaledTime; }

private void OnGUI()
    {
        if (whitePixel == null || Event.current.type != EventType.Repaint || timeline == null) return;
        float preBeat = Mathf.Clamp01((Time.unscaledTime - cueTime) / cueLeadSeconds);
        float streak = Mathf.Clamp01((Time.unscaledTime - strikeTime) / streakSeconds);
        bool charging = preBeat < 1f;
        bool moving = streak < 1f;
        if (!charging && !moving) return;

        float scale = moving ? cueScale : Mathf.Lerp(idleScale, cueScale, preBeat);
        Color tint = Color.Lerp(idleColor, cueColor, moving ? 1f - streak * 0.45f : preBeat);
        float pixels = coreSizePixels * scale;
        float x = Screen.width * centerX + (moving ? streak * trailLengthPixels * 0.3f : 0f);
        float y = Screen.height * centerY + (moving ? streak * trailLengthPixels : 0f);

        GUI.depth = -1000;
        DrawDisc(x, y, pixels * 2.2f, new Color(tint.r, tint.g, tint.b, tint.a * 0.34f));
        DrawDisc(x, y, pixels, tint);
        if (moving)
        {
            for (int i = 1; i <= 8; i++)
            {
                float tail = i / 8f;
                float alpha = tint.a * (1f - tail) * (1f - streak);
                DrawDisc(x - tail * trailLengthPixels * 0.3f, y - tail * trailLengthPixels,
                    Mathf.Max(2f, pixels * (1f - tail) * 0.65f),
                    new Color(tint.r, tint.g, tint.b, alpha));
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
