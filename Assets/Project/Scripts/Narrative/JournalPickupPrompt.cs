using TMPro;
using UnityEngine;

/// <summary>Screen-space interaction prompt anchored to a journal fragment.</summary>
public class JournalPickupPrompt : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI actionText;
    [SerializeField] private CanvasGroup visualGroup;
    [SerializeField] private TextConfig textConfig;
    [SerializeField] private string actionKey = "journal.pickup.action";
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.65f, 0f);
    [SerializeField] private Vector2 screenOffset = new Vector2(38f, 12f);
    [Min(0f)] [SerializeField] private float followSpeed = 15f;

    private RectTransform rect;
    private RectTransform canvasRect;
    private Canvas canvas;
    private Camera worldCamera;
    private Transform target;
    private bool requestedVisible;
    private bool positioned;

    public void SetTarget(Transform value)
    {
        if (target == value) return;
        target = value;
        positioned = false;
    }

    public void SetVisible(bool value)
    {
        requestedVisible = value;
        if (visualGroup != null) visualGroup.alpha = value ? 1f : 0f;
    }

    private void Awake()
    {
        rect = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasRect = canvas.transform as RectTransform;
        if (textConfig == null) textConfig = Resources.Load<TextConfig>("TextConfig");
        if (actionText != null)
            actionText.text = textConfig != null ? textConfig.Get(actionKey) : "拾取";
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (!requestedVisible || rect == null || canvasRect == null || target == null) return;
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;
        Vector3 screen = worldCamera.WorldToScreenPoint(target.position + worldOffset);
        bool onScreen = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width &&
            screen.y >= 0f && screen.y <= Screen.height;
        if (visualGroup != null) visualGroup.alpha = onScreen ? 1f : 0f;
        if (!onScreen) { positioned = false; return; }

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screen, uiCamera, out Vector2 local)) return;
        Vector2 desired = local + screenOffset;
        if (!positioned)
        {
            rect.anchoredPosition = desired;
            positioned = true;
        }
        else
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, desired,
                1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime));
    }
}
