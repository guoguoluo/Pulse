using System.Collections.Generic;
using UnityEngine;

/// <summary>Procedural placeholder art with persistent path progress and score-driven light.</summary>
public class ConductorWorldFeedback : MonoBehaviour
{
    [SerializeField] private ConductorTuning tuning;
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Light localLight;

    [Header("Score Mapping Fallback")]
    [Min(0f)] [SerializeField] private float fullScoreBrightness = 3.2f;

    [Header("Shared Light Envelope")]
    [Min(0.01f)] [SerializeField] private float brightenSeconds = 0.55f;
    [SerializeField] private AnimationCurve brightenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Visual Colors")]
    [SerializeField] private Color leafColor = new Color(0.68f, 1f, 0.77f);
    [SerializeField] private Color myceliumColor = new Color(0.46f, 0.87f, 1f);

    private readonly System.Collections.Generic.List<Renderer> path = new System.Collections.Generic.List<Renderer>();
    private readonly System.Collections.Generic.List<Transform> leaves = new System.Collections.Generic.List<Transform>();
    private readonly System.Collections.Generic.List<Renderer> mycelia = new System.Collections.Generic.List<Renderer>();
    private Light footLight;
    private float transitionStartTime;
    private float transitionStartStrength;
    private float targetStrength;
    private float currentStrength;
    private int unlockedPathCount;

    public float CurrentStrength => currentStrength;

    private Material NewMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader);
        material.color = new Color(color.r, color.g, color.b, 1f);
        return material;
    }

    private GameObject Primitive(string label, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = label;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        go.GetComponent<Renderer>().material = NewMaterial(color);
        return go;
    }

    private void Awake()
    {
        if (tuning == null) { var timeline = FindFirstObjectByType<FirstBeatTimeline>(); if (timeline != null) tuning = timeline.Tuning; }
        if (player == null) { var found = FindFirstObjectByType<PlayerController>(); if (found != null) player = found.transform; }
        if (player == null) return;

        for (int i = 0; i < 6; i++)
        {
            float z = 2f + i * 1.9f;
            float x = i % 2 == 0 ? -0.5f : 0.5f;
            Renderer tile = Primitive("Permanent light path", PrimitiveType.Cube, transform,
                new Vector3(x * 0.4f, 0.08f, z), new Vector3(1.1f, 0.035f, 0.65f), myceliumColor * 0.13f).GetComponent<Renderer>();
            path.Add(tile);
            Renderer filament = Primitive("Mycelium strand", PrimitiveType.Cube, transform,
                new Vector3(x, 0.12f, z), new Vector3(0.025f, 0.025f, 1.1f), myceliumColor * 0.12f).GetComponent<Renderer>();
            mycelia.Add(filament);
        }
        for (int i = 0; i < 12; i++)
        {
            float z = 2f + (i % 6) * 1.9f;
            float x = i % 2 == 0 ? -1.7f : 1.7f;
            Transform leaf = Primitive("Wind leaf", PrimitiveType.Cube, transform,
                new Vector3(x, 0.8f + (i % 3) * 0.2f, z), new Vector3(0.09f, 0.02f, 0.22f), leafColor * 0.22f).transform;
            leaves.Add(leaf);
        }
        footLight = player.gameObject.AddComponent<Light>();
        footLight.type = LightType.Point;
        footLight.range = 3.2f;
        footLight.color = myceliumColor;
        footLight.shadows = LightShadows.None;
        footLight.intensity = 0f;
        if (localLight != null) localLight.enabled = false;
    }

    private void Update()
    {
        float duration = tuning != null ? tuning.brightenSeconds : brightenSeconds;
        float progress = Mathf.Clamp01((Time.time - transitionStartTime) / Mathf.Max(0.01f, duration));
        AnimationCurve curve = tuning != null ? tuning.brightenCurve : brightenCurve;
        currentStrength = Mathf.Lerp(transitionStartStrength, targetStrength,
            curve != null ? curve.Evaluate(progress) : progress);
        if (footLight != null) footLight.intensity = currentStrength * 0.22f;
        float fullBrightness = tuning != null ? tuning.fullScoreBrightness : fullScoreBrightness;
        float pathGlow = Mathf.Clamp01(currentStrength / Mathf.Max(0.01f, fullBrightness));
        for (int i = 0; i < path.Count; i++)
        {
            float power = i < unlockedPathCount ? pathGlow : 0.13f * pathGlow;
            path[i].material.color = new Color(myceliumColor.r * power,
                myceliumColor.g * power, myceliumColor.b * power, 1f);
        }
        for (int i = 0; i < mycelia.Count; i++)
        {
            float power = currentStrength * 0.28f;
            mycelia[i].material.color = new Color(myceliumColor.r * power, myceliumColor.g * power, myceliumColor.b * power, 1f);
        }
        for (int i = 0; i < leaves.Count; i++)
        {
            leaves[i].localPosition += Vector3.right * (Mathf.Sin(Time.time * 2f + i) * 0.003f);
            float power = currentStrength * 0.28f;
            leaves[i].GetComponent<Renderer>().material.color = new Color(leafColor.r * power, leafColor.g * power, leafColor.b * power, 1f);
        }
    }

    public void CueMeteor() { }
    public void StrikeMeteor() { }
    public void SetAssistance(float value) { }

    public void SetLightScore(int score, int maximumScore)
    {
        float nextTarget = (tuning != null ? tuning.LightFraction(score, maximumScore)
            : Mathf.Clamp01((float)score / Mathf.Max(1, maximumScore))) *
            (tuning != null ? tuning.fullScoreBrightness : fullScoreBrightness);
        if (Mathf.Approximately(targetStrength, nextTarget)) return;
        transitionStartStrength = currentStrength;
        targetStrength = nextTarget;
        transitionStartTime = Time.time;
    }

    public void AdvancePath(int progress, int total)
    {
        unlockedPathCount = Mathf.Max(unlockedPathCount,
            Mathf.CeilToInt((float)progress / Mathf.Max(1, total) * path.Count));
    }

    public void Complete() { }
}
