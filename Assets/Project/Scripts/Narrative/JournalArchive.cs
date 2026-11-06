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
    private int collectedMask;
    private const int FragmentCount = 6;
    private const string FragmentMaskSuffix = ".Fragments.v2";
    public int UnlockedCount => unlockedCount;
    public int TotalCount => chapters.Count;
    public bool HasSavedProgress => PlayerPrefs.GetInt(saveKey, 0) > 0 ||
        PlayerPrefs.GetInt(saveKey + FragmentMaskSuffix, 0) != 0;
    public event Action Changed;

    private void Awake()
    {
        unlockedCount = Mathf.Clamp(PlayerPrefs.GetInt(saveKey, 0), 0, chapters.Count);
        string maskKey = saveKey + FragmentMaskSuffix;
        if (PlayerPrefs.HasKey(maskKey))
            collectedMask = PlayerPrefs.GetInt(maskKey, 0) & ((1 << FragmentCount) - 1);
        else
        {
            // Earlier saves collected fragments in order, so their count identifies the collected sites.
            collectedMask = (1 << Mathf.Min(unlockedCount, FragmentCount)) - 1;
            if (unlockedCount > 0)
            {
                PlayerPrefs.SetInt(maskKey, collectedMask);
                PlayerPrefs.Save();
            }
        }
    }

    public bool IsFragmentCollected(int fragmentIndex)
    {
        return fragmentIndex >= 0 && fragmentIndex < FragmentCount &&
            (collectedMask & (1 << fragmentIndex)) != 0;
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

    public bool CollectFragment(int fragmentIndex)
    {
        if (fragmentIndex < 0 || fragmentIndex >= FragmentCount ||
            IsFragmentCollected(fragmentIndex) || unlockedCount >= chapters.Count) return false;
        collectedMask |= 1 << fragmentIndex;
        unlockedCount = Mathf.Min(unlockedCount + 1, chapters.Count);
        PlayerPrefs.SetInt(saveKey, unlockedCount);
        PlayerPrefs.SetInt(saveKey + FragmentMaskSuffix, collectedMask);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    [ContextMenu("Reset Journal Progress")]
    public void ResetProgress()
    {
        unlockedCount = 0;
        collectedMask = 0;
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.DeleteKey(saveKey + FragmentMaskSuffix);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
