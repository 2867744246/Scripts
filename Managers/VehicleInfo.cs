using UnityEngine;

/// <summary>
/// 车辆信息定义：用于开始界面、选择界面与游戏场景之间共享。
/// </summary>
[System.Serializable]
public class VehicleInfo
{
    [Tooltip("车辆名称，会显示在主界面和选择界面。")]
    public string vehicleName;

    [Tooltip("开始界面中当前选中车辆的预览模型预制体。")]
    public GameObject previewPrefab;

    [Tooltip("进入地图时实例化的玩家车辆预制体。")]
    public GameObject gamePrefab;

    [Tooltip("车辆选择界面下方固定图标，需要手动绑定。")]
    public Sprite icon;
}
