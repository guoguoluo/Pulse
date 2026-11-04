using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class JournalArchive : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public string title;
        [TextArea(8, 20)] public string body;
    }

    [SerializeField] private List<Entry> chapters = new List<Entry>();
    [SerializeField] private TextConfig textConfig;
    [SerializeField] private string saveKey = "Pulse.Journal.Chapters.v1";

    private int unlockedCount;
    public int UnlockedCount => unlockedCount;
    public int TotalCount => chapters.Count;
    public bool HasSavedProgress => PlayerPrefs.GetInt(saveKey, 0) > 0;
    public event Action Changed;

    private void Awake()
    {
        unlockedCount = Mathf.Clamp(PlayerPrefs.GetInt(saveKey, 0), 0, chapters.Count);
    }

    public Entry GetChapter(int index)
    {
        if (index < 0 || index >= unlockedCount) return null;
        if (textConfig == null) textConfig = Resources.Load<TextConfig>("TextConfig");
        return textConfig == null ? chapters[index] : new Entry
        {
            title = textConfig.Get($"journal.{index + 1:00}.title"),
            body = textConfig.Get($"journal.{index + 1:00}.body")
        };
    }

    public bool CollectNext()
    {
        if (unlockedCount >= chapters.Count) return false;
        unlockedCount++;
        PlayerPrefs.SetInt(saveKey, unlockedCount);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    [ContextMenu("Reset Journal Progress")]
    public void ResetProgress()
    {
        unlockedCount = 0;
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
