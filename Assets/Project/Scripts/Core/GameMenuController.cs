using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMenuController : MonoBehaviour
{
    [SerializeField] private RectTransform menuParent;
    [SerializeField] private GameObject mainMenuPrefab;
    [SerializeField] private GameObject pauseMenuPrefab;
    [SerializeField] private GameObject manualPrefab;
    [SerializeField] private GameObject creditsPrefab;
    [SerializeField] private AudioClip creditsMusic;
    [Range(0f, 1f)] [SerializeField] private float creditsMusicVolume = 0.7f;
    [SerializeField] private JournalArchive archive;
    [SerializeField] private JournalReader journalReader;
    [SerializeField] private PlayerController player;
    [SerializeField] private DownstrokeInput downstrokeInput;
    [SerializeField] private ConductorDemo conductor;
    [SerializeField] private FirstBeatTimeline timeline;
    [SerializeField] private GestureDrivenMusic gestureMusic;
    [SerializeField] private GuideMeteorOverlay guideMeteor;
    [SerializeField] private AudioSource[] gameplayAudio;

    private GameObject currentPage;
    private AudioSource creditsSource;
    private bool ending;
    private bool playing;
    private float previousTimeScale = 1f;
    private readonly System.Collections.Generic.List<AudioSource> pausedAudio = new System.Collections.Generic.List<AudioSource>();
    private float lastEscapeTime = -10f;
    public bool IsGameplayActive => playing && currentPage == null;
    public bool IsMenuOpen => currentPage != null;

    private void Awake()
    {
        if (archive == null) archive = FindFirstObjectByType<JournalArchive>();
        if (journalReader == null) journalReader = FindFirstObjectByType<JournalReader>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (downstrokeInput == null) downstrokeInput = FindFirstObjectByType<DownstrokeInput>();
        if (conductor == null) conductor = FindFirstObjectByType<ConductorDemo>();
        if (timeline == null) timeline = FindFirstObjectByType<FirstBeatTimeline>();
        if (gestureMusic == null) gestureMusic = FindFirstObjectByType<GestureDrivenMusic>();
        if (guideMeteor == null) guideMeteor = FindFirstObjectByType<GuideMeteorOverlay>();
        if (menuParent == null) menuParent = transform as RectTransform;
        if (StartGameAfterReload.pending)
        {
            StartGameAfterReload.pending = false;
            BeginGameplay();
        }
        else ShowMainMenu();
    }

    private void Update()
    {
        if (currentPage != null && (Cursor.lockState != CursorLockMode.None || !Cursor.visible))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (Time.unscaledTime - lastEscapeTime < 0.15f) return;
        lastEscapeTime = Time.unscaledTime;
        if (journalReader != null && journalReader.WasOpenThisFrame)
        {
            if (journalReader.IsOpen) journalReader.Close();
            return;
        }
        if (currentPage != null)
        {
            if (ending) return;
            if (currentPage.name.StartsWith("Manual")) CloseManual();
            else if (playing) Resume();
        }
        else if (playing) Pause();
    }

    private void Show(GameObject prefab)
    {
        if (currentPage != null) Destroy(currentPage);
        currentPage = prefab != null ? Instantiate(prefab, menuParent, false) : null;
        if (currentPage == null) return;
        currentPage.SetActive(true);
        Bind("NewGame", NewGame);
        Bind("Continue", ContinueGame);
        Bind("Tutorial", OpenManual);
        Bind("Resume", Resume);
        Bind("MainMenu", ReturnToMainMenu);
        Bind("Back", CloseManual);
        Bind("Quit", Quit);
        Bind("CreditsMainMenu", ReturnToMainMenu);
        var continueButton = FindButton("Continue");
        if (continueButton != null) continueButton.interactable = archive != null && archive.HasSavedProgress;
    }

    private Button FindButton(string name)
    {
        if (currentPage == null) return null;
        foreach (var button in currentPage.GetComponentsInChildren<Button>(true))
            if (button.name == name) return button;
        return null;
    }

    private void Bind(string name, UnityEngine.Events.UnityAction action)
    {
        var button = FindButton(name);
        if (button != null) button.onClick.AddListener(action);
    }

    private void SetMenuState(bool menuOpen)
    {
        if (player != null) player.SetJournalOpen(menuOpen);
        if (journalReader != null) journalReader.SetInputEnabled(!menuOpen);
        if (conductor != null) conductor.SetScoringPaused(menuOpen);
        if (timeline != null) timeline.SetPaused(menuOpen);
        if (downstrokeInput != null) downstrokeInput.SetGameplayInputEnabled(!menuOpen);
        if (gestureMusic != null)
        {
            gestureMusic.SetMenuPaused(menuOpen);
            gestureMusic.enabled = !menuOpen;
        }
        if (guideMeteor != null) guideMeteor.SetPaused(menuOpen);
        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuOpen;
        if (menuOpen)
        {
            if (pausedAudio.Count == 0)
            {
                var sources = gameplayAudio != null && gameplayAudio.Length > 0
                    ? gameplayAudio : FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
                foreach (var source in sources)
                    if (source != null && source.isPlaying) { source.Pause(); pausedAudio.Add(source); }
            }
        }
        else
        {
            foreach (var source in pausedAudio)
                if (source != null) source.UnPause();
            pausedAudio.Clear();
        }
    }

    public void SetJournalState(bool open)
    {
        if (!playing || IsMenuOpen) return;
        if (conductor != null) conductor.SetScoringPaused(open);
        if (timeline != null) timeline.SetPaused(open);
        if (downstrokeInput != null) downstrokeInput.SetGameplayInputEnabled(!open);
        if (guideMeteor != null) guideMeteor.SetJournalHidden(open);
        if (timeline != null && timeline.Tuning != null &&
            timeline.Tuning.musicMode == ConductorTuning.MusicMode.Timeline)
        {
            var recording = timeline.RecordingSource;
            if (recording != null)
            {
                if (open) recording.Pause();
                else recording.UnPause();
            }
        }
    }

    public void ShowMainMenu()
    {
        ending = false;
        StopCreditsMusic();
        playing = false;
        Time.timeScale = 0f;
        SetMenuState(true);
        Show(mainMenuPrefab);
    }

    public void NewGame()
    {
        if (archive != null) archive.ResetProgress();
        RestartGameplay();
    }

    public void ContinueGame()
    {
        if (archive == null || !archive.HasSavedProgress) return;
        RestartGameplay();
    }

    private void RestartGameplay()
    {
        Time.timeScale = 1f;
        StartGameAfterReload.pending = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BeginGameplay()
    {
        ending = false;
        playing = true;
        Time.timeScale = 1f;
        if (currentPage != null) Destroy(currentPage);
        currentPage = null;
        SetMenuState(false);
    }

    public void Pause()
    {
        if (!playing) return;
        if (journalReader != null && journalReader.IsOpen) journalReader.Close();
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        SetMenuState(true);
        Show(pauseMenuPrefab);
    }

    public void Resume()
    {
        if (!playing) return;
        Time.timeScale = previousTimeScale;
        if (currentPage != null) Destroy(currentPage);
        currentPage = null;
        SetMenuState(false);
    }

    public void ReturnToMainMenu()
    {
        StopCreditsMusic();
        ending = false;
        Time.timeScale = 1f;
        StartGameAfterReload.pending = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OpenManual()
    {
        SetMenuState(true);
        Show(manualPrefab);
    }

    public void CloseManual()
    {
        Show(playing ? pauseMenuPrefab : mainMenuPrefab);
    }

    public void EnterHibernation()
    {
        if (!playing || ending || archive == null || archive.TotalCount == 0 ||
            archive.UnlockedCount != archive.TotalCount || creditsPrefab == null)
            return;
        ending = true;
        playing = false;
        Time.timeScale = 0f;
        SetMenuState(true);
        Show(creditsPrefab);
        if (creditsMusic != null)
        {
            if (creditsSource == null) creditsSource = gameObject.AddComponent<AudioSource>();
            creditsSource.playOnAwake = false;
            creditsSource.loop = true;
            creditsSource.ignoreListenerPause = true;
            creditsSource.clip = creditsMusic;
            creditsSource.volume = creditsMusicVolume;
            creditsSource.Play();
        }
    }

    private void StopCreditsMusic()
    {
        if (creditsSource != null) creditsSource.Stop();
    }

    public void Quit() { Application.Quit(); }
}

public static class StartGameAfterReload
{
    public static bool pending;
}
