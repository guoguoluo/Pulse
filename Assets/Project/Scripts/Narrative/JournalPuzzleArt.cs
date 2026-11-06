using UnityEngine;

public class JournalPuzzleArt : MonoBehaviour
{
    [System.Serializable]
    public class Piece
    {
        public Transform transform;
        public Renderer renderer;
        public Vector3 hiddenScale = Vector3.one;
        public Vector3 shownScale = Vector3.one;
        public float swayDegrees;
        public float rise;
    }

    [SerializeField] private Piece[] pieces;
    [SerializeField] private Light accentLight;
    [SerializeField] private Color glowColor = new Color(0.2f, 0.75f, 1f);
    [SerializeField] private float glowStrength = 1.8f;
    [SerializeField] private float revealSpeed = 2.2f;
    [SerializeField] private float pulseDecay = 1.4f;
    [SerializeField] private ConductorDemo conductor;
    [SerializeField] private ConductorTuning tuning;
    private float progress;
    private float target;
    private float pulse;
    private MaterialPropertyBlock properties;
    private Vector3[] positions;
    private Quaternion[] rotations;

    private void Awake()
    {
        if (conductor == null) conductor = FindFirstObjectByType<ConductorDemo>();
        if (tuning == null)
        {
            var timeline = FindFirstObjectByType<FirstBeatTimeline>();
            if (timeline != null) tuning = timeline.Tuning;
        }
        properties = new MaterialPropertyBlock();
        positions = new Vector3[pieces.Length];
        rotations = new Quaternion[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].transform == null) continue;
            positions[i] = pieces[i].transform.localPosition;
            rotations[i] = pieces[i].transform.localRotation;
            pieces[i].transform.localScale = pieces[i].hiddenScale;
        }
    }

    public void SetProgress(int hits, int required)
    {
        target = Mathf.Clamp01((float)hits / Mathf.Max(1, required));
        pulse = 1f;
    }

    public void SetComplete()
    {
        target = 1f;
        progress = 1f;
    }

    private void Update()
    {
        if (properties == null) properties = new MaterialPropertyBlock();
        if (pieces == null || positions == null || rotations == null ||
            positions.Length != pieces.Length || rotations.Length != pieces.Length)
            return;
        float visibility = conductor != null
            ? Mathf.Clamp01((float)conductor.LightScore / Mathf.Max(1, tuning != null ? tuning.maximumLightScore : 100))
            : 0f;
        progress = Mathf.MoveTowards(progress, target, Time.deltaTime * revealSpeed);
        pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * pulseDecay);
        for (int i = 0; i < pieces.Length; i++)
        {
            var piece = pieces[i];
            if (piece.transform == null) continue;
            float threshold = (float)i / Mathf.Max(1, pieces.Length);
            float local = Mathf.Clamp01((progress - threshold) * pieces.Length);
            local = local * local * (3f - 2f * local);
            piece.transform.localScale = Vector3.Lerp(piece.hiddenScale, piece.shownScale, local);
            piece.transform.localPosition = positions[i] + Vector3.up * piece.rise * local;
            piece.transform.localRotation = rotations[i] *
                Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.5f + i * 1.8f) * piece.swayDegrees * local);
            if (piece.renderer == null) continue;
            piece.renderer.GetPropertyBlock(properties);
            properties.SetColor("_EmissionColor", glowColor *
                ((0.08f + local * glowStrength + pulse * 0.35f) * visibility));
            piece.renderer.SetPropertyBlock(properties);
        }
        if (accentLight != null)
            accentLight.intensity = (progress * 0.55f + pulse * 0.16f) * visibility;
    }
}
