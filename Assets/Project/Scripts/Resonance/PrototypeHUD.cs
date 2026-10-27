using TMPro;
using UnityEngine;

/// <summary>
/// Minimal UI for onboarding and resonance feedback.
/// </summary>
public class PrototypeHUD : MonoBehaviour
{
    [SerializeField] private float initialPromptDuration = 10f;
    [SerializeField] private float pickupPromptDuration = 5f;
    [SerializeField] private float instrumentDisplayDuration = 5f;
    [SerializeField] private float resonanceCompleteDuration = 5f;

    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI instrumentText;
    [SerializeField] private TextMeshProUGUI resonanceText;

    [SerializeField] private PlayerInstrumentController instrumentController;
    [SerializeField] private ResonanceMatcher resonanceMatcher;

    [SerializeField] private string initialPrompt = "WASD Move | Mouse Look | F Pick | RMB Resonate";
    [SerializeField] private string noInstrumentPrompt = "Move closer to the object.";
    [SerializeField] private string pickupPrompt = "F";
    [SerializeField] private string withInstrumentPrompt = "Right click 3 times to resonate.";
    [SerializeField] private string successPrompt = "The path rises. Climb upward.";

    private float initialPromptHideTime;
    private float pickupPromptHideTime = -1f;
    private float instrumentHideTime = -1f;
    private float resonanceCompleteHideTime = -1f;
    private bool trackedInstrumentPickup;
    private bool trackedResonanceComplete;

    private void Start()
    {
        if (instrumentController == null)
            instrumentController = FindFirstObjectByType<PlayerInstrumentController>();
        if (resonanceMatcher == null)
            resonanceMatcher = FindFirstObjectByType<ResonanceMatcher>();

        initialPromptHideTime = Time.time + initialPromptDuration;
    }

    private void Update()
    {
        TrackMilestones();

        if (instrumentText != null)
        {
            bool showInstrument = instrumentController != null
                && instrumentController.HasInstrument
                && Time.time <= instrumentHideTime;
            instrumentText.gameObject.SetActive(showInstrument);
            if (showInstrument)
                instrumentText.text = "Instrument: " + instrumentController.EquippedInstrument.InstrumentName;
        }

        if (resonanceText != null)
        {
            bool showResonance = resonanceMatcher != null && (resonanceMatcher.Progress01 > 0f || resonanceMatcher.HasSucceeded);
            if (resonanceMatcher != null && resonanceMatcher.HasSucceeded)
                showResonance = Time.time <= resonanceCompleteHideTime;

            resonanceText.gameObject.SetActive(showResonance);
            if (showResonance)
            {
                if (resonanceMatcher.HasSucceeded)
                    resonanceText.text = "Resonance: Complete";
                else
                    resonanceText.text = "Resonance: " + Mathf.RoundToInt(resonanceMatcher.Progress01 * 100f) + "%";
            }
        }

        if (promptText != null)
        {
            bool showPrompt = false;

            if (Time.time <= initialPromptHideTime)
            {
                promptText.text = initialPrompt;
                showPrompt = true;
            }
            else if (Time.time <= pickupPromptHideTime)
            {
                promptText.text = withInstrumentPrompt;
                showPrompt = true;
            }

            promptText.gameObject.SetActive(showPrompt);
        }
    }

    private void TrackMilestones()
    {
        if (!trackedInstrumentPickup && instrumentController != null && instrumentController.HasInstrument)
        {
            trackedInstrumentPickup = true;
            pickupPromptHideTime = Time.time + pickupPromptDuration;
            instrumentHideTime = Time.time + instrumentDisplayDuration;
        }

        if (!trackedResonanceComplete && resonanceMatcher != null && resonanceMatcher.HasSucceeded)
        {
            trackedResonanceComplete = true;
            resonanceCompleteHideTime = Time.time + resonanceCompleteDuration;
        }
    }
}
