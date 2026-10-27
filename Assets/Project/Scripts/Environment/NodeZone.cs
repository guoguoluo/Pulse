using UnityEngine;

/// <summary>
/// NodeZone — trigger volume at the node area.
/// Notifies GaitRecognizer when player enters/exits.
/// The zone is a safe area where the player's walk pattern is watched.
///
/// Attach to a GameObject with a Collider (IsTrigger = true).
/// Size the collider to cover the area around the node event location.
/// </summary>
[RequireComponent(typeof(Collider))]
public class NodeZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("If null, auto-finds GaitRecognizer on player at runtime")]
    [SerializeField] private GaitRecognizer gaitRecognizer;

    [Header("Debug")]
    [SerializeField] private bool showDebugBounds = true;

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (gaitRecognizer == null)
            gaitRecognizer = other.GetComponent<GaitRecognizer>();

        if (gaitRecognizer != null)
            gaitRecognizer.EnterNodeZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (gaitRecognizer == null)
            gaitRecognizer = other.GetComponent<GaitRecognizer>();

        if (gaitRecognizer != null)
            gaitRecognizer.ExitNodeZone();
    }

    private void OnDrawGizmos()
    {
        if (!showDebugBounds) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);

        if (TryGetComponent<BoxCollider>(out BoxCollider box))
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.6f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (TryGetComponent<SphereCollider>(out SphereCollider sphere))
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.6f);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
    }
}
