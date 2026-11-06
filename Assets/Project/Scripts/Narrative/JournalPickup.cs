using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SphereCollider))]
public class JournalPickup : MonoBehaviour
{
    [SerializeField] private JournalArchive archive;
    [SerializeField] private JournalReader reader;
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private GameObject visual;

    private Transform player;
    private PlayerController playerController;
    private SphereCollider pickupCollider;
    private bool collectedThisSession;

    private void Awake()
    {
        if (archive == null) archive = FindFirstObjectByType<JournalArchive>();
        if (reader == null) reader = FindFirstObjectByType<JournalReader>();
        pickupCollider = GetComponent<SphereCollider>();
        pickupCollider.isTrigger = true;
        playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Start()
    {
        if (archive != null && archive.IsFragmentCollected(0)) Hide();
    }

    private void Update()
    {
        if (collectedThisSession || archive == null || reader == null) return;
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null) return;
        player = playerController.transform;
        bool nearby = Vector3.Distance(player.position, pickupCollider.bounds.center) <= interactionDistance;
        if (!nearby)
        {
            reader.ClearPickupTarget(visual != null ? visual.transform : transform);
            reader.SetFirstFragmentNearby(false);
            return;
        }
        if (reader.IsOpen)
        {
            reader.ClearPickupTarget(visual != null ? visual.transform : transform);
            reader.SetFirstFragmentNearby(false);
            return;
        }
        reader.SetFirstFragmentNearby(false);
        reader.SetPickupTarget(visual != null ? visual.transform : transform, true);
        reader.SetPickupAvailable(true);
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && archive.CollectFragment(0))
        {
            Hide();
            reader.ShowAcquired();
        }
    }

    private void Hide()
    {
        collectedThisSession = true;
        if (reader != null)
        {
            reader.ClearPickupTarget(visual != null ? visual.transform : transform);
            reader.SetFirstFragmentNearby(false);
        }
        if (visual != null) visual.SetActive(false);
        else if (TryGetComponent<MeshRenderer>(out var renderer)) renderer.enabled = false;
    }
}
