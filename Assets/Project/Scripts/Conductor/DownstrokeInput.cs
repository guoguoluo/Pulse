using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Converts relative mouse motion (or a future device adapter) to one downstroke event.</summary>
public class DownstrokeInput : MonoBehaviour
{
    [Header("Downstroke Detection")]
    [Tooltip("Higher values react faster to mouse movement.")]
    [Min(0.1f)] [SerializeField] private float smoothing = 12f;
    [Tooltip("Filtered downward speed in pixels per second needed to trigger.")]
    [Min(0f)] [SerializeField] private float triggerSpeed = 420f;
    [Tooltip("Speed below which another downstroke can be armed.")]
    [Min(0f)] [SerializeField] private float resetSpeed = 110f;
    [Min(0f)] [SerializeField] private float cooldown = 0.38f;
    [Min(0f)] [SerializeField] private float minimumStrokePixels = 28f;

    [Header("Any Motion Feedback")]
    [Tooltip("Absolute mouse movement in pixels per frame for a faint response.")]
    [Min(0f)] [SerializeField] private float motionThresholdPixels = 1f;
    [Min(0f)] [SerializeField] private float motionFeedbackCooldown = 0.12f;
    private float filteredSpeed;
    private float strokeDistance;
    private float lastStrokeTime = -100f;
    private float lastMotionTime = -100f;
    private bool armed = true;


    public event Action<double> Downstroke;
    public event Action Motion;

private void Update()
    {
        if (Mouse.current == null) return;
        float dy = Mouse.current.delta.ReadValue().y;
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        float speed = -dy / dt;
        filteredSpeed = Mathf.Lerp(filteredSpeed, speed, 1f - Mathf.Exp(-smoothing * dt));

        if (Mathf.Abs(dy) >= motionThresholdPixels && Time.unscaledTime - lastMotionTime >= motionFeedbackCooldown)
        {
            lastMotionTime = Time.unscaledTime;
            Motion?.Invoke();
        }

        if (filteredSpeed > resetSpeed)
            strokeDistance += Mathf.Max(0f, -dy);
        else if (filteredSpeed < resetSpeed)
            strokeDistance = 0f;

        if (!armed && filteredSpeed < resetSpeed && Time.unscaledTime - lastStrokeTime >= cooldown)
            armed = true;

        if (armed && filteredSpeed >= triggerSpeed && strokeDistance >= minimumStrokePixels
            && Time.unscaledTime - lastStrokeTime >= cooldown)
        {
            armed = false;
            lastStrokeTime = Time.unscaledTime;
            strokeDistance = 0f;
            EmitDownstroke(AudioSettings.dspTime);
        }
    }

    public void EmitDownstroke(double dspTime)
    {
        Downstroke?.Invoke(dspTime);
    }


public void EmitMotion()
    {
        Motion?.Invoke();
    }
}
