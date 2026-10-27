using UnityEngine;

/// <summary>
/// PulseBeatClock — ticks at PulseProfile BPM interval.
/// Drives AudioManager.PlayPulseBeat() and exposes OnBeat for subscribers.
///
/// Attached to AudioManager GameObject.
/// </summary>
public class PulseBeatClock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PulseMatcher pulseMatcher;
    [Tooltip("Whether to play the pulse beat audio on each tick")]
    [SerializeField] private bool playAudioBeat = true;

    public static event System.Action OnBeat;

    private float nextBeatTime;
    private PulseProfile currentProfile;

    private void Start()
    {
        if (pulseMatcher == null)
            pulseMatcher = FindFirstObjectByType<PulseMatcher>();
        SyncProfile();
    }

    private void Update()
    {
        SyncProfile();
        if (currentProfile == null) return;

        if (Time.time >= nextBeatTime)
        {
            nextBeatTime = Time.time + currentProfile.BeatInterval;
            OnBeat?.Invoke();
            if (playAudioBeat)
                AudioManager.Instance?.PlayPulseBeat();
        }
    }

    private void SyncProfile()
    {
        if (pulseMatcher != null && pulseMatcher.ActiveProfile != currentProfile)
        {
            currentProfile = pulseMatcher.ActiveProfile;
            nextBeatTime = Time.time; // reset beat timing on profile switch
        }
    }

    public void ResetClock()
    {
        nextBeatTime = Time.time;
    }
}
