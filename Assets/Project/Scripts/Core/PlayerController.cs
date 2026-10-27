using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person CharacterController movement for the prototype.
/// Keeps a few legacy properties so existing debug/footstep scripts still compile.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVerticalSpeed = -2f;
    [SerializeField] private float stairSnapForce = 6f;
    [SerializeField] private float groundedStickForce = 3f;
    [SerializeField] private float maxStepAssistHeight = 0.45f;
    [SerializeField] private float stepAssistForwardCheck = 0.4f;
    [SerializeField] private float stepAssistLiftSpeed = 6f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float gamepadSensitivity = 120f;
    [SerializeField] private float pitchClamp = 70f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraPivot;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float cameraPitch;
    private float verticalVelocity;
    private bool suppressNextMouseLook;

    public bool IsMoving { get; private set; }
    public Vector3 PlanarVelocity { get; private set; }
    public float NormalizedMoveSpeed => moveSpeed > 0.001f ? Mathf.Clamp01(PlanarVelocity.magnitude / moveSpeed) : 0f;
    public Vector3 Velocity => controller != null ? controller.velocity : Vector3.zero;

    // Legacy compatibility for existing scripts in the repo.
    public bool IsAccenting => false;
    public bool IsPaused => false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player["Move"].performed += OnMove;
        inputActions.Player["Move"].canceled += OnMove;
        inputActions.Player["Look"].performed += OnLook;
        inputActions.Player["Look"].canceled += OnLook;
    }

    private void OnDisable()
    {
        inputActions.Player["Move"].performed -= OnMove;
        inputActions.Player["Move"].canceled -= OnMove;
        inputActions.Player["Look"].performed -= OnLook;
        inputActions.Player["Look"].canceled -= OnLook;
        inputActions.Disable();
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (cameraPivot == null && playerCamera != null)
            cameraPivot = playerCamera.transform.parent != null ? playerCamera.transform.parent : playerCamera.transform;

        if (cameraPivot != null)
            cameraPitch = NormalizePitch(cameraPivot.localEulerAngles.x);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        suppressNextMouseLook = true;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void HandleLook()
    {
        if (lookInput.sqrMagnitude < 0.0001f)
            return;

        bool useGamepad = inputActions.Player["Look"].activeControl != null
            && inputActions.Player["Look"].activeControl.device is Gamepad;

        if (!useGamepad && suppressNextMouseLook)
        {
            suppressNextMouseLook = false;
            return;
        }

        float sensitivity = useGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity;
        float yawDelta = lookInput.x * sensitivity;
        float pitchDelta = lookInput.y * sensitivity;

        cameraPitch -= pitchDelta;
        cameraPitch = Mathf.Clamp(cameraPitch, -pitchClamp, pitchClamp);

        transform.Rotate(Vector3.up * yawDelta, Space.World);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector3 forward = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        Vector3 right = playerCamera != null ? playerCamera.transform.right : transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMove = forward * moveInput.y + right * moveInput.x;
        if (desiredMove.sqrMagnitude > 1f)
            desiredMove.Normalize();

        IsMoving = desiredMove.sqrMagnitude > 0.001f;

        if (IsMoving)
            TryStepAssist(desiredMove);

        if (controller.isGrounded)
        {
            // Keep the controller pressed into step surfaces so staircase traversal feels stable.
            verticalVelocity = desiredMove.sqrMagnitude > 0.001f ? -stairSnapForce : -groundedStickForce;
            if (verticalVelocity > groundedVerticalSpeed)
                verticalVelocity = groundedVerticalSpeed;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = desiredMove * moveSpeed;
        PlanarVelocity = new Vector3(motion.x, 0f, motion.z);
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    private void TryStepAssist(Vector3 moveDirection)
    {
        if (controller == null || !controller.isGrounded)
            return;

        Vector3 origin = transform.position + controller.center;
        origin.y += (-controller.height * 0.5f) + controller.radius + 0.05f;

        if (!Physics.Raycast(origin, moveDirection, stepAssistForwardCheck, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        Vector3 upperOrigin = origin + Vector3.up * maxStepAssistHeight;
        if (Physics.Raycast(upperOrigin, moveDirection, stepAssistForwardCheck, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        controller.Move(Vector3.up * stepAssistLiftSpeed * Time.deltaTime);
    }

    private static float NormalizePitch(float pitch)
    {
        if (pitch > 180f)
            pitch -= 360f;

        return pitch;
    }
}
