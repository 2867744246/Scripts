using UnityEngine;

/// <summary>
/// 车道系统 - 管理 2 车道的宽度和位置
/// </summary>
public class LaneSystem : MonoBehaviour
{
    [Header("车道配置")]
    [Tooltip("车道数量（固定为 2）")]
    public int laneCount = 2;
    
    [Tooltip("每条车道的宽度（米）")]
    public float laneWidth = 3.5f;
    
    [Tooltip("道路中心线的 Z 坐标")]
    public float roadCenterZ = 0f;
    
    [Header("编辑器可视化配置")]
    [Tooltip("编辑器中车道线的绘制长度（米）")]
    public float lineLength = 10.0f;
    [Tooltip("编辑器中车道线的 Y 坐标")]
    public float lineY = 2.0f;
    
    [Tooltip("编辑器中车道线的颜色")]
    public Color lineColor = Color.yellow;
    
    /// <summary>
    /// 获取指定车道的中心 Z 坐标
    /// </summary>
    /// <param name="laneIndex">车道索引（-1: 左车道，1: 右车道）</param>
    /// <returns>车道中心的世界坐标值</returns>
    public float GetLanePosition(int laneIndex)
    {
        return roadCenterZ + (laneIndex * laneWidth / 2f);
    }
    
    /// <summary>
    /// 随机选择一个车道索引
    /// </summary>
    public int GetRandomLane()
    {
        return Random.value > 0.5f ? -1 : 1;
    }
    
    /// <summary>
    /// 在编辑器中绘制车道线 Gizmos
    /// </summary>
    private void OnDrawGizmos()
    {
        // 只在编辑器模式下绘制
        if (!Application.isPlaying)
        {
            DrawLaneLines();
        }
    }
    
    /// <summary>
    /// 在编辑器中绘制车道线（运行时也调用）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        DrawLaneLines();
    }
    
    /// <summary>
    /// 绘制车道线的核心方法
    /// </summary>
    private void DrawLaneLines()
    {
        Gizmos.color = lineColor;
        
        // 绘制所有车道线（车道数 + 1 条线）
        // 例如：2 车道需要绘制 3 条线（左边界、中间分隔线、右边界）
        for (int i = 0; i <= laneCount; i++)
        {
            // 计算每条线的 X 坐标
            // i=0: 最左侧边界
            // i=1: 中间分隔线（对于 2 车道）
            // i=2: 最右侧边界
            float zPos = roadCenterZ + ((i - 1) * laneWidth);
            
            Vector3 startPos = new Vector3(transform.position.x - lineLength / 2f, lineY, zPos);
            Vector3 endPos = new Vector3(transform.position.x + lineLength / 2f, lineY, zPos);
            
            Gizmos.DrawLine(startPos, endPos);
        }
        
    }
}

/// <summary>
/// 车道边界结构
/// </summary>
[System.Serializable]
public struct LaneBounds
{
    public float leftEdge;    // 左边界
    public float rightEdge;   // 右边界
    public float center;      // 中心点
}