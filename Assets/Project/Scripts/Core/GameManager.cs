using UnityEngine;

/// <summary>
/// 全局 GameManager —— 单例，管理核心游戏状态。
/// 「无失败」设计：玩家状态只有 Walking / Aligned / EventTriggered。
/// 通过 AudioMixer 参数驱动动态混音，无 HUD。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Walking;

    public enum GameState
    {
        Walking,       // 玩家正常行走，脉冲待对齐
        Aligned,       // 脚步与脉冲对齐，正在播放旁白
        EventTriggered // 节点事件已触发，环境正在"成真"
    }

    public GameState CurrentState => currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 由 PulseMatcher 调用：进入对齐状态
    /// </summary>
    public void SetAligned()
    {
        if (currentState == GameState.Walking)
        {
            currentState = GameState.Aligned;
        }
    }

    /// <summary>
    /// 由 NodeTrigger 调用：触发节点事件
    /// </summary>
    public void SetEventTriggered()
    {
        currentState = GameState.EventTriggered;
    }

    /// <summary>
    /// 由 PulseMatcher 调用：失去对齐回到行走状态
    /// </summary>
    public void SetWalking()
    {
        currentState = GameState.Walking;
    }
}