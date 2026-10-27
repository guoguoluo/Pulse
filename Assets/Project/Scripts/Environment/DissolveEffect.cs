using System.Collections;
using UnityEngine;

/// <summary>
/// 材质溶解效果 —— 用于单个物体的"成真"过渡。
/// 通过修改 URP Lit Shader 的 _DissolveProgress 参数（需自定义 Shader 支持）。
/// 
/// 简单替代方案：使用 URP Lit + 自定义 ShaderGraph 添加 _DissolveProgress。
/// 或在没有 ShaderGraph 时，用顶点颜色 + alpha 渐变作为 fallback。
/// 
/// 挂载到单个环境物体上，由 EnvironmentReveal 统一控制。
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class DissolveEffect : MonoBehaviour
{
    [Header("Dissolve")]
    [Tooltip("溶解进度 0-1，0=全息/虚拟，1=完全实体")]
    [Range(0f, 1f)]
    [SerializeField] private float progress = 0f;

    [Tooltip("溶解持续时间")]
    [SerializeField] private float duration = 5f;

    [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock mpb;
    private static readonly int DissolveProgress = Shader.PropertyToID("_DissolveProgress");
    private Coroutine activeCoroutine;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    /// <summary>
    /// 从当前进度溶解到目标进度
    /// </summary>
    public void DissolveTo(float target, float overrideDuration = -1f)
    {
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(DissolveRoutine(target, overrideDuration > 0f ? overrideDuration : duration));
    }

    private IEnumerator DissolveRoutine(float target, float dur)
    {
        float start = progress;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float curvedT = progressCurve.Evaluate(t);
            progress = Mathf.Lerp(start, target, curvedT);

            ApplyProgress();
            yield return null;
        }

        progress = target;
        ApplyProgress();
    }

    private void ApplyProgress()
    {
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(DissolveProgress, progress);
        meshRenderer.SetPropertyBlock(mpb);
    }
}