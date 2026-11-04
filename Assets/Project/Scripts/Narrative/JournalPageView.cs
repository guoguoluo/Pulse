using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JournalPageView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private ScrollRect bodyScroll;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    private JournalReader reader;

    public void Bind(JournalReader owner)
    {
        reader = owner;
        if (previousButton != null) previousButton.onClick.AddListener(Previous);
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void SetEntry(string title, string body, int index, int total)
    {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (counterText != null) counterText.text = string.Format("{0:00} / {1:00}", index, total);
        if (bodyScroll != null) bodyScroll.verticalNormalizedPosition = 1f;
        if (previousButton != null) previousButton.interactable = index > 1;
        if (nextButton != null) nextButton.interactable = index < total;
    }

    public void Previous() { if (reader != null) reader.ChangePage(-1); }
    public void Next() { if (reader != null) reader.ChangePage(1); }
    public void Close() { if (reader != null) reader.Close(); }
}
