using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger placed at the elevated destination.
/// </summary>
public class GoalBeacon : MonoBehaviour
{
    [SerializeField] private UnityEvent onGoalReached;
    [SerializeField] private Light goalLight;
    [SerializeField] private Color reachedColor = Color.white;

    private bool reached;

    private void OnTriggerEnter(Collider other)
    {
        if (reached)
            return;

        if (other.GetComponent<PlayerController>() == null)
            return;

        reached = true;
        if (goalLight != null)
            goalLight.color = reachedColor;

        onGoalReached?.Invoke();
    }
}
