using UnityEngine;

/// <summary>Score-driven plant emission and local lights; plant geometry remains in the scene when dark.</summary>
public class ConductorLuminousGarden : MonoBehaviour
{
    [SerializeField] private ConductorWorldFeedback feedback;
    [SerializeField] private ConductorTuning tuning;
    [SerializeField] private Renderer[] petals;
    [SerializeField] private Renderer[] hearts;
    [SerializeField] private Renderer[] foliage;
    [SerializeField] private Light[] bloomLights;
    [Header("Light Response")]
    [Min(0f)] [SerializeField] private float petalEmission = 2.4f;
    [Min(0f)] [SerializeField] private float heartEmission = 3.1f;
    [Min(0f)] [SerializeField] private float foliageEmission = 0.24f;
    [Min(0f)] [SerializeField] private float pointLightIntensity = 2.1f;
    [SerializeField] private Color petalGlow = new Color(1f, 0.35f, 0.68f);
    [SerializeField] private Color heartGlow = new Color(1f, 0.58f, 0.28f);
    [SerializeField] private Color foliageGlow = new Color(0.18f, 0.62f, 0.62f);

    private MaterialPropertyBlock block;

    private void Awake()
    {
        if (feedback == null) feedback = FindFirstObjectByType<ConductorWorldFeedback>();
        if (tuning == null)
        {
            var timeline = FindFirstObjectByType<FirstBeatTimeline>();
            if (timeline != null) tuning = timeline.Tuning;
        }
        block = new MaterialPropertyBlock();
        UpdateGlow(0f);
    }

    private void LateUpdate()
    {
        float full = tuning != null ? tuning.fullScoreBrightness : 3.2f;
        float normalized = feedback != null ? Mathf.Clamp01(feedback.CurrentStrength / Mathf.Max(0.01f, full)) : 0f;
        UpdateGlow(normalized);
    }

    private void UpdateGlow(float strength)
    {
        SetEmission(petals, petalGlow * (petalEmission * strength));
        SetEmission(hearts, heartGlow * (heartEmission * strength));
        SetEmission(foliage, foliageGlow * (foliageEmission * strength));
        if (bloomLights == null) return;
        foreach (var light in bloomLights)
            if (light != null) light.intensity = pointLightIntensity * strength;
    }

    private void SetEmission(Renderer[] renderers, Color color)
    {
        if (renderers == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", color);
            renderer.SetPropertyBlock(block);
        }
    }
}
