using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SphereCollider))]
public class JournalFragmentSite : MonoBehaviour
{
    public enum PuzzleMode { Accumulate, Consecutive }

    [SerializeField] private JournalArchive archive;
    [SerializeField] private JournalReader reader;
    [Tooltip("Unique fragment identity (0-5), not the chapter shown when collected.")]
    [Range(0, 5)] [SerializeField] private int unlocksChapterIndex;
    [Tooltip("Pickup distance from the fragment; independent of the puzzle activation radius.")]
    [Min(0f)] [SerializeField] private float pickupDistance = 2.5f;
    [SerializeField] private float interactionDistance = 3f;
    [Tooltip("Optional reachable-side center for the route puzzle. The fragment pickup remains centered on its visual.")]
    [SerializeField] private Transform puzzleActivationPoint;
    [SerializeField] private GameObject visual;
    [SerializeField] private GameObject[] activateOnUnlock;
    [Tooltip("Optional animated route. A solved puzzle triggers its Raise animation.")]
    [SerializeField] private RisingStaircase animatedRoute;
    [SerializeField] private bool requireBeatPuzzle = true;
    [Min(1)] [SerializeField] private int requiredHits = 3;
    [SerializeField] private ConductorGestureRating minimumRating = ConductorGestureRating.Good;
    [Header("Animation Trigger")]
    [SerializeField] private PuzzleMode puzzleMode = PuzzleMode.Accumulate;

    private DownstrokeInput input;
    private FirstBeatTimeline timeline;
    private Transform player;
    private PlayerController playerController;
    private int hits;
    private int lastBeat = -1;
    private bool collected;
    private bool solved;
    private ConductorTuning tuning;
    private ConductorTuning.FragmentPuzzle Puzzle => tuning != null ? tuning.GetFragmentPuzzle(unlocksChapterIndex) : null;
    private float InteractionDistance => Puzzle != null ? Puzzle.interactionDistance : interactionDistance;
    private float PickupDistance => pickupDistance;
    private bool RequiresBeatPuzzle => Puzzle != null ? Puzzle.requireBeatPuzzle : requireBeatPuzzle;
    private int RequiredHits => Puzzle != null ? Mathf.Max(1, Puzzle.requiredHits) : Mathf.Max(1, requiredHits);
    private ConductorGestureRating MinimumRating => Puzzle != null ? Puzzle.minimumRating : minimumRating;
    private PuzzleMode Mode => Puzzle != null ? Puzzle.mode : puzzleMode;

    private void Awake()
    {
        if (archive == null) archive = FindFirstObjectByType<JournalArchive>();
        if (reader == null) reader = FindFirstObjectByType<JournalReader>();
        input = FindFirstObjectByType<DownstrokeInput>();
        timeline = FindFirstObjectByType<FirstBeatTimeline>();
        playerController = FindFirstObjectByType<PlayerController>();
        if (timeline != null) tuning = timeline.Tuning;
        GetComponent<SphereCollider>().isTrigger = true;
    }

    private void OnEnable()
    {
        if (input == null) input = FindFirstObjectByType<DownstrokeInput>();
        if (input != null) input.Downstroke += OnDownstroke;
    }

    private void OnDisable()
    {
        if (input != null) input.Downstroke -= OnDownstroke;
        if (reader != null)
        {
            reader.ClearPickupTarget(visual != null ? visual.transform : transform);
            reader.SetNearbySite(null);
        }
    }

    private void OnValidate()
    {
        requiredHits = Mathf.Max(1, requiredHits);
    }

    private void Start()
    {
        if (archive != null && archive.IsFragmentCollected(unlocksChapterIndex))
        {
            if (animatedRoute != null) animatedRoute.RestoreRaised();
            foreach (var target in activateOnUnlock)
                if (target != null) target.SetActive(true);
            CollectVisual();
        }
    }

    private void Update()
    {
        if (collected || archive == null || reader == null) return;
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null) return;
        player = playerController.transform;
        bool near = HorizontalDistance(player.position, transform.position) <= PickupDistance;
        if (near && !reader.IsOpen)
            reader.SetPickupTarget(visual != null ? visual.transform : transform, true);
        else
            reader.ClearPickupTarget(visual != null ? visual.transform : transform);
        if (near && !reader.IsOpen) reader.SetPickupAvailable(true);
        if (near && !reader.IsOpen && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (archive.CollectFragment(unlocksChapterIndex))
            {
                foreach (var target in activateOnUnlock) if (target != null) target.SetActive(true);
                CollectVisual();
                reader.ShowAcquired();
            }
        }
    }

    private void OnDownstroke(double dspTime)
    {
        if (!RequiresBeatPuzzle || solved || player == null || timeline == null) return;
        if (HorizontalDistance(player.position,
            puzzleActivationPoint != null ? puzzleActivationPoint.position : transform.position) > InteractionDistance) return;
        // Progress is persistent within a visit; missed beats never erase it.
        float window = tuning != null ? tuning.goodWindowSeconds : 0.3f;
        if (!timeline.TryMatch(dspTime, window, out var beat, out var offset) || beat == lastBeat) return;
        float perfect = tuning != null ? tuning.perfectWindowSeconds : 0.08f;
        float great = tuning != null ? tuning.greatWindowSeconds : 0.17f;
        var rating = Mathf.Abs(offset) <= perfect ? ConductorGestureRating.Perfect
            : Mathf.Abs(offset) <= great ? ConductorGestureRating.Great : ConductorGestureRating.Good;
        if (rating < MinimumRating) return;
        if (Mode == PuzzleMode.Consecutive && lastBeat >= 0 && beat != lastBeat + 1)
        {
            lastBeat = beat;
            return;
        }
        lastBeat = beat;
        hits = Mathf.Min(RequiredHits, hits + 1);
        if (hits >= RequiredHits)
        {
            solved = true;
            if (animatedRoute != null) animatedRoute.Raise();
            foreach (var target in activateOnUnlock) if (target != null) target.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) player = other.transform;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform != player) return;
        if (reader != null)
        {
            reader.ClearPickupTarget(visual != null ? visual.transform : transform);
            reader.SetNearbySite(null);
        }
    }

    private void CollectVisual()
    {
        collected = true;
        if (visual != null) visual.SetActive(false);
        else if (TryGetComponent<MeshRenderer>(out var renderer)) renderer.enabled = false;
        if (reader != null)
        {
            reader.ClearPickupTarget(visual != null ? visual.transform : transform);
            reader.SetNearbySite(null);
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}
