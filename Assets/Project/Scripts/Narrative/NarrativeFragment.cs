using UnityEngine;

/// <summary>
/// 旁白碎片数据 —— 一段被机器从同伴对话中重建的诗性旁白。
/// 
/// 通过 ScriptableObject 定义每段旁白的：
///   - 音频剪辑（经过失真处理的对话片段）
///   - 字幕文本（世界空间显示）
///   - 完成对话后解锁的关联节点
/// 
/// [可配置项] isLastFragment：是否为最后一个碎片（触发节点事件）
/// </summary>
[CreateAssetMenu(fileName = "NarrativeFragment", menuName = "PulseMachine/Narrative Fragment")]
public class NarrativeFragment : ScriptableObject
{
    [Header("Content")]
    [TextArea(2, 6)]
    [SerializeField] private string subtitleText = "They said... the pulse was never a signal. It was a memory.";

    [Tooltip("音频剪辑 —— 经 machine reconstruction 处理的对话")]
    [SerializeField] private AudioClip audioClip;

    [Header("Timing")]
    [Tooltip("字幕在世界空间中显示的持续时间（秒）")]
    [SerializeField] private float subtitleDuration = 5f;

    [Tooltip("播放完成后到下一个碎片的最小间隔（秒）")]
    [SerializeField] private float cooldownSeconds = 3f;

    [Header("Narrative Logic")]
    [Tooltip("是否为该段落最后一个碎片，完成后触发节点事件")]
    [SerializeField] private bool isLastFragment;

    [Tooltip("完成后激活的节点触发器（可选，仅 isLastFragment 使用）")]
    [SerializeField] private string nodeTriggerName;

    [Header("Audio Effect Preset")]
    [Tooltip("机器重建效果预设标签 —— AudioManager 根据此标签调整 DSP 效果")]
    [SerializeField] private string fxPreset = "glitch_light";

    public string SubtitleText => subtitleText;
    public AudioClip AudioClip => audioClip;
    public float SubtitleDuration => subtitleDuration;
    public float CooldownSeconds => cooldownSeconds;
    public bool IsLastFragment => isLastFragment;
    public string NodeTriggerName => nodeTriggerName;
    public string FxPreset => fxPreset;
}