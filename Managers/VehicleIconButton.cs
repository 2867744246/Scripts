using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 车辆选择图标按钮，用于固定图标绑定对应车辆索引。
/// </summary>
[RequireComponent(typeof(Button))]
public class VehicleIconButton : MonoBehaviour
{
    public int vehicleIndex;
    public MenuFlowManager flowManager;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (flowManager != null)
        {
            flowManager.OnVehicleIconClicked(vehicleIndex);
        }
    }
}
