using UnityEngine;

/// <summary>
/// Defines the sound and identity of a pickupable resonance instrument.
/// </summary>
public class ResonanceInstrument : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string instrumentName = "Stone Chime";

    [Header("Audio")]
    [SerializeField] private AudioClip noteClip;
    [SerializeField] private float volume = 1f;
    [SerializeField] private float pitch = 1f;

    [Header("Visual")]
    [SerializeField] private Color glowColor = new Color(0.5f, 0.8f, 1f, 1f);

    public string InstrumentName => instrumentName;
    public AudioClip NoteClip => noteClip;
    public float Volume => volume;
    public float Pitch => pitch;
    public Color GlowColor => glowColor;
}
