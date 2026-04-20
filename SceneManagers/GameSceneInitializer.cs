using UnityEngine;
using Cinemachine;
/// <summary>
/// 地图场景初始化器。
/// 进入地图场景后读取 GameSettings 中的车辆选择并实例化玩家车辆。
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    [Tooltip("玩家车辆生成点，若为空则使用本对象位置。")]
    public Transform playerSpawnPoint;
    [Tooltip("虚拟相机")]
    public CinemachineVirtualCamera virtualCamera;

    private void Awake()
    {
        if (GameSettings.Instance == null)
        {
            Debug.LogWarning("GameSettings 实例缺失，无法选择玩家车辆。");
            return;
        }

        VehicleInfo selectedVehicle = GameSettings.Instance.SelectedVehicle;
        if (selectedVehicle == null)
        {
            Debug.LogWarning("未选择车辆，无法生成玩家车辆。");
            return;
        }

        if (selectedVehicle.gamePrefab == null)
        {
            Debug.LogWarning($"车辆 {selectedVehicle.vehicleName} 未设置 gamePrefab，无法生成车辆。");
            return;
        }

        Transform spawn = playerSpawnPoint != null ? playerSpawnPoint : transform;
        
        GameObject playerVehicle = Instantiate(selectedVehicle.gamePrefab, spawn.position, spawn.rotation);
        SetCameraFollow(playerVehicle.transform);
        
        Time.timeScale = 1f;
    }

    private void SetCameraFollow(Transform target)
    {
        if (virtualCamera != null)
        {
            virtualCamera.Follow = target;
        }
    }
}
