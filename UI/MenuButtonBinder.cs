using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 按钮绑定器：将主界面、车辆选择、地图选择按钮与 MenuFlowManager 的逻辑连接起来。
/// </summary>
public class MenuButtonBinder : MonoBehaviour
{
    [Header("逻辑引用")]
    public MenuFlowManager flowManager;

    [Header("主界面按钮")]
    public Button startGameButton;
    public Button vehicleSelectButton;
    public Button mapSelectButton;

    [Header("车辆选择按钮")]
    public Button vehiclePrevButton;
    public Button vehicleNextButton;
    public Button vehicleConfirmButton;
    public Button vehicleSelectBackButton;

    [Header("地图选择按钮")]
    public Button mapSelectBackButton;

    private void Awake()
    {
        if (flowManager == null)
            flowManager = FindObjectOfType<MenuFlowManager>();
    }

    private void Start()
    {
        if (flowManager == null)
        {
            Debug.LogError("MenuButtonBinder 需要 MenuFlowManager 引用，请在 Inspector 中绑定或确保场景中存在 MenuFlowManager。");
            return;
        }

        BindButton(startGameButton, flowManager.StartGame);
        BindButton(vehicleSelectButton, flowManager.OpenVehicleSelect);
        BindButton(mapSelectButton, flowManager.OpenMapSelect);

        BindButton(vehiclePrevButton, flowManager.OnVehiclePrev);
        BindButton(vehicleNextButton, flowManager.OnVehicleNext);
        BindButton(vehicleConfirmButton, flowManager.ConfirmVehicleSelection);
        BindButton(vehicleSelectBackButton, flowManager.CancelVehicleSelection);

        BindButton(mapSelectBackButton, flowManager.CancelMapSelection);
    }

    private void BindButton(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
