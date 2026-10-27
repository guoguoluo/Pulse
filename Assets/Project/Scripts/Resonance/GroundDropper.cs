using UnityEngine;

/// <summary>
/// Drops an object onto nearby ground so prototype objects do not need manual height tuning.
/// </summary>
[DisallowMultipleComponent]
public class GroundDropper : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float probeHeight = 5f;
    [SerializeField] private float probeDistance = 20f;
    [SerializeField] private float groundOffset = 0.02f;
    [SerializeField] private bool runContinuously;

    private void Start()
    {
        SnapToGround();
    }

    private void Update()
    {
        if (runContinuously)
            SnapToGround();
    }

    [ContextMenu("Snap To Ground")]
    public void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * probeHeight;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, probeDistance, groundMask, QueryTriggerInteraction.Ignore))
            return;

        Vector3 position = transform.position;
        position.y = hit.point.y + groundOffset;
        transform.position = position;
    }
}
