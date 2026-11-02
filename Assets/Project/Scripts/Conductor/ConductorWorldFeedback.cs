using System.Collections.Generic;
using UnityEngine;

/// <summary>Procedural placeholder art: one guide meteor, transient leaves/mycelium and permanent path.</summary>
public class ConductorWorldFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Light localLight;

    [Header("Motion - Faint Response")]
    [Min(0f)] [SerializeField] private float motionBrightness = 0.35f;
    [Min(0f)] [SerializeField] private float motionHoldSeconds = 0.08f;

    [Header("Off Beat")]
    [Min(0f)] [SerializeField] private float offBeatBrightness = 0.8f;
    [Min(0f)] [SerializeField] private float offBeatHoldSeconds = 0.35f;

    [Header("Good")]
    [Min(0f)] [SerializeField] private float goodBrightness = 1.5f;
    [Min(0f)] [SerializeField] private float goodHoldSeconds = 0.65f;

    [Header("Great")]
    [Min(0f)] [SerializeField] private float greatBrightness = 2.3f;
    [Min(0f)] [SerializeField] private float greatHoldSeconds = 0.9f;

    [Header("Perfect")]
    [Min(0f)] [SerializeField] private float perfectBrightness = 3.2f;
    [Min(0f)] [SerializeField] private float perfectHoldSeconds = 1.2f;

    [Header("Shared Light Envelope")]
    [Min(0.01f)] [SerializeField] private float brightenSeconds = 0.55f;
    [SerializeField] private AnimationCurve brightenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Min(0.01f)] [SerializeField] private float fadeBackSeconds = 2.5f;
    [Min(0f)] [SerializeField] private float restingBrightness = 0.03f;

    [Header("Visual Colors")]
    [SerializeField] private Color leafColor = new Color(0.68f, 1f, 0.77f);
    [SerializeField] private Color myceliumColor = new Color(0.46f, 0.87f, 1f);

    private readonly System.Collections.Generic.List<Renderer> path = new System.Collections.Generic.List<Renderer>();
    private readonly System.Collections.Generic.List<Transform> leaves = new System.Collections.Generic.List<Transform>();
    private readonly System.Collections.Generic.List<Renderer> mycelia = new System.Collections.Generic.List<Renderer>();
    private Light footLight;
    private float eventStartTime = -100f;
    private float eventStartStrength;
    private float eventTargetStrength;
    private float eventHoldSeconds;
    private float currentStrength;

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
        footLight.range = 5f;
        footLight.color = myceliumColor;
        footLight.shadows = LightShadows.None;
        footLight.intensity = restingBrightness;
        if (localLight != null) localLight.enabled = false;
    }

    private float EvaluateStrength(float now)
    {
        float elapsed = now - eventStartTime;
        if (elapsed < 0f) return 0f;
        float envelope = ConductorLightEnvelope.Evaluate(elapsed, brightenSeconds,
            eventHoldSeconds, fadeBackSeconds, brightenCurve);
        if (elapsed < brightenSeconds)
            return Mathf.Lerp(eventStartStrength, eventTargetStrength, envelope);
        return eventTargetStrength * envelope;
    }

    private void Update()
    {
        currentStrength = EvaluateStrength(Time.time);
        if (footLight != null) footLight.intensity = restingBrightness + currentStrength;
        for (int i = 0; i < mycelia.Count; i++)
        {
            float power = 0.27f + currentStrength * 0.28f;
            mycelia[i].material.color = new Color(myceliumColor.r * power, myceliumColor.g * power, myceliumColor.b * power, 1f);
        }
        for (int i = 0; i < leaves.Count; i++)
        {
            leaves[i].localPosition += Vector3.right * (Mathf.Sin(Time.time * 2f + i) * 0.003f);
            float power = 0.27f + currentStrength * 0.28f;
            leaves[i].GetComponent<Renderer>().material.color = new Color(leafColor.r * power, leafColor.g * power, leafColor.b * power, 1f);
        }
    }

    public void CueMeteor() { }
    public void StrikeMeteor() { }
    public void SetAssistance(float value) { }

    public void ShowRating(ConductorGestureRating rating)
    {
        float brightness = rating == ConductorGestureRating.Perfect ? perfectBrightness
            : rating == ConductorGestureRating.Great ? greatBrightness
            : rating == ConductorGestureRating.Good ? goodBrightness
            : rating == ConductorGestureRating.OffBeat ? offBeatBrightness : motionBrightness;
        float hold = rating == ConductorGestureRating.Perfect ? perfectHoldSeconds
            : rating == ConductorGestureRating.Great ? greatHoldSeconds
            : rating == ConductorGestureRating.Good ? goodHoldSeconds
            : rating == ConductorGestureRating.OffBeat ? offBeatHoldSeconds : motionHoldSeconds;
        float active = EvaluateStrength(Time.time);
        if (rating == ConductorGestureRating.Motion && active > brightness)
            return;
        eventStartStrength = active;
        eventTargetStrength = brightness;
        eventHoldSeconds = hold;
        eventStartTime = Time.time;
    }

    public void AdvancePath(int progress, int total)
    {
        int count = Mathf.CeilToInt((float)progress / Mathf.Max(1, total) * path.Count);
        for (int i = 0; i < count; i++) path[i].material.color = new Color(myceliumColor.r, myceliumColor.g, myceliumColor.b, 1f);
        if (progress >= total && localLight != null) localLight.enabled = true;
    }

    public void Complete() { if (localLight != null) localLight.enabled = true; }
}
