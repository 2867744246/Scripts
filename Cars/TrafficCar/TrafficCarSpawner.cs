using UnityEngine;

/// <summary>
/// 交通车辆生成器
/// 负责计算生成位置和初始化车辆
/// </summary>
public class TrafficCarSpawner
{
    private LaneSystem laneSystem;
    private Transform playerTransform;
    private float spawnOffset;
    private float spawnPositionY;
    private float speedVariation;

    /// <summary>
    /// 初始化生成器
    /// </summary>
    public void Initialize(LaneSystem laneSystem, Transform playerTransform, float spawnOffset, float spawnPositionY, float speedVariation)
    {
        this.laneSystem = laneSystem;
        this.playerTransform = playerTransform;
        this.spawnOffset = spawnOffset;
        this.spawnPositionY = spawnPositionY;
        this.speedVariation = speedVariation;
    }

    /// <summary>
    /// 计算安全的生成位置
    /// </summary>
    public Vector3 CalculateSpawnPosition(int laneIndex)
    {
        float spawnX;
        float spawnY = spawnPositionY;
        float spawnZ = 0f;

        if (playerTransform != null)
        {
            // 在玩家前方生成
            spawnX = playerTransform.position.x + spawnOffset;

            // 保持与玩家相同的 Y 和 Z(假设玩家在道路上)
            spawnY = playerTransform.position.y;
            spawnZ = laneSystem.GetLanePosition(laneIndex);
        }
        else
        {
            // 如果没有玩家引用，使用固定起点
            spawnX = spawnOffset;
            // 即使没有玩家，也使用 laneSystem 计算 Z 坐标
            if (laneSystem != null)
            {
                spawnZ = laneSystem.GetLanePosition(laneIndex);
            }
        }

        Vector3 pos = new Vector3(spawnX, spawnY, spawnZ);

        Debug.Log($"计算生成位置：X={spawnX:F2}, Y={spawnY:F2}, Z={spawnZ:F2}");

        return pos;
    }

    /// <summary>
    /// 初始化车辆
    /// </summary>
    public void InitializeCar(TrafficCar car)
    {
        // 先确定随机车道
        int randomLane = laneSystem.GetRandomLane();
        car.currentLane = randomLane;
        
        // 随机速度差异
        float speedMultiplier = Random.Range(1f - speedVariation / 100f, 1f + speedVariation / 100f);
        car.Initialize(speedMultiplier);

        // 设置位置（使用确定的车道）
        car.transform.position = CalculateSpawnPosition(randomLane);
        
        //检测生成位置周围是否有车辆
        if (Physics.CheckSphere(car.transform.position, 10f, LayerMask.GetMask("Vehicle")))
        {
            car.ReturnToPool();
            Debug.LogWarning($"位置被占用，放弃生成车辆");
            return; // 位置被占用，放弃生成
        }

        // 激活车辆
        car.gameObject.SetActive(true);

        // 再次确认激活状态
        if (!car.gameObject.activeSelf)
        {
            Debug.LogError($"严重：车辆 {car.name} 未能激活！请检查预制体设置。");
        }
        else
        {
            Debug.Log($"✅ 车辆 {car.name} 已成功激活，activeSelf={car.gameObject.activeSelf}");
        }
    }
}