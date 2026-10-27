using UnityEngine;

/// <summary>
/// FootstepDetector — attached to Player.
/// Detects footsteps based on horizontal displacement (stepDistance).
/// Exposes step history for GaitRecognizer and provides StepAccent modifier.
///
/// [Config] stepDistance: horizontal distance per step (default 1.5m)
/// [Config] minStepInterval: minimum seconds between steps (default 0.2s)
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class FootstepDetector : MonoBehaviour
{
    [Header("Step Rhythm")]
    [Tooltip("Horizontal distance per step (meters)")]
    [SerializeField] private float stepDistance = 1.5f;
    [Tooltip("Minimum seconds between consecutive steps")]
    [SerializeField] private float minStepInterval = 0.2f;

    [Header("Accent")]
    [Tooltip("Step distance multiplier when accenting (heavy step)")]
    [SerializeField] private float accentDistanceMult = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("History")]
    [Tooltip("Max number of step timestamps to keep for gait analysis")]
    [SerializeField] private int maxHistoryEntries = 32;

    private PlayerController playerController;
    private Vector3 lastStepPosition;
    private float lastStepTime;
    private float distanceAccumulator;

    public float CurrentStepBPM { get; private set; }
    public GroundMaterial.TerrainType CurrentTerrain { get; private set; } = GroundMaterial.TerrainType.Metal;

    /// <summary>
    /// Ring buffer of recent step timestamps (most recent first).
    /// Used by GaitRecognizer to analyse footstep patterns.
    /// </summary>
    public System.Collections.Generic.List<float> StepHistory { get; private set; } = new System.Collections.Generic.List<float>();

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        lastStepPosition = transform.position;
        lastStepTime = Time.time;
    }

    private void Update()
    {
        if (!playerController.IsMoving) return;

        Vector3 currentPos = transform.position;
        Vector3 delta = currentPos - lastStepPosition;
        delta.y = 0f;
        distanceAccumulator += delta.magnitude;
        lastStepPosition = currentPos;

        float effectiveDistance = playerController.IsAccenting
            ? stepDistance * accentDistanceMult
            : stepDistance;

        if (distanceAccumulator >= effectiveDistance && Time.time - lastStepTime >= minStepInterval)
        {
            distanceAccumulator = 0f;
            lastStepTime = Time.time;
            OnStep();
        }

        float instantBPM = 60f / Mathf.Max(Time.time - lastStepTime, 0.1f);
        CurrentStepBPM = Mathf.Lerp(CurrentStepBPM, instantBPM, 10f * Time.deltaTime);
    }

    private void OnStep()
    {
        DetectTerrain();

        StepHistory.Insert(0, Time.time);
        if (StepHistory.Count > maxHistoryEntries)
            StepHistory.RemoveAt(StepHistory.Count - 1);

        FootstepEventBus.Publish(new FootstepEvent
        {
            position = transform.position,
            terrain = CurrentTerrain,
            timestamp = Time.time,
            isAccent = playerController.IsAccenting
        });
    }

    private void DetectTerrain()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down,
                out RaycastHit hit, groundCheckDistance + 0.8f, groundLayer))
        {
            var mat = hit.collider.GetComponent<GroundMaterial>();
            if (mat != null)
                CurrentTerrain = mat.CurrentTerrainType;
        }
    }
}
