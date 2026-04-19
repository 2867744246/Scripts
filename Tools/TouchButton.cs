using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 简单的触摸按钮脚本：与 UI Button 等价，但更方便地在 Inspector 中绑定 MobileInputManager。
/// 将此脚本挂到 UI 按钮对象上，并设置 buttonType 与 inputManager。
/// </summary>
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Throttle, Brake, Handbrake, Left, Right, Custom }
    public ButtonType buttonType;
    public float throttleValue = 1f; // 用于 Throttle 按钮
    public MobileInputManager inputManager;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (inputManager == null) return;

        switch (buttonType)
        {
            case ButtonType.Throttle:
                inputManager.SetThrottle(throttleValue);
                break;
            case ButtonType.Brake:
                inputManager.SetThrottle(-1f);
                break;
            case ButtonType.Handbrake:
                inputManager.SetHandbrake(true);
                break;
            case ButtonType.Left:
                inputManager.SetSteer(-1f);
                break;
            case ButtonType.Right:
                inputManager.SetSteer(1f);
                break;
            case ButtonType.Custom:
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (inputManager == null) return;

        switch (buttonType)
        {
            case ButtonType.Throttle:
            case ButtonType.Brake:
                inputManager.SetThrottle(0f);
                break;
            case ButtonType.Handbrake:
                inputManager.SetHandbrake(false);
                break;
            case ButtonType.Left:
            case ButtonType.Right:
                // 松开左右按钮时将方向归零（若同时使用摇杆可能需要更复杂的优先策略）
                inputManager.SetSteer(0f);
                break;
        }
    }
}
