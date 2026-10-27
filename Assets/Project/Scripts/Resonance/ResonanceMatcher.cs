using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple prototype resonance: three instrument plays completes the sequence.
/// </summary>
public class ResonanceMatcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInstrumentController instrumentController;

    [Header("Matching")]
    [SerializeField] private int requiredNotes = 3;

    [Header("Events")]
    [SerializeField] private UnityEvent onResonanceSuccess;
    [SerializeField] private UnityEvent<float> onProgressChanged;
    private int playedNotes;

    public float Progress01 { get; private set; }
    public bool HasSucceeded { get; private set; }

    private void Awake()
    {
        if (instrumentController == null)
            instrumentController = FindFirstObjectByType<PlayerInstrumentController>();
    }

    private void OnEnable()
    {
        if (instrumentController != null)
            instrumentController.NotePlayed += HandleNotePlayed;
    }

    private void OnDisable()
    {
        if (instrumentController != null)
            instrumentController.NotePlayed -= HandleNotePlayed;
    }

    private void HandleNotePlayed(float noteTime, ResonanceInstrument instrument)
    {
        if (HasSucceeded)
            return;

        playedNotes++;
        SetProgress((float)playedNotes / requiredNotes);

        if (playedNotes >= requiredNotes)
        {
            HasSucceeded = true;
            onResonanceSuccess?.Invoke();
        }
    }

    private void SetProgress(float value)
    {
        Progress01 = Mathf.Clamp01(value);
        onProgressChanged?.Invoke(Progress01);
    }
}
