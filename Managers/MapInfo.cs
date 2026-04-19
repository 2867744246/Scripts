using UnityEngine;

/// <summary>
/// 地图信息定义：用于菜单地图选择与场景切换。
/// </summary>
[System.Serializable]
public class MapInfo
{
    [Tooltip("地图名称，会显示在主界面和选择界面。")]
    public string mapName;

    [Tooltip("地图对应的场景名称，用于加载场景。")]
    public string sceneName;

    [Tooltip("地图选择图标，需要手动绑定。")]
    public Sprite icon;
}
