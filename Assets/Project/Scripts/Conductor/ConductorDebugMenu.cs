using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Runtime-only readout of active conductor score awards.</summary>
public class ConductorDebugMenu : MonoBehaviour
{
    [SerializeField] private ConductorDemo conductor;
    [SerializeField] private GameMenuController gameMenu;
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI countsText;
    private bool requestedVisible;

    private void Awake()
    {
        if (conductor == null) conductor = FindFirstObjectByType<ConductorDemo>();
        if (gameMenu == null) gameMenu = FindFirstObjectByType<GameMenuController>();
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (panel == null) return;
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            requestedVisible = !requestedVisible;
        bool visible = requestedVisible && (gameMenu == null || gameMenu.IsGameplayActive);
        if (panel.activeSelf != visible) panel.SetActive(visible);
        if (!visible) return;
        if (conductor == null) return;
        if (scoreText != null) scoreText.text = $"当前总分  {conductor.LightScore}";
        if (countsText != null)
            countsText.text = $"Motion  {conductor.ActiveAwardCount(ConductorGestureRating.Motion)}\n" +
                $"Good  {conductor.ActiveAwardCount(ConductorGestureRating.Good)}\n" +
                $"Great  {conductor.ActiveAwardCount(ConductorGestureRating.Great)}\n" +
                $"Perfect  {conductor.ActiveAwardCount(ConductorGestureRating.Perfect)}";
    }
}
