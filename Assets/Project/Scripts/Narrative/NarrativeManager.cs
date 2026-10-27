using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// NarrativeManager — plays NarrativeFragment queue in order.
/// Subscribes to PulseMatcher.OnAligned to trigger next fragment.
///
/// Feedback:
///   - Audio: plays fragment voice clip through voiceAudioSource
///   - Subtitle: world-space TMP fade-in/out
///   - After last fragment: notifies NodeTriggerManager
/// </summary>
public class NarrativeManager : MonoBehaviour
{
    [Header("Fragment Queue")]
    [Tooltip("Fragments played in order as player aligns with pulse")]
    [SerializeField] private List<NarrativeFragment> fragmentQueue = new List<NarrativeFragment>();

    [Header("Display")]
    [SerializeField] private TextMeshPro subtitleDisplay;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [Tooltip("Duration for subtitle fade in/out (seconds)")]
    [SerializeField] private float subtitleFadeDuration = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("State")]
    [SerializeField] private bool autoStart = true;

    private int currentFragmentIndex = -1;
    private bool isPlaying;
    private float cooldownEndTime;

    private void Start()
    {
        if (subtitleCanvasGroup != null)
            subtitleCanvasGroup.alpha = 0f;

        if (autoStart)
            StartCoroutine(InitializeCooldown());
    }

    private IEnumerator InitializeCooldown()
    {
        yield return new WaitForSeconds(0.5f);
        cooldownEndTime = Time.time;
    }

    /// <summary>
    /// Called by PulseMatcher.OnAligned UnityEvent.
    /// Tries to play the next fragment in the queue.
    /// Ignores if already playing or in cooldown.
    /// </summary>
    public void TryPlayNextFragment()
    {
        if (isPlaying) return;
        if (Time.time < cooldownEndTime) return;
        if (fragmentQueue.Count == 0) return;

        currentFragmentIndex++;
        if (currentFragmentIndex >= fragmentQueue.Count)
        {
            Debug.Log("[NarrativeManager] All fragments exhausted.");
            return;
        }

        // Register alignment progress with NodeTriggerManager
        NodeTriggerManager.Instance?.RegisterAlignment();

        StartCoroutine(PlayFragment(fragmentQueue[currentFragmentIndex]));
    }

    private IEnumerator PlayFragment(NarrativeFragment fragment)
    {
        isPlaying = true;

        if (fragment.AudioClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.clip = fragment.AudioClip;
            voiceAudioSource.Play();
        }

        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = fragment.SubtitleText;
            yield return StartCoroutine(FadeSubtitle(0f, 1f));
        }

        yield return new WaitForSeconds(fragment.SubtitleDuration);

        if (subtitleDisplay != null)
            yield return StartCoroutine(FadeSubtitle(1f, 0f));

        cooldownEndTime = Time.time + fragment.CooldownSeconds;
        isPlaying = false;

        if (fragment.IsLastFragment)
        {
            NodeTriggerManager.Instance?.TriggerNode(fragment.NodeTriggerName);
        }
    }

    private IEnumerator FadeSubtitle(float from, float to)
    {
        if (subtitleCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < subtitleFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / subtitleFadeDuration;
            subtitleCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        subtitleCanvasGroup.alpha = to;
    }

    public void JumpToFragment(int index)
    {
        currentFragmentIndex = Mathf.Clamp(index - 1, -1, fragmentQueue.Count - 1);
        cooldownEndTime = 0f;
        isPlaying = false;
    }
}
