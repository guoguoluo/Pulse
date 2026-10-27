using UnityEngine;
using TMPro;

/// <summary>
/// DebugHUD — shows runtime system state.
/// Only active when isDebugEnabled = true (default) or DEVELOPMENT_BUILD.
/// Displays: GameState, BPM, Alignment, Terrain, Accent/Pause state.
/// Toggle with F1 (DebugToggle action).
///
/// Attached to DebugCanvas GameObject with child TMP text objects.
/// </summary>
public class DebugHUD : MonoBehaviour
{
    [Header("Enable")]
    [SerializeField] private bool isDebugEnabled = true;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI bpmText;
    [SerializeField] private TextMeshProUGUI alignmentText;
    [SerializeField] private TextMeshProUGUI terrainText;
    [SerializeField] private TextMeshProUGUI fragmentText;
    [SerializeField] private TextMeshProUGUI inputText;

    [Header("Runtime Ref")]
    [SerializeField] private PlayerController player;
    [SerializeField] private FootstepDetector footstepDetector;
    [SerializeField] private PulseMatcher pulseMatcher;
    [SerializeField] private GaitRecognizer gaitRecognizer;

    private bool debugToggle;
    private PlayerInputActions inputActions;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (footstepDetector == null) footstepDetector = FindFirstObjectByType<FootstepDetector>();
        if (pulseMatcher == null) pulseMatcher = FindFirstObjectByType<PulseMatcher>();
        if (gaitRecognizer == null) gaitRecognizer = FindFirstObjectByType<GaitRecognizer>();

        inputActions = new PlayerInputActions();
        inputActions.Enable();
        inputActions.Player["DebugToggle"].performed += OnDebugToggle;

#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        isDebugEnabled = false;
#endif
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Player["DebugToggle"].performed -= OnDebugToggle;
            inputActions.Disable();
            inputActions.Dispose();
        }
    }

    private void OnDebugToggle(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            isDebugEnabled = !isDebugEnabled;
        }
    }

    private void Update()
    {
        if (!isDebugEnabled) return;

        if (stateText != null)
            stateText.text = "State: " + GameManager.Instance?.CurrentState;

        if (bpmText != null && footstepDetector != null)
            bpmText.text = "Step BPM: " + footstepDetector.CurrentStepBPM.ToString("F1");

        if (alignmentText != null && pulseMatcher != null)
        {
            float align = pulseMatcher.CurrentAlignment;
            float targetBPM = pulseMatcher.ActiveProfile != null ? pulseMatcher.ActiveProfile.BPM : 0f;
            alignmentText.text = "Align: " + align.ToString("F2") + " | Target: " + targetBPM.ToString("F0") + " BPM";
        }

        if (terrainText != null && footstepDetector != null)
            terrainText.text = "Terrain: " + footstepDetector.CurrentTerrain;

        if (fragmentText != null && gaitRecognizer != null)
            fragmentText.text = "Node Zone: " + (gaitRecognizer.IsInNodeZone ? "IN" : "OUT");

        if (inputText != null && player != null)
        {
            string extra = player.IsMoving ? "[MOVE]" : "[IDLE]";
            inputText.text = extra;
        }
    }
}
