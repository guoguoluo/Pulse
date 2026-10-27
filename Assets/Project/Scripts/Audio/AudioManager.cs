using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioManager — global audio system (singleton).
///
/// AudioMixer group hierarchy:
///   Master
///   |-- Ambience Group (environment ambience)
///   |-- Pulse Group (heartbeat/reference pulse)
///   |-- Footsteps Group (step SFX, varied by terrain)
///   |-- Voice Group (narration dialogue)
///
/// Handles footstep playback (terrain-based), mixer parameter control,
/// pulse beat playback, and ambience crossfading.
/// </summary>
[RequireComponent(typeof(AudioSource), typeof(AudioSource), typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Footstep Sources")]
    [SerializeField] private AudioSource footstepSource1;
    [SerializeField] private AudioSource footstepSource2;
    private bool useSource1 = true;

    [Header("Footstep Clips by Terrain")]
    [SerializeField] private AudioClip[] footstepMetal;
    [SerializeField] private AudioClip[] footstepStone;
    [SerializeField] private AudioClip[] footstepDirt;
    [SerializeField] private AudioClip[] footstepOrganic;
    [SerializeField] private AudioClip[] footstepVoid;

    [Header("Accented Step Clips (optional, louder/heavier)")]
    [SerializeField] private AudioClip[] footstepMetalAccent;
    [SerializeField] private AudioClip[] footstepStoneAccent;
    [SerializeField] private AudioClip[] footstepDirtAccent;

    [Header("Pulse Beat")]
    [SerializeField] private AudioSource pulseSource;
    [SerializeField] private AudioClip defaultPulseClip;

    [Header("Ambience")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioClip corridorAmbience;
    [SerializeField] private AudioClip revealAmbience;

    [Header("Volume / Variation")]
    [Range(0f, 1f)]
    [SerializeField] private float footstepVolumeBase = 0.7f;
    [Range(0f, 0.5f)]
    [SerializeField] private float footstepVolumeVariance = 0.15f;
    [SerializeField] private float footstepPitchBase = 1f;
    [SerializeField] private float footstepPitchVariance = 0.1f;
    [Tooltip("Extra volume for accented (heavy) steps")]
    [Range(0f, 1f)]
    [SerializeField] private float accentVolumeBoost = 0.15f;

    [Header("Mixer Param Names")]
    [SerializeField] private string pulseVolumeParam = "PulseVolume";
    [SerializeField] private string pulseLowpassParam = "PulseLowpass";

    private Dictionary<GroundMaterial.TerrainType, AudioClip[]> footstepClipMap;
    private Dictionary<GroundMaterial.TerrainType, AudioClip[]> footstepAccentMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildFootstepMap();
    }

    private void Start()
    {
        if (ambienceSource != null && corridorAmbience != null)
        {
            ambienceSource.clip = corridorAmbience;
            ambienceSource.loop = true;
            ambienceSource.Play();
        }
    }

    private void OnEnable()
    {
        FootstepEventBus.OnFootstep += PlayFootstep;
    }

    private void OnDisable()
    {
        FootstepEventBus.OnFootstep -= PlayFootstep;
    }

    private void BuildFootstepMap()
    {
        footstepClipMap = new Dictionary<GroundMaterial.TerrainType, AudioClip[]>
        {
            { GroundMaterial.TerrainType.Metal,   footstepMetal },
            { GroundMaterial.TerrainType.Stone,   footstepStone },
            { GroundMaterial.TerrainType.Dirt,    footstepDirt },
            { GroundMaterial.TerrainType.Organic, footstepOrganic },
            { GroundMaterial.TerrainType.Void,    footstepVoid }
        };

        footstepAccentMap = new Dictionary<GroundMaterial.TerrainType, AudioClip[]>
        {
            { GroundMaterial.TerrainType.Metal, footstepMetalAccent },
            { GroundMaterial.TerrainType.Stone, footstepStoneAccent },
            { GroundMaterial.TerrainType.Dirt,  footstepDirtAccent }
        };
    }

    private void PlayFootstep(FootstepEvent evt)
    {
        AudioClip[] clips;
        bool useAccent = evt.isAccent && footstepAccentMap.TryGetValue(evt.terrain, out AudioClip[] ac) && ac != null && ac.Length > 0;
        if (!useAccent)
        {
            if (!footstepClipMap.TryGetValue(evt.terrain, out clips) || clips == null || clips.Length == 0)
                return;
        }
        else
        {
            clips = footstepAccentMap[evt.terrain];
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        AudioSource source = useSource1 ? footstepSource1 : footstepSource2;
        useSource1 = !useSource1;

        float vol = footstepVolumeBase + Random.Range(-footstepVolumeVariance, footstepVolumeVariance);
        if (useAccent) vol += accentVolumeBoost;
        source.volume = Mathf.Clamp01(vol);
        source.pitch = footstepPitchBase + Random.Range(-footstepPitchVariance, footstepPitchVariance);
        source.PlayOneShot(clip);
    }

    /// <summary>
    /// Set an AudioMixer parameter (0-1 normalized, mapped to actual range).
    /// </summary>
    public void SetMixerParameter(string paramName, float value01)
    {
        if (audioMixer == null) return;

        float mapped;

        switch (paramName)
        {
            case "PulseAlignment":
            case "PulseVolume":
                mapped = Mathf.Lerp(-80f, 0f, Mathf.Clamp01(value01));
                break;
            case "PulseLowpass":
                mapped = Mathf.Lerp(500f, 22000f, Mathf.Clamp01(value01));
                break;
            case "MasterWarmth":
                mapped = Mathf.Lerp(-12f, 0f, Mathf.Clamp01(value01));
                break;
            default:
                mapped = Mathf.Lerp(-80f, 0f, Mathf.Clamp01(value01));
                break;
        }

        if (!audioMixer.SetFloat(paramName, mapped))
        {
            // Fallback: try direct value
            audioMixer.SetFloat(paramName, mapped);
        }
    }

    public void PlayPulseBeat()
    {
        if (pulseSource != null)
        {
            if (defaultPulseClip != null && pulseSource.clip == null)
                pulseSource.clip = defaultPulseClip;
            pulseSource.Play();
        }
    }

    public void CrossfadeAmbience(AudioClip newClip, float fadeDuration = 2f)
    {
        if (ambienceSource == null) return;
        StartCoroutine(CrossfadeAmbienceRoutine(newClip, fadeDuration));
    }

    private System.Collections.IEnumerator CrossfadeAmbienceRoutine(AudioClip newClip, float duration)
    {
        float startVol = ambienceSource.volume;
        float elapsed = 0f;

        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            ambienceSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (duration / 2f));
            yield return null;
        }

        ambienceSource.clip = newClip;
        ambienceSource.Play();
        elapsed = 0f;

        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            ambienceSource.volume = Mathf.Lerp(0f, startVol, elapsed / (duration / 2f));
            yield return null;
        }
        ambienceSource.volume = startVol;
    }
}
