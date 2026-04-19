using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 开始界面流程控制器。
/// 管理主界面、车辆选择界面和地图选择界面，以及摄像机位置切换。
/// </summary>
public class MenuFlowManager : MonoBehaviour
{
    [Header("主界面面板")]
    public GameObject mainPanel;
    public TextMeshProUGUI mainVehicleNameText;
    public TextMeshProUGUI mainMapNameText;

    [Header("车辆选择面板")]
    public GameObject vehicleSelectPanel;
    public TextMeshProUGUI vehicleSelectNameText;
    public TextMeshProUGUI vehicleConfirmButtonText;

    [Header("地图选择面板")]
    public GameObject mapSelectPanel;
    public TextMeshProUGUI mapSelectNameText;
    public TextMeshProUGUI mapConfirmButtonText;

    [Header("摄像机控制")]
    public Camera mainCamera;
    public Transform menuCameraTransform;
    public Transform vehicleSelectCameraTransform;
    public float cameraMoveSpeed = 3f;

    [Header("车辆预览")]
    public Transform previewRoot;

    private GameSettings gameSettings;
    private int previewVehicleIndex;
    private int previewMapIndex;
    private GameObject currentPreviewInstance;
    private bool isCameraMoving;
    private Vector3 targetCameraPosition;
    private Quaternion targetCameraRotation;

    private void Start()
    {
        gameSettings = GameSettings.Instance;
        if (gameSettings == null)
        {
            Debug.LogError("GameSettings 实例缺失，请确保 StartScene 中包含 GameSettings。继续游戏可能失败。");
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        previewVehicleIndex = gameSettings.selectedVehicleIndex;
        previewMapIndex = gameSettings.selectedMapIndex;

        ShowMainPanel();
        UpdateMainMenuUI();
        UpdatePreviewModel();
    }

    private void Update()
    {
        if (!isCameraMoving || mainCamera == null)
            return;

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCameraPosition, Time.deltaTime * cameraMoveSpeed);
        mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, targetCameraRotation, Time.deltaTime * cameraMoveSpeed);

