using UnityEngine;

/// <summary>
/// PulseZone — trigger collider that switches PulseProfile when player enters.
/// Each corridor segment should have one PulseZone overlapping its extent.
///
/// Attach to a GameObject with Collider (IsTrigger=true).
/// </summary>
[RequireComponent(typeof(Collider))]
public class PulseZone : MonoBehaviour
{
    [Tooltip("The pulse profile for this zone")]
    [SerializeField] private PulseProfile zoneProfile;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PulseMatcher matcher = other.GetComponent<PulseMatcher>();
        if (matcher != null && zoneProfile != null)
        {
            matcher.ActiveProfile = zoneProfile;
            Debug.Log("[PulseZone] Switched to: " + zoneProfile.name + " (BPM=" + zoneProfile.BPM + ")");
        }
    }

    private void OnDrawGizmos()
    {
        if (zoneProfile == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        if (TryGetComponent<BoxCollider>(out BoxCollider box))
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (TryGetComponent<SphereCollider>(out SphereCollider sphere))
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
    }
}
