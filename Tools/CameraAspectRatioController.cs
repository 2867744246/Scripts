using UnityEngine;
using Cinemachine;
using Unity.Mathematics;

/**
 * @class CameraAspectRatioController
 * @brief 相机宽高比控制器，根据屏幕宽高比动态调整Cinemachine的视野(FOV)
 * @details 
 * - 基准宽高比为16:9，当宽高比增大时线性增加FOV
 * - 19:9宽高比时FOV增加0.7度
 * - 用于适配不同屏幕比例，保持游戏画面的一致性
 */
public class CameraAspectRatioController : MonoBehaviour
{
    /** @brief Cinemachine虚拟相机组件 */
    private CinemachineVirtualCamera virtualCamera;

    [Tooltip("FOV缩放因子")]
    public float fovScaleFactor = 1f;

    [Tooltip("基准宽高比")]
    public float baseAspectRatio = 19f / 9f;

    [Tooltip("基准FOV")]
    public float baseFov;
    /**
     * @brief 初始化方法
     * @details 获取Cinemachine虚拟相机组件并根据当前屏幕宽高比调整FOV
     */
    void Start()
    {
        // 获取Cinemachine虚拟相机组件
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        
        if (virtualCamera == null)
        {
            Debug.LogWarning("[CameraAspectRatioController] 未找到CinemachineVirtualCamera组件！");
            return;
        }

        // 计算当前屏幕宽高比
        float currentAspectRatio = Screen.width / (float)Screen.height;
        
        Debug.Log($"当前FOV: {virtualCamera.m_Lens.FieldOfView}");
        // 根据宽高比调整FOV
        AdjustFieldOfView(currentAspectRatio);
        
        Debug.Log($"[CameraAspectRatioController] 屏幕宽高比: {currentAspectRatio:F2}, FOV已调整");
    }

    /**
     * @brief 根据宽高比调整视野(FOV)
     * @param aspectRatio 当前屏幕宽高比
     */
    private void AdjustFieldOfView(float aspectRatio)
    {
        // 计算宽高比差
        float changeRatio = aspectRatio - baseAspectRatio;
        // 应用调整后的FOV,宽高比越大，FOV越小
        virtualCamera.m_Lens.FieldOfView = baseFov + changeRatio * fovScaleFactor;
        Debug.Log($"已调整为FOV: {virtualCamera.m_Lens.FieldOfView}");
    }
}
