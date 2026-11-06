using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Converts a burst of relative mouse motion (or a future device adapter) to one beat action.</summary>
public class DownstrokeInput : MonoBehaviour
{
    [Header("Beat Motion Detection (any direction)")]
    [SerializeField] private ConductorTuning tuning;
    [Tooltip("Higher values react faster to mouse movement.")]
    [Min(0.1f)] [SerializeField] private float smoothing = 12f;
    [Tooltip("Filtered mouse speed in pixels per second needed to trigger, regardless of direction.")]
    [Min(0f)] [SerializeField] private float triggerSpeed = 420f;
    [Tooltip("Speed below which another beat action can be armed.")]
    [Min(0f)] [SerializeField] private float resetSpeed = 110f;
    [Min(0f)] [SerializeField] private float cooldown = 0.38f;
    [Tooltip("Mouse travel required within one motion burst.")]
    [Min(0f)] [SerializeField] private float minimumStrokePixels = 18f;

    [Header("Any Motion Feedback")]
    [Tooltip("Mouse travel in pixels per frame for a faint response.")]
    [Min(0f)] [SerializeField] private float motionThresholdPixels = 1f;
    [Min(0f)] [SerializeField] private float motionFeedbackCooldown = 0.12f;
    private float filteredSpeed;
    private float previousFilteredSpeed;
    private float strokeDistance;
    private float lastMotionSampleTime;
    private Vector2 previousDirection;
    private bool hasDirection;
    private bool motionReleased;
    private float lastStrokeTime = -100f;
    private float lastMotionTime = -100f;
    private bool armed = true;
    private JournalReader journalReader;
    private GameMenuController gameMenu;
    private bool gameplayInputEnabled;
    private float Smoothing => tuning != null ? tuning.motionSmoothing : smoothing;
    private float TriggerSpeed => tuning != null ? tuning.beatMotionTriggerSpeed : triggerSpeed;
    private float ResetSpeed => tuning != null ? tuning.beatMotionResetSpeed : resetSpeed;
    private float Cooldown => tuning != null ? tuning.beatMotionCooldownSeconds : cooldown;
    private float MinimumStrokePixels => tuning != null ? tuning.beatMotionMinimumPixels : minimumStrokePixels;
    private float MotionThreshold => tuning != null ? tuning.motionThresholdPixels : motionThresholdPixels;
    private float MotionCooldown => tuning != null ? tuning.motionFeedbackCooldownSeconds : motionFeedbackCooldown;


    public event Action<double> Downstroke;
    public event Action Motion;

    private void Awake()
    {
        if (tuning == null)
        {
            var timeline = FindFirstObjectByType<FirstBeatTimeline>();
            if (timeline != null) tuning = timeline.Tuning;
        }
    }

    public void SetGameplayInputEnabled(bool enabled)
    {
        gameplayInputEnabled = enabled;
        filteredSpeed = 0f;
        previousFilteredSpeed = 0f;
        strokeDistance = 0f;
        previousDirection = Vector2.zero;
        hasDirection = false;
        motionReleased = false;
        armed = true;
        lastStrokeTime = Time.unscaledTime;
        lastMotionTime = Time.unscaledTime;
        lastMotionSampleTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (!gameplayInputEnabled || !IsGameplayActive) return;
        if (journalReader == null) journalReader = FindFirstObjectByType<JournalReader>();
        if (journalReader != null && journalReader.IsOpen) return;
        if (Mouse.current == null) return;
        Vector2 delta = Mouse.current.delta.ReadValue();
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        float travel = delta.magnitude;
        float speed = travel / dt;
        previousFilteredSpeed = filteredSpeed;
        filteredSpeed = Mathf.Lerp(filteredSpeed, speed, 1f - Mathf.Exp(-Smoothing * dt));

        if (travel >= MotionThreshold && Time.unscaledTime - lastMotionTime >= MotionCooldown)
        {
            lastMotionTime = Time.unscaledTime;
            Motion?.Invoke();
        }

        bool moving = travel >= MotionThreshold;
        bool changedDirection = moving && hasDirection &&
            Vector2.Dot(previousDirection, delta / travel) < 0.35f;
        if (moving)
        {
            if (Time.unscaledTime - lastMotionSampleTime > 0.18f)
            {
                strokeDistance = 0f;
                hasDirection = false;
            }
            if (changedDirection)
                strokeDistance = 0f;
            strokeDistance += travel;
            lastMotionSampleTime = Time.unscaledTime;
            previousDirection = delta / travel;
            hasDirection = true;
        }
        else if (Time.unscaledTime - lastMotionSampleTime > 0.18f)
        {
            strokeDistance = 0f;
            hasDirection = false;
        }

        if (!moving && filteredSpeed < ResetSpeed)
            motionReleased = true;

        if (!armed && (motionReleased || changedDirection) &&
            Time.unscaledTime - lastStrokeTime >= Cooldown)
        {
            armed = true;
            strokeDistance = changedDirection ? travel : 0f;
            motionReleased = false;
        }

        // A fresh acceleration is a distinct action even if the mouse never came to a full stop.
        if (!armed && Time.unscaledTime - lastStrokeTime >= Cooldown &&
            filteredSpeed >= TriggerSpeed &&
            previousFilteredSpeed < TriggerSpeed * 0.7f &&
            filteredSpeed > previousFilteredSpeed)
        {
            armed = true;
            strokeDistance = travel;
        }

        if (armed && filteredSpeed >= TriggerSpeed && strokeDistance >= MinimumStrokePixels
            && Time.unscaledTime - lastStrokeTime >= Cooldown)
        {
            armed = false;
            motionReleased = false;
            lastStrokeTime = Time.unscaledTime;
            strokeDistance = 0f;
            EmitDownstroke(AudioSettings.dspTime);
        }
    }

    public void EmitDownstroke(double dspTime)
    {
        if (!gameplayInputEnabled || !IsGameplayActive) return;
        Downstroke?.Invoke(dspTime);
    }

    public void EmitMotion()
    {
        if (!gameplayInputEnabled || !IsGameplayActive) return;
        Motion?.Invoke();
    }

    private bool IsGameplayActive
    {
        get
        {
            if (gameMenu == null) gameMenu = FindFirstObjectByType<GameMenuController>();
            return gameMenu == null || gameMenu.IsGameplayActive;
        }
    }
}
