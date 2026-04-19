using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 跨场景持久化游戏设置。
/// 由开始界面创建并保持整个游戏流程中唯一实例。
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("车辆配置")]
    [Tooltip("所有可供选择的车辆数据列表")] 
    public List<VehicleInfo> vehicleList = new List<VehicleInfo>();

    [Header("地图配置")]
    [Tooltip("所有可供选择的地图数据列表")] 
    public List<MapInfo> mapList = new List<MapInfo>();

    [HideInInspector]
    public int selectedVehicleIndex = 0;

    [HideInInspector]
    public int selectedMapIndex = 0;

    /// <summary>
    /// 当前选中的车辆信息。
    /// </summary>
    public VehicleInfo SelectedVehicle
    {
        get
        {
            if (vehicleList == null || vehicleList.Count == 0)
                return null;
            return vehicleList[Mathf.Clamp(selectedVehicleIndex, 0, vehicleList.Count - 1)];
        }
    }

    /// <summary>
    /// 当前选中的地图信息。
    /// </summary>
    public MapInfo SelectedMap
    {
        get
        {
            if (mapList == null || mapList.Count == 0)
                return null;
            return mapList[Mathf.Clamp(selectedMapIndex, 0, mapList.Count - 1)];
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 选择车辆索引。
    /// </summary>
    /// <param name="index">目标索引。</param>
    public void SetSelectedVehicle(int index)
    {
        if (vehicleList == null || vehicleList.Count == 0)
            return;

        selectedVehicleIndex = Mathf.Clamp(index, 0, vehicleList.Count - 1);
    }

    /// <summary>
    /// 选择地图索引。
    /// </summary>
    /// <param name="index">目标索引。</param>
    public void SetSelectedMap(int index)
    {
        if (mapList == null || mapList.Count == 0)
            return;

        selectedMapIndex = Mathf.Clamp(index, 0, mapList.Count - 1);
    }
}
