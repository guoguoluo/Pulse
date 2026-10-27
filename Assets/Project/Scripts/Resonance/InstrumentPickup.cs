using UnityEngine;

/// <summary>
/// Simple world pickup that equips a ResonanceInstrument on the player.
/// </summary>
public class InstrumentPickup : MonoBehaviour
{
    [SerializeField] private ResonanceInstrument instrument;
    [SerializeField] private GameObject pickupVisual;
    [SerializeField] private WorldPromptPresenter worldPrompt;
    [SerializeField] private GroundDropper groundDropper;

    public ResonanceInstrument Instrument => instrument;

    private void Awake()
    {
        if (worldPrompt == null)
            worldPrompt = GetComponentInChildren<WorldPromptPresenter>(true);
        if (groundDropper == null)
            groundDropper = GetComponent<GroundDropper>();
    }

    private void Start()
    {
        if (groundDropper != null)
            groundDropper.SnapToGround();
    }

    private void Reset()
    {
        instrument = GetComponent<ResonanceInstrument>();
    }

    public void SetPromptVisible(bool visible)
    {
        if (worldPrompt != null)
            worldPrompt.SetVisible(visible);
    }

    public void Collect()
    {
        SetPromptVisible(false);

        if (pickupVisual != null)
            pickupVisual.SetActive(false);

        gameObject.SetActive(false);
    }
}
