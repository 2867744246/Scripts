using UnityEngine;

/// <summary>
/// 简单的移动输入管理器：供 UI 按钮/摇杆调用并在运行时为车辆提供 throttle/steer/handbrake 值。
/// 把此脚本挂到场景中的一个空物体（例如 MobileInput），并在 Inspector 中把该引用拖到 CarController.mobileInput。
/// </summary>
public class MobileInputManager : MonoBehaviour
{
    [HideInInspector] public float throttle; // -1..1
    [HideInInspector] public float steer;    // -1..1
    [HideInInspector] public bool handbrake;

    // 平滑过渡（可在 Inspector 调整）
    public float steerSmoothing = 8f;
    public float throttleSmoothing = 8f;

    private float targetThrottle;
    private float targetSteer;

    void Update()
    {
        // 平滑过渡防止瞬间跳变
        throttle = Mathf.Lerp(throttle, targetThrottle, Time.deltaTime * throttleSmoothing);
        steer = Mathf.Lerp(steer, targetSteer, Time.deltaTime * steerSmoothing);
    }

    // UI/摇杆调用接口
    public void SetThrottle(float value)
    {
        targetThrottle = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetSteer(float value)
    {
        targetSteer = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetHandbrake(bool pressed)
    {
        handbrake = pressed;
    }
}
