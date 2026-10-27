using UnityEngine;

/// <summary>
/// 地面材质标签 —— 挂载在每个地面 GameObject 上。
/// 用于 FootstepDetector 获取脚部着地时的材质信息。
/// 决定播放哪组脚步声和音频混音参数。
/// 
/// Inspector 配置：
///   - 在每个地面 Mesh 的根 GameObject 上添加此组件
///   - terrainType 对应 AudioManager 中的脚步音效组
/// </summary>
public class GroundMaterial : MonoBehaviour
{
    public enum TerrainType
    {
        Metal,    // 金属走廊 — 清脆、反射强
        Stone,    // 石板地面 — 沉稳
        Dirt,     // 泥土 — 闷响
        Organic,  // 有机质/生物质 — 柔软、湿润
        Void      // 虚空 — 极度寂静 / 极低频
    }

    [SerializeField] private TerrainType terrainType = TerrainType.Metal;

    public TerrainType CurrentTerrainType => terrainType;

#if UNITY_EDITOR
    /// <summary>
    /// 调试：在 Scene 视图中用不同颜色显示地面材质。
    /// </summary>
    private void OnDrawGizmos()
    {
        Color c = terrainType switch
        {
            TerrainType.Metal => Color.gray,
            TerrainType.Stone => new Color(0.5f, 0.4f, 0.3f),
            TerrainType.Dirt => new Color(0.4f, 0.25f, 0.1f),
            TerrainType.Organic => new Color(0.2f, 0.6f, 0.3f),
            TerrainType.Void => new Color(0.05f, 0.05f, 0.1f),
            _ => Color.magenta
        };

        Gizmos.color = new Color(c.r, c.g, c.b, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
            Gizmos.DrawWireMesh(mf.sharedMesh);
        else if (TryGetComponent<Collider>(out var col))
            Gizmos.DrawCube(col.bounds.center - transform.position, col.bounds.size);
        else
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
#endif
}