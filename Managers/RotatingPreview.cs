using UnityEngine;

/// <summary>
/// 预览模型旋转组件：让模型缓慢绕 Y 轴旋转，用于展示效果。
/// </summary>
public class RotatingPreview : MonoBehaviour
{
    [Tooltip("旋转速度（度/秒）")]
    public float rotationSpeed = 30f;

    [Tooltip("是否启用旋转")]
    public bool enableRotation = true;

    private void Update()
    {
        if (!enableRotation)
            return;

        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }
}