using UnityEngine;

/// <summary>Camera-only follow; mouse remains reserved for conducting.</summary>
[RequireComponent(typeof(Camera))]
public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Framing")]
    [Tooltip("World-space position relative to the player. Negative Z stays behind the path.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 4.1f, -10.5f);
    [Tooltip("Look this far above the player's feet.")]
    [Min(0f)] [SerializeField] private float lookHeight = 1.75f;
    [Tooltip("Look slightly ahead along the path.")]
    [SerializeField] private float lookAhead = 2.3f;
    [Range(40f, 90f)] [SerializeField] private float fieldOfView = 68f;

    [Header("Follow")]
    [Tooltip("Higher values create more positional lag.")]
    [Min(0.01f)] [SerializeField] private float positionSmoothTime = 0.28f;
    [Tooltip("Higher values turn toward the target more slowly.")]
    [Min(0.01f)] [SerializeField] private float rotationSmoothTime = 0.2f;
    [Header("Ground Clearance")]
    [Tooltip("Minimum height of the camera above the ground below it.")]
    [Min(0f)] [SerializeField] private float minimumGroundClearance = 0.8f;
    [Tooltip("Ground geometry used for the camera height check.")]
    [SerializeField] private LayerMask groundLayers = ~0;

    private Camera followCamera;
    private Vector3 followVelocity;
    private Vector3 lookVelocity;
    private Vector3 smoothedLookPoint;

private void Awake()
    {
        followCamera = GetComponent<Camera>();
        if (target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }
        if (target == null) { enabled = false; return; }

        transform.SetParent(null, true);
        transform.position = AboveGround(target.position + offset);
        smoothedLookPoint = target.position + Vector3.up * lookHeight + Vector3.forward * lookAhead;
        transform.rotation = Quaternion.LookRotation(smoothedLookPoint - transform.position, Vector3.up);
        followCamera.fieldOfView = fieldOfView;
    }

private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPosition = AboveGround(target.position + offset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition,
            ref followVelocity, positionSmoothTime);
        transform.position = AboveGround(transform.position);
        Vector3 desiredLookPoint = target.position + Vector3.up * lookHeight + Vector3.forward * lookAhead;
        smoothedLookPoint = Vector3.SmoothDamp(smoothedLookPoint, desiredLookPoint,
            ref lookVelocity, rotationSmoothTime);
        Vector3 direction = smoothedLookPoint - transform.position;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private Vector3 AboveGround(Vector3 position)
    {
        Vector3 origin = new Vector3(position.x, Mathf.Max(position.y, target.position.y) + 40f, position.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 100f, groundLayers, QueryTriggerInteraction.Ignore);
        float groundHeight = float.NegativeInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.transform.IsChildOf(target)) continue;
            if (hits[i].normal.y < 0.5f) continue;
            if (hits[i].point.y > groundHeight) groundHeight = hits[i].point.y;
        }
        if (groundHeight > float.NegativeInfinity)
            position.y = Mathf.Max(position.y, groundHeight + minimumGroundClearance);
        return position;
    }
}
