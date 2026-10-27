using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Handles nearby pickup interaction and playing the equipped instrument.
/// </summary>
public class PlayerInstrumentController : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private float pickupRange = 2.2f;
    [SerializeField] private LayerMask pickupMask = ~0;
    [SerializeField] private Transform interactionOrigin;

    [Header("Audio")]
    [SerializeField] private AudioSource instrumentSource;

    [Header("Events")]
    [SerializeField] private UnityEvent<string> onInstrumentPicked;
    [SerializeField] private UnityEvent onInstrumentPlayed;

    private PlayerInputActions inputActions;
    private ResonanceInstrument equippedInstrument;
    private InstrumentPickup currentPickup;

    public event Action<float, ResonanceInstrument> NotePlayed;

    public ResonanceInstrument EquippedInstrument => equippedInstrument;
    public bool HasInstrument => equippedInstrument != null;
    public bool HasPickupInRange => currentPickup != null && !HasInstrument;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        if (interactionOrigin == null)
            interactionOrigin = transform;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player["Pick"].performed += OnPickPerformed;
        inputActions.Player["Resonate"].performed += OnResonatePerformed;
    }

    private void OnDisable()
    {
        inputActions.Player["Pick"].performed -= OnPickPerformed;
        inputActions.Player["Resonate"].performed -= OnResonatePerformed;
        inputActions.Disable();
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
    }

    private void Update()
    {
        UpdateNearbyPickup();
    }

    private void OnPickPerformed(InputAction.CallbackContext context)
    {
        TryPickInstrument();
    }

    private void OnResonatePerformed(InputAction.CallbackContext context)
    {
        PlayEquippedInstrument();
    }

    public bool TryPickInstrument()
    {
        UpdateNearbyPickup();
        if (currentPickup == null || currentPickup.Instrument == null)
            return false;

        equippedInstrument = currentPickup.Instrument;
        currentPickup.Collect();
        currentPickup = null;
        onInstrumentPicked?.Invoke(equippedInstrument.InstrumentName);
        return true;
    }

    public bool PlayEquippedInstrument()
    {
        if (equippedInstrument == null)
            return false;

        if (instrumentSource != null && equippedInstrument.NoteClip != null)
        {
            instrumentSource.pitch = equippedInstrument.Pitch;
            instrumentSource.PlayOneShot(equippedInstrument.NoteClip, equippedInstrument.Volume);
        }

        NotePlayed?.Invoke(Time.time, equippedInstrument);
        onInstrumentPlayed?.Invoke();
        return true;
    }

    private void UpdateNearbyPickup()
    {
        InstrumentPickup previousPickup = currentPickup;

        if (HasInstrument)
        {
            currentPickup = null;
            if (previousPickup != null)
                previousPickup.SetPromptVisible(false);
            return;
        }

        Vector3 origin = interactionOrigin.position + Vector3.up * 0.4f;
        Collider[] hits = Physics.OverlapSphere(origin, pickupRange, pickupMask, QueryTriggerInteraction.Collide);

        InstrumentPickup bestPickup = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            InstrumentPickup pickup = hits[i].GetComponentInParent<InstrumentPickup>();
            if (pickup == null || pickup.Instrument == null || !pickup.gameObject.activeInHierarchy)
                continue;

            float distance = (pickup.transform.position - interactionOrigin.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPickup = pickup;
            }
        }

        currentPickup = bestPickup;

        if (previousPickup != null && previousPickup != currentPickup)
            previousPickup.SetPromptVisible(false);

        if (currentPickup != null)
            currentPickup.SetPromptVisible(true);
    }
}
