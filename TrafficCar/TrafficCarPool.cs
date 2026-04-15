using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交通车辆对象池 - 管理 AI 车辆的生成和回收
/// </summary>
public class TrafficCarPool : MonoBehaviour
{
    [Header("对象池配置")]
    [Tooltip("车辆预制体引用")]
    public TrafficCar carPrefab;
    
    [Tooltip("初始池大小（预加载车辆数）")]
    public int initialSize = 10;
    
    [Tooltip("最大池容量")]
    public int maxSize = 30;
    
    [Tooltip("是否自动扩容")]
    public bool autoExpand = true;
    
    [Header("生成设置")]
    [Tooltip("生成点：玩家前方多少米")]
    public float spawnOffset = 200f;
    
    [Tooltip("前方回收点：玩家前方最大距离 (超出回收)")]
    public float frontDespawnOffset = 250f;
    
    [Tooltip("后方回收点：玩家后方最大距离 (掉队回收)")]
    public float backDespawnOffset = -40f;
    
    [Tooltip("最小安全距离 (前后车之间的距离)")]
    public float minSpawnDistance = 12f;
    
    [Tooltip("生成间隔时间 (秒)")]
    public float spawnInterval = 2f;

    [Tooltip("生成位置的 Y 坐标")]
    public float spawnPositionY = 2f; 
    
    [Header("回收检测")]
    [Tooltip("协程检查间隔时间 (秒)")]
    public float recycleCheckInterval = 0.5f;

    
    [Header("速度差异")]
    [Tooltip("速度变化范围（±km/h）")]
    public float speedVariation = 10f;
    
    // 私有变量
    private ObjectPool<TrafficCar> objectPool;
    private TrafficCarSpawner spawner;
    private TrafficCarRecycler recycler;
    private float nextSpawnTime;
    private LaneSystem laneSystem;
    private Transform playerTransform;
    private Transform despawnPoint; // 保留引用以防旧代码依赖，但优先使用动态计算
    private Coroutine recycleCoroutine; // 保留以兼容，但现在由 recycler 管理
    
    /// <summary>
    /// 单例模式
    /// </summary>
    private static TrafficCarPool instance;
    public static TrafficCarPool Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TrafficCarPool>();
                if (instance == null)
                {
                    Debug.LogError("TrafficCarPool 未找到！请确保场景中有该组件。");
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        // 确保单例
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePool();
    }
    
    void Start()
    {
        // 查找或创建 LaneSystem
        if (laneSystem == null)
        {
            laneSystem = FindObjectOfType<LaneSystem>();
            
            if (laneSystem == null)
            {
                GameObject laneObj = new GameObject("LaneSystem");
                laneSystem = laneObj.AddComponent<LaneSystem>();
                Debug.LogWarning("未找到 LaneSystem，已自动创建！");
            }
        }
        
        // 获取玩家位置引用
        CarController player = FindObjectOfType<CarController>();
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log($"TrafficCarPool 已找到玩家：{player.name}");
        }
        else
        {
            Debug.LogWarning("未找到玩家车辆，将使用固定位置生成！");
        }
                
        // 初始化生成器和回收器
        InitializeComponents();
    }
    
    void Update()
    {
        // 定时生成车辆
        if (Time.time >= nextSpawnTime)
        {
            TrySpawnCar();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    /// <summary>
    /// 初始化对象池
    /// </summary>
    public void InitializePool()
    {
        objectPool = new ObjectPool<TrafficCar>();
        objectPool.Initialize(carPrefab, transform, initialSize, maxSize, autoExpand);
    }
    
    /// <summary>
    /// 初始化生成器和回收器
    /// </summary>
    private void InitializeComponents()
    {
        // 初始化生成器
        spawner = new TrafficCarSpawner();
        spawner.Initialize(laneSystem, playerTransform, spawnOffset, spawnPositionY, speedVariation);
        
        // 初始化回收器
        recycler = new TrafficCarRecycler();
        recycler.Initialize(objectPool.GetAllCars(), playerTransform, frontDespawnOffset, backDespawnOffset, recycleCheckInterval, Return, this);
    }
    
    /// <summary>
    /// 尝试生成一辆车
    /// </summary>
    void TrySpawnCar()
    {
        TrafficCar car = objectPool.Get();
        if (car != null)
        {
            spawner.InitializeCar(car);
        }
    }
    
    /// <summary>
    /// 从池中获取车辆（外部调用）
    /// </summary>
    public TrafficCar Get(Vector3 position, Quaternion rotation)
    {
        TrafficCar car = objectPool.Get();
        if (car != null)
        {
            car.transform.position = position;
            car.transform.rotation = rotation;
        }
        return car;
    }
    
    /// <summary>
    /// 回收车辆到池中
    /// </summary>
    public void Return(TrafficCar car)
    {
        if (car == null) return;
        objectPool.Return(car);
        Debug.Log($"回收车辆，池中可用：{objectPool.GetAvailableCount()}");
    }
    
    /// <summary>
    /// 清空对象池
    /// </summary>
    public void Clear()
    {
        objectPool.Clear();
    }
    
    /// <summary>
    /// 获取池状态信息
    /// </summary>
    public string GetPoolStatus()
    {
        return objectPool.GetPoolStatus();
    }
    
    void OnDestroy()
    {
        if (recycler != null)
        {
            recycler.Stop();
        }
    }
    
}
