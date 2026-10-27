using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NodeTriggerManager — manages node event triggering.
/// When NarrativeManager finishes the last fragment, TriggerNode() is called.
/// Also listens to GaitRecognizer for alternative unlock.
///
/// [Config] requiredAlignmentsBeforeNode: success alignments needed before node fires
/// </summary>
public class NodeTriggerManager : MonoBehaviour
{
    public static NodeTriggerManager Instance { get; private set; }

    [Header("Node Configuration")]
    [Tooltip("Number of successful alignments needed before node can fire")]
    [SerializeField] private int requiredAlignmentsBeforeNode = 4;

    [Header("Gait Recognition")]
    [Tooltip("If true, gait recognition can also trigger a node (alternative path)")]
    [SerializeField] private bool enableGaitTrigger = true;

    [Header("Events")]
    public UnityEvent OnNodeTriggered;

    private int alignmentCount;
    private bool nodeHasTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Wire GaitRecognizer if present
        GaitRecognizer gr = FindFirstObjectByType<GaitRecognizer>();
        if (gr != null && enableGaitTrigger)
        {
            gr.OnGaitRecognized.AddListener(OnGaitRecognizedHandler);
        }
    }

    /// <summary>
    /// Register a completed alignment (narration fragment successfully played).
    /// </summary>
    public void RegisterAlignment()
    {
        alignmentCount++;
        Debug.Log("[NodeTriggerManager] Alignments: " + alignmentCount + "/" + requiredAlignmentsBeforeNode);
    }

    /// <summary>
    /// Called by NarrativeManager when the last fragment finishes playing.
    /// </summary>
    public void TriggerNode(string nodeName)
    {
        if (nodeHasTriggered) return;

        if (alignmentCount < requiredAlignmentsBeforeNode)
        {
            Debug.Log("[NodeTriggerManager] Not enough alignments yet (" + alignmentCount + "/" + requiredAlignmentsBeforeNode + ")");
            return;
        }

        FireNode(nodeName);
    }

    /// <summary>
    /// Alternative trigger: gait recognition bypasses alignment count.
    /// </summary>
    private void OnGaitRecognizedHandler()
    {
        if (nodeHasTriggered) return;
        alignmentCount = requiredAlignmentsBeforeNode; // force unlock
        FireNode("GaitRecognized");
    }

    private void FireNode(string nodeName)
    {
        nodeHasTriggered = true;
        GameManager.Instance?.SetEventTriggered();
        OnNodeTriggered.Invoke();
        Debug.Log("[NodeTriggerManager] Node triggered: '" + nodeName + "' — environment reveal started.");
    }
}
