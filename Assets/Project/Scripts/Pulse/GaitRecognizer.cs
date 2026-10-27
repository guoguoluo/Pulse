using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// GaitRecognizer — mounted on Player.
/// In a node zone, collects recent footstep intervals and checks
/// whether the player's "gait signature" matches a target pattern.
///
/// A "gait" is the rhythm of the last N steps (inter-step intervals).
/// When the player enters a NodeZone, GaitRecognizer watches the
/// StepHistory from FootstepDetector and attempts to match.
///
/// This provides an alternative unlock path: the player learns to
/// "walk the pulse" rather than just holding BPM steady.
/// </summary>
public class GaitRecognizer : MonoBehaviour
{
    [Header("Recognition")]
    [Tooltip("How many seconds to look back for step history")]
    [SerializeField] private float historyWindow = 3f;
    [Tooltip("Minimum number of steps needed in the window to attempt recognition")]
    [SerializeField] private int minStepsForRecognition = 3;
    [Tooltip("Maximum mean deviation (seconds) between expected intervals and actual")]
    [SerializeField] private float intervalTolerance = 0.15f;
    [Tooltip("Recognition check interval (s)")]
    [SerializeField] private float checkInterval = 0.5f;

    [Header("Events")]
    public UnityEvent OnGaitRecognized;

    private FootstepDetector footstepDetector;
    private PulseMatcher pulseMatcher;
    private bool isInNodeZone;
    private float lastCheckTime;
    private float zoneEnterTime;

    private void Awake()
    {
        footstepDetector = GetComponent<FootstepDetector>();
        if (footstepDetector == null)
            footstepDetector = FindFirstObjectByType<FootstepDetector>();
        pulseMatcher = GetComponent<PulseMatcher>();
        if (pulseMatcher == null)
            pulseMatcher = FindFirstObjectByType<PulseMatcher>();
    }

    private void Update()
    {
        if (!isInNodeZone) return;
        if (Time.time - lastCheckTime < checkInterval) return;
        lastCheckTime = Time.time;

        if (footstepDetector == null) return;
        List<float> history = footstepDetector.StepHistory;
        if (history.Count < minStepsForRecognition) return;

        // Collect steps within the history window
        float cutoff = Time.time - historyWindow;
        List<float> recentSteps = new List<float>();
        for (int i = 0; i < history.Count; i++)
        {
            if (history[i] >= cutoff)
                recentSteps.Add(history[i]);
            else
                break;
        }
        recentSteps.Reverse(); // oldest first
        if (recentSteps.Count < minStepsForRecognition) return;

        // Compute actual intervals from recorded timestamps
        List<float> intervals = new List<float>();
        for (int i = 1; i < recentSteps.Count; i++)
            intervals.Add(recentSteps[i] - recentSteps[i - 1]);

        // Expected interval from current PulseProfile
        if (pulseMatcher == null || pulseMatcher.ActiveProfile == null) return;
        float expected = pulseMatcher.ActiveProfile.BeatInterval;

        // Mean absolute deviation check
        float sumDev = 0f;
        foreach (float iv in intervals)
            sumDev += Mathf.Abs(iv - expected);
        float meanDev = sumDev / intervals.Count;

        if (meanDev <= intervalTolerance)
        {
            Debug.Log("[GaitRecognizer] Gait pattern matched — triggering recognition.");
            isInNodeZone = false; // fire only once per zone entry
            OnGaitRecognized.Invoke();
        }
    }

    /// <summary>
    /// Called by NodeZone when player enters.
    /// </summary>
    public void EnterNodeZone()
    {
        isInNodeZone = true;
        zoneEnterTime = Time.time;
        lastCheckTime = Time.time;
        Debug.Log("[GaitRecognizer] Entered node zone — watching step patterns.");
    }

    /// <summary>
    /// Called by NodeZone when player exits.
    /// </summary>
    public void ExitNodeZone()
    {
        isInNodeZone = false;
        Debug.Log("[GaitRecognizer] Exited node zone.");
    }

    public bool IsInNodeZone { get { return isInNodeZone; } }
}
