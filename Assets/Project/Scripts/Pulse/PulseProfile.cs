using UnityEngine;

/// <summary>
/// 星球脉冲系统 —— 定义脉冲的 BPM 和节奏模式。
/// 脉冲是"星球的心跳"，玩家脚步需要对齐这个节奏来"翻译"旁白。
/// 
/// 每个走廊段可以有不同的 pulseProfile（例如前段 72BPM、中段 90BPM）。
/// 挂载在场景中的 PulseZone 触发器上。
/// 
/// [可配置项] bpm：脉冲速度
/// [可配置项] tolerancePercent：对齐宽容度（百分比），默认 ±15%
/// [可配置项] consecutiveStepsRequired：需要连续多少步对齐才判定成功
/// </summary>
[CreateAssetMenu(fileName = "PulseProfile", menuName = "PulseMachine/Pulse Profile")]
public class PulseProfile : ScriptableObject
{
    [Tooltip("脉冲 BPM —— 星球的心跳速度")]
    [Range(40f, 160f)]
    [SerializeField] private float bpm = 72f;

    [Tooltip("BPM 对齐宽容度（百分比），例如 15 表示 ±15%")]
    [Range(5f, 30f)]
    [SerializeField] private float tolerancePercent = 15f;

    [Tooltip("需要连续对齐多少步才算完成一次'翻译'")]
    [Range(2, 8)]
    [SerializeField] private int consecutiveStepsRequired = 4;

    [Tooltip("音频：脉冲参考音（低频底鼓/心跳声），用于在 AudioManager 中合成节拍")]
    [SerializeField] private AudioClip pulseClip;

    public float BPM => bpm;
    public float TolerancePercent => tolerancePercent;
    public int ConsecutiveStepsRequired => consecutiveStepsRequired;
    public AudioClip PulseClip => pulseClip;

    /// <summary>
    /// 根据 BPM 返回每次拍子之间的间隔（秒）
    /// </summary>
    public float BeatInterval => 60f / bpm;

    /// <summary>
    /// 检测给定 BPM 是否在当前脉冲的宽容范围内
    /// </summary>
    public bool IsBPMInTolerance(float playerBPM)
    {
        float dev = Mathf.Abs(playerBPM - bpm) / bpm * 100f;
        return dev <= tolerancePercent;
    }
}