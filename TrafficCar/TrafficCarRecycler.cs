using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交通车辆回收器
/// 负责检测和回收超出范围的车辆
/// </summary>
public class TrafficCarRecycler
{
    private List<TrafficCar> allCars;
    private Transform playerTransform;
    private float frontDespawnOffset;
    private float backDespawnOffset;
    private float recycleCheckInterval;
    private System.Action<TrafficCar> onRecycle;

    private Coroutine recycleCoroutine;
    private MonoBehaviour coroutineHost;

    /// <summary>
    /// 初始化回收器
    /// </summary>
    public void Initialize(List<TrafficCar> allCars, Transform playerTransform, float frontDespawnOffset, float backDespawnOffset, float recycleCheckInterval, System.Action<TrafficCar> onRecycle, MonoBehaviour coroutineHost)
    {
        this.allCars = allCars;
        this.playerTransform = playerTransform;
        this.frontDespawnOffset = frontDespawnOffset;
        this.backDespawnOffset = backDespawnOffset;
        this.recycleCheckInterval = recycleCheckInterval;
        this.onRecycle = onRecycle;
        this.coroutineHost = coroutineHost;

        // 启动回收检测协程
        recycleCoroutine = coroutineHost.StartCoroutine(RecycleCheckRoutine());
        Debug.Log($"已启动回收检测协程，间隔：{recycleCheckInterval}秒");
    }

    /// <summary>
    /// 停止回收协程
    /// </summary>
    public void Stop()
    {
        if (recycleCoroutine != null)
        {
            coroutineHost.StopCoroutine(recycleCoroutine);
            recycleCoroutine = null;
        }
    }

    /// <summary>
    /// 协程：定期检查并回收超出范围的车辆
    /// </summary>
    private IEnumerator RecycleCheckRoutine()
    {
        WaitForSeconds waitTime = new WaitForSeconds(recycleCheckInterval);

        while (true)
        {
            yield return waitTime;

            if (playerTransform == null) continue;

            float playerX = playerTransform.position.x;
            float frontThreshold = playerX + frontDespawnOffset;
            float backThreshold = playerX + backDespawnOffset;

            // 遍历所有激活的车辆
            for (int i = 0; i < allCars.Count; i++)
            {
                TrafficCar car = allCars[i];

                if (car == null || !car.gameObject.activeSelf) continue;

                float carX = car.transform.position.x;

                // 检查是否超出回收区间
                if (carX > frontThreshold || carX < backThreshold)
                {
                    onRecycle(car);
                    i--; // 列表索引调整
                }
            }
        }
    }
}