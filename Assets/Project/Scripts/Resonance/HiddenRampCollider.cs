using UnityEngine;

/// <summary>
/// Keeps a hidden ramp collider aligned to a staircase so the player can walk up smoothly.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class HiddenRampCollider : MonoBehaviour
{
    [SerializeField] private Transform rampTransform;
    [SerializeField] private BoxCollider rampCollider;
    [SerializeField] private Vector3 localCenter = new Vector3(0f, 1.5f, 4f);
    [SerializeField] private Vector3 localEulerAngles = new Vector3(-16.7f, 0f, 0f);
    [SerializeField] private Vector3 colliderSize = new Vector3(3f, 0.5f, 10f);
    [SerializeField] private bool hideRenderer = true;

    private void Reset()
    {
        EnsureRamp();
        ApplyLayout();
    }

    private void OnValidate()
    {
        EnsureRamp();
        ApplyLayout();
    }

    private void Awake()
    {
        EnsureRamp();
        ApplyLayout();
    }

    private void EnsureRamp()
    {
        if (rampTransform == null)
        {
            Transform existing = transform.Find("HiddenRamp");
            if (existing != null)
                rampTransform = existing;
        }

        if (rampTransform == null)
        {
            GameObject rampObject = new GameObject("HiddenRamp");
            rampObject.transform.SetParent(transform, false);
            rampTransform = rampObject.transform;
        }

        if (rampCollider == null)
            rampCollider = rampTransform.GetComponent<BoxCollider>();

        if (rampCollider == null)
            rampCollider = rampTransform.gameObject.AddComponent<BoxCollider>();

        MeshRenderer meshRenderer = rampTransform.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = rampTransform.GetComponent<MeshFilter>();
        if (hideRenderer)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = false;
            if (meshFilter != null)
                meshFilter.sharedMesh = null;
        }
    }

    [ContextMenu("Apply Ramp Layout")]
    public void ApplyLayout()
    {
        if (rampTransform == null || rampCollider == null)
            return;

        rampTransform.localPosition = localCenter;
        rampTransform.localRotation = Quaternion.Euler(localEulerAngles);
        rampTransform.localScale = Vector3.one;

        rampCollider.isTrigger = false;
        rampCollider.center = Vector3.zero;
        rampCollider.size = colliderSize;
    }
}
