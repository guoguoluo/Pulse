using TMPro;
using UnityEngine;

/// <summary>
/// Shows a configurable TMP message for a configurable duration.
/// </summary>
public class GoalFeedbackPresenter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private string message = "You reached the high place. The pulse accepts you.";
    [SerializeField] private float displayDuration = 5f;

    private float hideTime = -1f;

    private void Update()
    {
        if (targetText != null && targetText.gameObject.activeSelf && hideTime >= 0f && Time.time > hideTime)
            targetText.gameObject.SetActive(false);
    }

    private void Awake()
    {
        if (targetText != null)
            targetText.gameObject.SetActive(false);
    }

    public void Present()
    {
        if (targetText != null)
        {
            targetText.text = message;
            targetText.gameObject.SetActive(true);
            hideTime = Time.time + displayDuration;
        }
    }

    public void PresentMessage(string overrideMessage)
    {
        if (targetText != null)
        {
            targetText.text = overrideMessage;
            targetText.gameObject.SetActive(true);
            hideTime = Time.time + displayDuration;
        }
    }
}
