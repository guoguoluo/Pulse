using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SphereCollider))]
public class JournalFragmentSite : MonoBehaviour
{
    public enum PuzzleMode { Accumulate, Consecutive, AlternateSides, VisitAnchors }

    [SerializeField] private JournalArchive archive;
    [SerializeField] private JournalReader reader;
    [Range(0, 5)] [SerializeField] private int unlocksChapterIndex;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private GameObject visual;
    [SerializeField] private GameObject[] activateOnUnlock;
    [SerializeField] private bool requireBeatPuzzle = true;
    [Min(1)] [SerializeField] private int requiredHits = 3;
    [SerializeField] private ConductorGestureRating minimumRating = ConductorGestureRating.Good;
    [Min(0f)] [SerializeField] private float resetAfterSeconds = 9f;
    [Header("Puzzle Pattern")]
    [SerializeField] private PuzzleMode puzzleMode = PuzzleMode.Accumulate;
    [SerializeField] private Transform[] anchors;
    [Min(0.5f)] [SerializeField] private float anchorRadius = 5f;
    [SerializeField] private Renderer[] responseRenderers;
    [SerializeField] private Light responseLight;
    [SerializeField] private JournalPuzzleArt puzzleArt;
    [SerializeField] private Color responseColor = new Color(0.12f, 0.7f, 1f);
    [Min(0f)] [SerializeField] private float responseIntensity = 1f;

    private DownstrokeInput input;
    private FirstBeatTimeline timeline;
    private Transform player;
    private int hits;
    private int lastBeat = -1;
    private float lastHitTime;
    private bool collected;
    private int lastSide;
    private int anchorIndex;
    private bool solved;
    private MaterialPropertyBlock responseProperties;
    private float responsePulse;

    private void Awake()
    {
        if (archive == null) archive = FindFirstObjectByType<JournalArchive>();
        if (reader == null) reader = FindFirstObjectByType<JournalReader>();
        input = FindFirstObjectByType<DownstrokeInput>();
        timeline = FindFirstObjectByType<FirstBeatTimeline>();
        GetComponent<SphereCollider>().isTrigger = true;
        responseProperties = new MaterialPropertyBlock();
        if (responseLight != null) responseLight.intensity = 0f;
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
            reader.SetPickupAvailable(false);
            reader.SetNearbySite(null);
        }
    }

    private void OnValidate()
    {
        requiredHits = Mathf.Max(1, requiredHits);
        if (puzzleMode == PuzzleMode.VisitAnchors && anchors != null && anchors.Length > 0)
            requiredHits = anchors.Length;
    }

    private void Start()
    {
        if (archive != null && archive.UnlockedCount > unlocksChapterIndex)
        {
            if (puzzleArt != null) puzzleArt.SetComplete();
            CollectVisual();
        }
    }

    private void Update()
    {
        if (collected || archive == null || reader == null) return;
        responsePulse = Mathf.MoveTowards(responsePulse, 0f, Time.deltaTime * 1.5f);
        UpdateResponse();
        if (player == null) return;
        bool near = Vector3.Distance(player.position, transform.position) <= interactionDistance;
        bool next = archive.UnlockedCount == unlocksChapterIndex;
        bool ready = !requireBeatPuzzle || solved;
        reader.SetNearbySite(near && next && !ready ? this : null);
        reader.SetPickupAvailable(near && next && ready && !reader.IsOpen);
        if (near && next && ready && !reader.IsOpen && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (archive.CollectNext())
            {
                foreach (var target in activateOnUnlock) if (target != null) target.SetActive(true);
                CollectVisual();
                reader.ShowAcquired();
            }
        }
    }

    private void UpdateResponse()
    {
        if (responseRenderers != null)
            for (int i = 0; i < responseRenderers.Length; i++)
            {
                var renderer = responseRenderers[i];
                if (renderer == null) continue;
                renderer.GetPropertyBlock(responseProperties);
                float lit = i < hits ? 1f : 0.05f;
                responseProperties.SetColor("_EmissionColor", responseColor * (lit + responsePulse * 0.5f));
                renderer.SetPropertyBlock(responseProperties);
            }
        if (responseLight != null)
            responseLight.intensity = Mathf.Lerp(responseLight.intensity,
                responseIntensity * (hits / Mathf.Max(1f, requiredHits) + responsePulse * 0.25f), Time.deltaTime * 3f);
    }

    private void OnDownstroke(double dspTime)
    {
        if (!requireBeatPuzzle || solved || collected || player == null || archive == null
            || archive.UnlockedCount != unlocksChapterIndex || timeline == null) return;
        if (Vector3.Distance(player.position, transform.position) > interactionDistance) return;
        // Progress is persistent within a visit; missed beats never erase it.
        if (!timeline.TryMatch(dspTime, .3f, out var beat, out var offset) || beat == lastBeat) return;
        var rating = Mathf.Abs(offset) <= .08f ? ConductorGestureRating.Perfect : Mathf.Abs(offset) <= .17f ? ConductorGestureRating.Great : ConductorGestureRating.Good;
        if (rating < minimumRating) return;
        if (puzzleMode == PuzzleMode.Consecutive && lastBeat >= 0 && beat != lastBeat + 1)
        {
            lastBeat = beat;
            responsePulse = 1f;
            return;
        }
        if (puzzleMode == PuzzleMode.AlternateSides)
        {
            int side = Vector3.Dot(player.position - transform.position, transform.right) < 0f ? -1 : 1;
            if (side == lastSide) return;
            lastSide = side;
        }
        if (puzzleMode == PuzzleMode.VisitAnchors && anchors != null && anchors.Length > 0)
        {
            var anchor = anchors[anchorIndex % anchors.Length];
            if (anchor == null || Vector3.Distance(player.position, anchor.position) > anchorRadius) return;
            anchorIndex++;
        }
        lastBeat = beat;
        lastHitTime = Time.time;
        hits = Mathf.Min(requiredHits, hits + 1);
        responsePulse = 1f;
        if (puzzleArt != null) puzzleArt.SetProgress(hits, requiredHits);
        if (hits >= requiredHits)
        {
            solved = true;
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
        player = null;
        if (reader != null)
        {
            reader.SetPickupAvailable(false);
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
            reader.SetPickupAvailable(false);
            reader.SetNearbySite(null);
        }
    }
}
