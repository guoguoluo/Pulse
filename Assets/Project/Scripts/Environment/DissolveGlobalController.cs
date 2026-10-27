using UnityEngine;

/// <summary>
/// 溶解 Shader 参数驱动 —— 在 URP Lit 基础上通过 MaterialPropertyBlock
/// 或全局 Shader 参数控制溶解进度。
/// 
/// 当没有自定义 ShaderGraph 时，这个组件作为 fallback，
/// 通过修改全局 Shader 浮点参数 _GlobalDissolve 让所有支持该参数的材质同步变化。
/// 
/// 推荐：在 ShaderGraph 中创建 URP Lit 变体，添加 _DissolveProgress 参数，
/// 在 Fragment 阶段用 Step(DissolveProgress, noise) 决定可见性。
/// </summary>
public class DissolveGlobalController : MonoBehaviour
{
    private static readonly int GlobalDissolve = Shader.PropertyToID("_GlobalDissolve");

    [Header("Global Dissolve")]
    [Range(0f, 1f)]
    [SerializeField] private float globalDissolveProgress = 0f;

    private void Update()
    {
        Shader.SetGlobalFloat(GlobalDissolve, globalDissolveProgress);
    }

    public void SetProgress(float value)
    {
        globalDissolveProgress = Mathf.Clamp01(value);
        Shader.SetGlobalFloat(GlobalDissolve, globalDissolveProgress);
    }

    public float GetProgress() => globalDissolveProgress;
}