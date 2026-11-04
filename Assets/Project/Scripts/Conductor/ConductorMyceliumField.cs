using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ConductorMyceliumField : MonoBehaviour
{
    [SerializeField] private ConductorWorldFeedback feedback;
    [SerializeField] private ConductorBlackout blackout;
    [SerializeField] private Transform player;
    [SerializeField] private Renderer[] glowRenderers;
    [SerializeField] private Light[] clusterLights;
    [Header("Response")]
    [Min(0f)] [SerializeField] private float glowPerLightStrength = 0.48f;
    [Min(0f)] [SerializeField] private float glowRange = 11f;
    [Min(0f)] [SerializeField] private float idleGlow = 0f;
    [Min(0f)] [SerializeField] private float clusterLightPerStrength = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float ambientGlowAtFullVisibility = 0.7f;
    [Min(0f)] [SerializeField] private float clusterLightPerVisibility = 0.8f;
    [Min(1f)] [SerializeField] private float softLightRange = 14f;
    [Min(0f)] [SerializeField] private float softLightHeight = 4f;

    private MaterialPropertyBlock properties;
    private readonly List<Vector3> positions = new List<Vector3>();

    private void Awake()
    {
        if (feedback == null) feedback = FindFirstObjectByType<ConductorWorldFeedback>();
        if (blackout == null) blackout = FindFirstObjectByType<ConductorBlackout>();
        if (player == null)
        {
            var controller = FindFirstObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }
        if (glowRenderers == null || glowRenderers.Length == 0) glowRenderers = GetComponentsInChildren<Renderer>(true);
        properties = new MaterialPropertyBlock();
        foreach (var renderer in glowRenderers) positions.Add(renderer != null ? renderer.bounds.center : Vector3.zero);
        if (clusterLights != null)
            foreach (var light in clusterLights)
            {
                if (light == null) continue;
                light.range = Mathf.Max(light.range, softLightRange);
                light.transform.position += Vector3.up * softLightHeight;
            }
    }

    private void LateUpdate()
    {
        float strength = feedback != null ? feedback.CurrentStrength : 0f;
        float visibility = blackout != null ? blackout.CurrentVisibility : 0f;
        for (int i = 0; i < glowRenderers.Length; i++)
        {
            var renderer = glowRenderers[i];
            if (renderer == null) continue;
            float distance = player != null ? Vector3.Distance(player.position, positions[i]) : 0f;
            float proximity = glowRange > 0f ? 1f - Mathf.SmoothStep(0f, 1f, distance / glowRange) : 1f;
            renderer.GetPropertyBlock(properties);
            properties.SetFloat("_Glow", idleGlow +
                (strength * glowPerLightStrength + visibility * ambientGlowAtFullVisibility) * proximity);
            renderer.SetPropertyBlock(properties);
        }
        if (clusterLights == null) return;
        foreach (var light in clusterLights)
        {
            if (light == null) continue;
            float distance = player != null ? Vector3.Distance(player.position, light.transform.position) : 0f;
            float proximity = glowRange > 0f ? 1f - Mathf.SmoothStep(0f, 1f, distance / glowRange) : 1f;
            light.intensity = (strength * clusterLightPerStrength +
                visibility * clusterLightPerVisibility) * proximity * 0.35f;
        }
    }
}
