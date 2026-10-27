using UnityEngine;

/// <summary>
/// Drives the imported astronaut animator from the prototype player movement.
/// </summary>
public class PlayerAnimatorBridge : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "AnimationPar";
    [SerializeField] private float idleValue = 0f;
    [SerializeField] private float runValue = 1f;
    [SerializeField] private float dampTime = 0.1f;
    [SerializeField] private float moveThreshold = 0.05f;

    private int speedParameterHash;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        speedParameterHash = Animator.StringToHash(speedParameter);
    }

    private void Update()
    {
        if (playerController == null || animator == null)
            return;

        int targetValue = playerController.NormalizedMoveSpeed > moveThreshold ? Mathf.RoundToInt(runValue) : Mathf.RoundToInt(idleValue);
        animator.SetInteger(speedParameterHash, targetValue);
    }
}
