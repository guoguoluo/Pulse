using TMPro;
using UnityEngine;

/// <summary>
/// Simple world-space prompt that faces the main camera.
/// </summary>
public class WorldPromptPresenter : MonoBehaviour
{
    [SerializeField] private Canvas promptCanvas;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string displayText = "F";
    [SerializeField] private Vector3 billboardOffset = new Vector3(0f, 1.2f, 0f);

    private Camera mainCamera;

    private void Awake()
    {
        if (promptCanvas == null)
            promptCanvas = GetComponentInChildren<Canvas>(true);
        if (promptText == null)
            promptText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (promptText != null)
            promptText.text = displayText;

        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (promptCanvas == null || !promptCanvas.gameObject.activeInHierarchy)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        transform.localPosition = billboardOffset;

        if (mainCamera == null)
            return;

        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    public void SetVisible(bool visible)
    {
        if (promptCanvas != null)
            promptCanvas.gameObject.SetActive(visible);
    }
}