        if (Vector3.Distance(mainCamera.transform.position, targetCameraPosition) < 0.01f && Quaternion.Angle(mainCamera.transform.rotation, targetCameraRotation) < 0.5f)
        {
            mainCamera.transform.position = targetCameraPosition;
            mainCamera.transform.rotation = targetCameraRotation;
            isCameraMoving = false;
        }
    }

    /// <summary>
    /// 打开车辆选择界面。
    /// </summary>
    public void OpenVehicleSelect()
    {
        previewVehicleIndex = gameSettings.selectedVehicleIndex;
        ShowVehicleSelectPanel();
        UpdateVehicleSelectUI();
        MoveCameraTo(vehicleSelectCameraTransform);
    }

    /// <summary>
    /// 打开地图选择界面。
    /// </summary>
    public void OpenMapSelect()
    {
        previewMapIndex = gameSettings.selectedMapIndex;
        ShowMapSelectPanel();
        UpdateMapSelectUI();
    }

    /// <summary>
    /// 开始游戏，加载当前选中地图场景。
    /// </summary>
    public void StartGame()
    {
        if (gameSettings == null)
            return;

        MapInfo selectedMap = gameSettings.SelectedMap;
        if (selectedMap == null || string.IsNullOrEmpty(selectedMap.sceneName))
        {
            Debug.LogWarning("未配置地图或地图场景名为空，无法开始游戏。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(selectedMap.sceneName);
    }

    /// <summary>
    /// 切换车辆到上一个。
    /// </summary>
    public void OnVehiclePrev()
    {
        if (gameSettings == null || gameSettings.vehicleList.Count == 0)
            return;

        previewVehicleIndex = (previewVehicleIndex - 1 + gameSettings.vehicleList.Count) % gameSettings.vehicleList.Count;
        UpdateVehicleSelectUI();
    }

    /// <summary>
    /// 切换车辆到下一个。
    /// </summary>
    public void OnVehicleNext()
    {
        if (gameSettings == null || gameSettings.vehicleList.Count == 0)
            return;

        previewVehicleIndex = (previewVehicleIndex + 1) % gameSettings.vehicleList.Count;
        UpdateVehicleSelectUI();
    }

    /// <summary>
    /// 车辆图标被点击。
    /// </summary>
    /// <param name="vehicleIndex">图标对应车辆索引。</param>
    public void OnVehicleIconClicked(int vehicleIndex)
    {
        if (gameSettings == null || vehicleIndex < 0 || vehicleIndex >= gameSettings.vehicleList.Count)
            return;

        previewVehicleIndex = vehicleIndex;
        UpdateVehicleSelectUI();
    }

    /// <summary>
    /// 地图图标被点击。
    /// </summary>
    /// <param name="mapIndex">图标对应地图索引。</param>
    public void OnMapIconClicked(int mapIndex)
    {
        if (gameSettings == null || mapIndex < 0 || mapIndex >= gameSettings.mapList.Count)
            return;

        previewMapIndex = mapIndex;
        UpdateMapSelectUI();
    }

    /// <summary>
    /// 确认选择当前车辆并返回主界面。
    /// </summary>
    public void ConfirmVehicleSelection()
    {
        if (gameSettings == null)
            return;

        gameSettings.SetSelectedVehicle(previewVehicleIndex);
        UpdateMainMenuUI();
        UpdatePreviewModel();
        ShowMainPanel();
    }

    /// <summary>
    /// 确认选择当前地图并返回主界面。
    /// </summary>
    public void ConfirmMapSelection()
    {
        if (gameSettings == null)
            return;

        gameSettings.SetSelectedMap(previewMapIndex);
        UpdateMainMenuUI();
        ShowMainPanel();
    }

    /// <summary>
    /// 车辆选择界面返回上一级，不提交当前预览选择。
    /// </summary>
    public void CancelVehicleSelection()
    {
        ShowMainPanel();
    }

    /// <summary>
    /// 地图选择界面返回上一级，不提交当前预览选择。
    /// </summary>
    public void CancelMapSelection()
    {
        ShowMainPanel();
    }

    private void UpdateMainMenuUI()
    {
        if (mainVehicleNameText != null)
        {
            mainVehicleNameText.text = gameSettings.SelectedVehicle != null ? gameSettings.SelectedVehicle.vehicleName : "未选择车辆";
        }

        if (mainMapNameText != null)
        {
            mainMapNameText.text = gameSettings.SelectedMap != null ? gameSettings.SelectedMap.mapName : "未选择地图";
        }
    }

    private void UpdateVehicleSelectUI()
    {
        VehicleInfo vehicle = GetPreviewVehicle();
        if (vehicle == null)
            return;

        if (vehicleSelectNameText != null)
            vehicleSelectNameText.text = vehicle.vehicleName;

        if (vehicleConfirmButtonText != null)
        {
            bool alreadySelected = previewVehicleIndex == gameSettings.selectedVehicleIndex;
            vehicleConfirmButtonText.text = alreadySelected ? "已选择" : "选择";
        }

        UpdatePreviewModel(vehicle);
    }

    private void UpdateMapSelectUI()
    {
        MapInfo map = GetPreviewMap();
        if (map == null)
            return;

        if (mapSelectNameText != null)
            mapSelectNameText.text = map.mapName;

        if (mapConfirmButtonText != null)
        {
            bool alreadySelected = previewMapIndex == gameSettings.selectedMapIndex;
            mapConfirmButtonText.text = alreadySelected ? "已选择" : "选择";
        }
    }

    private VehicleInfo GetPreviewVehicle()
    {
        if (gameSettings == null || gameSettings.vehicleList == null || gameSettings.vehicleList.Count == 0)
            return null;
        return gameSettings.vehicleList[Mathf.Clamp(previewVehicleIndex, 0, gameSettings.vehicleList.Count - 1)];
    }

    private MapInfo GetPreviewMap()
    {
        if (gameSettings == null || gameSettings.mapList == null || gameSettings.mapList.Count == 0)
            return null;
        return gameSettings.mapList[Mathf.Clamp(previewMapIndex, 0, gameSettings.mapList.Count - 1)];
    }

    private void UpdatePreviewModel(VehicleInfo vehicle = null)
    {
        if (previewRoot == null)
            return;

        if (vehicle == null)
            vehicle = gameSettings?.SelectedVehicle;

        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }

        if (vehicle == null || vehicle.previewPrefab == null)
            return;

        currentPreviewInstance = Instantiate(vehicle.previewPrefab, previewRoot);
        currentPreviewInstance.transform.localPosition = Vector3.zero;
        currentPreviewInstance.transform.localRotation = Quaternion.identity;
        currentPreviewInstance.transform.localScale = Vector3.one;

        // 确保预览模型有旋转组件
        if (currentPreviewInstance.GetComponent<RotatingPreview>() == null)
        {
            currentPreviewInstance.AddComponent<RotatingPreview>();
        }
    }

    private void MoveCameraTo(Transform target)
    {
        if (mainCamera == null || target == null)
            return;

        targetCameraPosition = target.position;
        targetCameraRotation = target.rotation;
        isCameraMoving = true;
    }

    private void ShowMainPanel()
    {
        SetPanelState(mainPanel, true);
        SetPanelState(vehicleSelectPanel, false);
        SetPanelState(mapSelectPanel, false);
        MoveCameraTo(menuCameraTransform);
    }

    private void ShowVehicleSelectPanel()
    {
        SetPanelState(mainPanel, false);
        SetPanelState(vehicleSelectPanel, true);
        SetPanelState(mapSelectPanel, false);
    }

    private void ShowMapSelectPanel()
    {
        SetPanelState(mainPanel, false);
        SetPanelState(vehicleSelectPanel, false);
        SetPanelState(mapSelectPanel, true);
    }

    private void SetPanelState(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
