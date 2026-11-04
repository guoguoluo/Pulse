using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class JournalReader : MonoBehaviour
{
    [SerializeField] private JournalArchive archive;
    [SerializeField] private TextConfig textConfig;
    [SerializeField] private JournalPageView pagePrefab;
    [SerializeField] private RectTransform pageParent;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private PlayerController playerController;

    private JournalPageView pageView;
    private int page;
    private JournalFragmentSite nearbySite;
    private bool firstFragmentNearby;
    private bool pickupAvailable;
    [Min(0.5f)] [SerializeField] private float acquiredNoticeSeconds = 3.5f;
    private float acquiredUntil;
    private string acquiredMessage;
    public bool IsOpen => pageView != null;
    public bool InputEnabled { get; set; } = true;
    public void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
        RefreshHint();
    }

    private void Awake()
    {
        if (archive == null) archive = FindFirstObjectByType<JournalArchive>();
        if (textConfig == null) textConfig = Resources.Load<TextConfig>("TextConfig");
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        RefreshHint();
    }

    private void Update()
    {
        if (acquiredUntil > 0f && Time.unscaledTime >= acquiredUntil)
        {
            acquiredUntil = 0f;
            RefreshHint();
        }

        if (!InputEnabled || Keyboard.current == null) return;
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            if (IsOpen) Close();
            else if (archive != null && archive.UnlockedCount > 0) OpenLatest();
        }
        if (!IsOpen) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) ChangePage(-1);
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) ChangePage(1);
    }

    public void OpenLatest()
    {
        if (archive == null || archive.UnlockedCount == 0 || pagePrefab == null) return;
        page = archive.UnlockedCount - 1;
        if (pageView == null)
        {
            RectTransform parent = pageParent != null ? pageParent : transform as RectTransform;
            pageView = Instantiate(pagePrefab, parent, false);
            pageView.Bind(this);
        }
        pageView.gameObject.SetActive(true);
        if (playerController != null) playerController.SetJournalOpen(true);
        Refresh();
    }

    public void Close()
    {
        if (pageView != null)
        {
            Destroy(pageView.gameObject);
            pageView = null;
        }
        if (playerController != null) playerController.SetJournalOpen(false);
        RefreshHint();
    }

    public void ChangePage(int offset)
    {
        if (archive == null || archive.UnlockedCount == 0) return;
        page = Mathf.Clamp(page + offset, 0, archive.UnlockedCount - 1);
        Refresh();
    }

    public void ShowAcquired()
    {
        if (archive == null || archive.UnlockedCount == 0) return;
        var entry = archive.GetChapter(archive.UnlockedCount - 1);
        acquiredMessage = string.Format(Text("journal.acquired"), entry != null ? entry.title : "");
        acquiredUntil = Time.unscaledTime + acquiredNoticeSeconds;
        pickupAvailable = false;
        nearbySite = null;
        RefreshHint();
    }

    
public void SetFirstFragmentNearby(bool nearby)
    {
        if (firstFragmentNearby == nearby) return;
        firstFragmentNearby = nearby;
        RefreshHint();
    }

    
public void SetNearbySite(JournalFragmentSite site)
    {
        nearbySite = site;
        RefreshHint();
    }

    
public void SetPickupAvailable(bool available)
    {
        pickupAvailable = available;
        RefreshHint();
    }

    private void Refresh()
    {
        var entry = archive.GetChapter(page);
        if (entry == null) return;
        if (pageView != null) pageView.SetEntry(entry.title, entry.body, page + 1, archive.TotalCount);
        RefreshHint();
    }

    private void RefreshHint()
    {
        if (hintText == null) return;
        if (!InputEnabled)
        {
            hintText.gameObject.SetActive(false);
            return;
        }
        bool acquired = Time.unscaledTime < acquiredUntil;
        hintText.gameObject.SetActive(!IsOpen && (acquired || pickupAvailable || firstFragmentNearby || nearbySite != null || (archive != null && archive.UnlockedCount > 0)));
        hintText.text = acquired ? acquiredMessage : pickupAvailable ? Text("journal.pickup") :
            firstFragmentNearby || nearbySite != null ? Text("journal.nearby") : Text("journal.open");
    }

    private string Text(string key) { return textConfig != null ? textConfig.Get(key) : key; }
}
