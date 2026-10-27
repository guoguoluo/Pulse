using UnityEngine;

/// <summary>
/// Footstep event data — published to all subscribers.
/// Fields: position, terrain type, timestamp, isAccent flag.
/// </summary>
public struct FootstepEvent
{
    public Vector3 position;
    public GroundMaterial.TerrainType terrain;
    public float timestamp;
    public bool isAccent;
}

/// <summary>
/// Lightweight static event bus for footstep events.
/// Subscribers: AudioManager, PulseMatcher, GaitRecognizer.
/// </summary>
public static class FootstepEventBus
{
    public delegate void FootstepHandler(FootstepEvent evt);
    public static event FootstepHandler OnFootstep;

    public static void Publish(FootstepEvent evt)
    {
        OnFootstep?.Invoke(evt);
    }
}
