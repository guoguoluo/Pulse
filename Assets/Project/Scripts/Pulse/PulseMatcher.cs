using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PulseMatcher — core gameplay system for beat-phase alignment.
/// Subscribes to FootstepEventBus; continuously calculates player BPM
/// vs active PulseProfile match quality.
///
/// Feedback:
///   - Alignment rising: AudioMixer "PulseAlignment" param clears filter
///   - Aligned: triggers OnAligned event (play narration fragment)
///   - Lost alignment: triggers OnMisaligned (pulse fades)
///
/// StepAccent gives a bonus to alignment delta (heavy step = stronger lock).
/// StepPause briefly pauses BPM tracking (creates intentional gap/line-break).
/// </summary>
public class PulseMatcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PulseProfile activeProfile;

    [Header("Alignment Settings")]
    [Tooltip("Alignment >= this value counts as successfully aligned (0-1)")]
    [Range(0.5f, 1f)]
    [SerializeField] private float alignmentThreshold = 0.8f;
    [Tooltip("Decay rate per second when no steps detected")]
    [Range(0.5f, 5f)]
    [SerializeField] private float decayRate = 2f;
    [Tooltip("Extra alignment gain multiplier for accented steps")]
    [Range(1f, 3f)]
    [SerializeField] private float accentBonusMultiplier = 1.8f;

    [Header("Audio Mixer")]
    [Tooltip("Exposed AudioMixer param name for pulse layer volume/filter")]
    [SerializeField] private string audioMixerParam = "PulseAlignment";

    [Header("Events")]
    public UnityEvent OnAligned;
    public UnityEvent OnMisaligned;

    private FootstepDetector footstepDetector;
    private PlayerController playerController;
    private float currentAlignment;
    private int consecutiveAlignedSteps;
    private bool wasAligned;
    private float lastStepTime;
    private float pauseUntil;

    public float CurrentAlignment { get { return currentAlignment; } }
    public PulseProfile ActiveProfile
    {
        get { return activeProfile; }
        set { activeProfile = value; }
    }

    private void Awake()
    {
        footstepDetector = GetComponent<FootstepDetector>();
        if (footstepDetector == null)
            footstepDetector = FindFirstObjectByType<FootstepDetector>();
        playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        FootstepEventBus.OnFootstep += HandleFootstep;
    }

    private void OnDisable()
    {
        FootstepEventBus.OnFootstep -= HandleFootstep;
    }

    private void Update()
    {
        if (activeProfile == null) return;

        // If player tapped StepPause, briefly freeze the BPM tracking
        if (playerController != null && playerController.IsPaused)
        {
            pauseUntil = Time.time + 0.5f;
        }

        float timeSinceLastStep = Time.time - lastStepTime;
        if (Time.time < pauseUntil || timeSinceLastStep > activeProfile.BeatInterval * 2.5f)
        {
            currentAlignment = Mathf.Max(0f, currentAlignment - decayRate * Time.deltaTime);
            consecutiveAlignedSteps = 0;
        }

        bool isAligned = currentAlignment >= alignmentThreshold;
        if (isAligned && !wasAligned)
        {
            wasAligned = true;
            GameManager.Instance?.SetAligned();
            OnAligned.Invoke();
        }
        else if (!isAligned && wasAligned)
        {
            wasAligned = false;
            GameManager.Instance?.SetWalking();
            OnMisaligned.Invoke();
        }

        UpdateAudioMixer();
    }

    private void HandleFootstep(FootstepEvent evt)
    {
        if (activeProfile == null) return;
        if (Time.time < pauseUntil) return;

        lastStepTime = Time.time;
        float playerBPM = footstepDetector.CurrentStepBPM;

        bool inTolerance = activeProfile.IsBPMInTolerance(playerBPM);

        if (inTolerance)
        {
            float devPercent = Mathf.Abs(playerBPM - activeProfile.BPM) / activeProfile.BPM * 100f;
            float closeness = 1f - (devPercent / activeProfile.TolerancePercent);
            closeness = Mathf.Clamp01(closeness);

            // Accent bonus: heavy steps lock harder
            float bonus = evt.isAccent ? accentBonusMultiplier : 1f;
            float lerpRate = 0.3f * bonus;
            currentAlignment = Mathf.Lerp(currentAlignment, closeness, lerpRate);

            consecutiveAlignedSteps++;
        }
        else
        {
            currentAlignment = Mathf.Max(0f, currentAlignment - decayRate * Time.deltaTime * 2f);
            consecutiveAlignedSteps = 0;
        }
    }

    private void UpdateAudioMixer()
    {
        AudioManager.Instance?.SetMixerParameter(audioMixerParam, currentAlignment);
    }
}
