using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 速度显示管理器
/// 用于在游戏界面上实时显示车辆当前速度和最大速度限制
/// </summary>
public class SpeedDisplayManager : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("显示当前速度的 Text 组件")]
    public Text currentSpeedText;
    
    [Tooltip("显示最大速度限制的 Text 组件（可选）")]
    public Text maxSpeedText;
    
    [Tooltip("进度条 Slider（可选，显示当前速度占最大速度的比例）")]
    public Slider speedSlider;
    
    [Tooltip("警告图标或文本（当达到最大速度时显示，可选）")]
    public GameObject maxSpeedWarning;

    [Header("显示设置")]
    [Tooltip("是否显示单位（km/h）")]
    public bool showUnit = true;
    
    [Tooltip("速度文字前缀")]
    public string speedPrefix = "速度：";
    
    [Tooltip("最大速度文字前缀")]
    public string maxSpeedPrefix = "限速：";
    
    [Tooltip("小数位数")]
    public int decimalPlaces = 0;

    [Header("颜色设置（可选）")]
    [Tooltip("低速时的颜色")]
    public Color normalColor = Color.white;
    
    [Tooltip("接近最大速度时的颜色（80% 以上）")]
    public Color warningColor = Color.yellow;
    
    [Tooltip("达到最大速度时的颜色")]
    public Color maxSpeedColor = Color.red;

    // 私有变量
    private CarController carController;
    private float maxSpeedLimit = 150f;

    void Start()
    {
        // 查找场景中的 CarController
        carController = FindObjectOfType<CarController>();
        
        if (carController == null)
        {
            Debug.LogWarning("未找到 CarController 组件！请确保场景中有车辆。");
        }
        else
        {
            // 获取最大速度限制
            maxSpeedLimit = carController.maxSpeed;
            
            // 初始化最大速度显示
            if (maxSpeedText != null)
            {
                UpdateMaxSpeedDisplay();
            }
            
            // 初始化警告状态
            if (maxSpeedWarning != null)
            {
                maxSpeedWarning.SetActive(false);
            }
        }
    }

    void LateUpdate()
    {
        if (carController == null) return;

        // 获取当前速度
        float currentSpeed = carController.CurrentSpeed;
        
        // 更新当前速度显示
        UpdateCurrentSpeedDisplay(currentSpeed);
        
        // 更新进度条
        UpdateSpeedSlider(currentSpeed);
        
        // 更新警告状态
        UpdateMaxSpeedWarning(currentSpeed);
        
        // 根据速度改变颜色
        UpdateSpeedColor(currentSpeed);
    }

    /// <summary>
    /// 更新当前速度显示
    /// </summary>
    void UpdateCurrentSpeedDisplay(float currentSpeed)
    {
        if (currentSpeedText != null)
        {
            string unit = showUnit ? " km/h" : "";
            currentSpeedText.text = $"{speedPrefix}{currentSpeed.ToString($"F{decimalPlaces}")}{unit}";
        }
    }

    /// <summary>
    /// 更新最大速度显示
    /// </summary>
    void UpdateMaxSpeedDisplay()
    {
        if (maxSpeedText != null)
        {
            string unit = showUnit ? " km/h" : "";
            maxSpeedText.text = $"{maxSpeedPrefix}{maxSpeedLimit.ToString($"F{decimalPlaces}")}{unit}";
        }
    }

    /// <summary>
    /// 更新速度进度条
    /// </summary>
    void UpdateSpeedSlider(float currentSpeed)
    {
        if (speedSlider != null && maxSpeedLimit > 0)
        {
            // 计算速度比例（0-1）
            float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeedLimit);
            speedSlider.value = speedRatio;
        }
    }

    /// <summary>
    /// 更新最大速度警告
    /// </summary>
    void UpdateMaxSpeedWarning(float currentSpeed)
    {
        if (maxSpeedWarning != null)
        {
            // 当速度达到最大速度的 95% 时显示警告
            bool shouldShowWarning = currentSpeed >= maxSpeedLimit * 0.95f;
            maxSpeedWarning.SetActive(shouldShowWarning);
        }
    }

    /// <summary>
    /// 根据速度更新文本颜色
    /// </summary>
    void UpdateSpeedColor(float currentSpeed)
    {
        if (currentSpeedText == null) return;

        float speedRatio = currentSpeed / maxSpeedLimit;
        
        if (speedRatio >= 0.95f)
        {
            currentSpeedText.color = maxSpeedColor;
        }
        else if (speedRatio >= 0.8f)
        {
            currentSpeedText.color = warningColor;
        }
        else
        {
            currentSpeedText.color = normalColor;
        }
    }

    /// <summary>
    /// 动态更新最大速度限制（可用于道具或升级系统）
    /// </summary>
    public void UpdateMaxSpeedLimit(float newMaxSpeed)
    {
        maxSpeedLimit = newMaxSpeed;
        if (carController != null)
        {
            carController.maxSpeed = newMaxSpeed;
        }
        UpdateMaxSpeedDisplay();
    }
}
