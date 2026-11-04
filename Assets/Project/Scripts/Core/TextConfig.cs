using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TextConfig", menuName = "PulseMachine/Text Config")]
public class TextConfig : ScriptableObject
{
    public enum Language { Chinese, English }

    [Serializable]
    public class Row
    {
        public string key;
        [TextArea(2, 12)] public string chinese;
        [TextArea(2, 12)] public string english;
    }

    [SerializeField] private Language language;
    [SerializeField] private List<Row> rows = new List<Row>();
    private Dictionary<string, Row> lookup;

    public Language CurrentLanguage => language;

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (lookup == null)
        {
            lookup = new Dictionary<string, Row>(StringComparer.Ordinal);
            foreach (var row in rows)
                if (row != null && !string.IsNullOrEmpty(row.key)) lookup[row.key] = row;
        }
        if (!lookup.TryGetValue(key, out var entry))
        {
            Debug.LogWarning("Missing text key: " + key, this);
            return key;
        }
        return language == Language.English && !string.IsNullOrEmpty(entry.english)
            ? entry.english : entry.chinese;
    }

    public void SetLanguage(Language value) { language = value; }
    private void OnValidate() { lookup = null; }
}
