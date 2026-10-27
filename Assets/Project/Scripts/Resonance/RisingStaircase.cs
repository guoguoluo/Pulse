using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Raises a blocked path when resonance succeeds.
/// </summary>
public class RisingStaircase : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator staircaseAnimator;
    [SerializeField] private string raiseTrigger = "Raise";
    [SerializeField] private bool invokeRaisedImmediately = true;

    [Header("Feedback")]
    [SerializeField] private AudioSource movementSource;
    [SerializeField] private AudioClip riseClip;
    [SerializeField] private Light feedbackLight;
    [SerializeField] private Color successColor = new Color(0.7f, 0.95f, 1f, 1f);
    [SerializeField] private UnityEvent onRaised;

    private bool raised;
    private int raiseTriggerHash;

    private void Reset()
    {
        staircaseAnimator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (staircaseAnimator == null)
            staircaseAnimator = GetComponent<Animator>();

        raiseTriggerHash = Animator.StringToHash(raiseTrigger);
    }

    public void Raise()
    {
        if (raised)
            return;

        raised = true;
        if (movementSource != null && riseClip != null)
            movementSource.PlayOneShot(riseClip);

        if (feedbackLight != null)
            feedbackLight.color = successColor;

        if (staircaseAnimator != null && !string.IsNullOrWhiteSpace(raiseTrigger))
        {
            staircaseAnimator.ResetTrigger(raiseTriggerHash);
            staircaseAnimator.SetTrigger(raiseTriggerHash);
        }
        else
        {
            Debug.LogWarning($"{nameof(RisingStaircase)} on {name} has no Animator or raise trigger configured.", this);
        }

        if (invokeRaisedImmediately)
            onRaised?.Invoke();
    }

    // Call this from the staircase animation timeline or a StateMachineBehaviour
    // when the stairs have finished rising and downstream events should fire.
    public void NotifyRaised()
    {
        onRaised?.Invoke();
    }
}
