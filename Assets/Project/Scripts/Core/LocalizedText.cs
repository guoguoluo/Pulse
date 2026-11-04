using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private TextConfig config;
    [SerializeField] private string key;
    private TMP_Text label;

    private void OnEnable() { Refresh(); }
    public void Refresh()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (config == null) config = Resources.Load<TextConfig>("TextConfig");
        if (config != null) label.text = config.Get(key);
    }
}
