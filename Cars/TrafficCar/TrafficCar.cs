using UnityEngine;

/// <summary>
/// AI 交通车辆 - 使用对象池管理
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public partial class TrafficCar : MonoBehaviour, IPoolable
{
    #region Inspector 配置

    [Header("车辆属性")]
    [Tooltip("基础行驶速度 (km/h)")]
    public float baseSpeed = 50f;
    
    [Tooltip("最大速度 (km/h)")]
    public float maxSpeed = 80f;
    
    [Tooltip("最小速度（遇到慢车时）(km/h)")]
    public float minSpeed = 0f;
    
    [Header("避障设置")]
    [Tooltip("前方检测距离（米）")]
    public float detectDistance = 20f;
    
    [Tooltip("减速响应时间（秒）")]
    public float brakeResponseTime = 0.5f;
    
    [Tooltip("减速度（km/h 每秒）")]
    public float deceleration = 20f;
    
    [Tooltip("检测起点高度偏移（米，避免撞地）")]
    public float detectHeightOffset = 1f;
    
    [Tooltip("BoxCast 半宽（米）")]
    public float boxHalfWidth = 1f;
    
    [Tooltip("BoxCast 半高（米）")]
    public float boxHalfHeight = 0.8f;
    
    [Tooltip("BoxCast 半长（米）")]
    public float boxHalfLength = 2f;
    
    [Tooltip("道路全局方向（默认 X 轴正方向）")]
    public Vector3 roadDirection = Vector3.right;
    public LayerMask carLayer;
    
    [Header("车道设置")]
    [Tooltip("当前车道（0:左车道，1:右车道）")]
    public int currentLane = 0;
    
    [Tooltip("变道概率（0-1）")]
    public float laneChangeProbability = 0.3f;
    
    [Tooltip("变道间隔时间（秒）")]
    public float laneChangeInterval = 3f;
    

    [Header("引用")]
    public LaneSystem laneSystem;

    [Header("车轮碰撞体引用")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    [Header("轮子模型引用（用于同步旋转）")]
    public Transform frontLeftWheelModel;
    public Transform frontRightWheelModel;
    public Transform rearLeftWheelModel;
    public Transform rearRightWheelModel;

    [Header("材质引用")]
    public Renderer brakeLightRenderer;
    [Tooltip("尾灯材质在Renderer中的索引（如果有多个材质）")]
    public int brakeLightMaterialIndex = 1;

    [Header("State Machine")]
    [Tooltip("状态最小保持时间(秒)")]
    public float minStateHoldTime = 0.2f;

    [Tooltip("跟随进入距离(米)")]
    public float followEnterDistance = 14f;

    [Tooltip("跟随退出距离(米,应大于进入距离)")]
    public float followExitDistance = 18f;

    [Tooltip("紧急刹车距离(米)")]
    public float emergencyBrakeDistance = 5f;

    [Tooltip("变道冷却时间(秒)")]
    public float laneChangeCooldown = 2f;

    [Tooltip("变道横向速度(米/秒)")]
    public float laneChangeLateralSpeed = 4f;

    #endregion


    #region 运行时字段

    private Rigidbody rb;
    private float currentSpeed;
    private float nextLaneChangeTime;
    private float nextAllowedLaneChangeTime;
    private bool isChangingLane = false;
    private Vector3 laneTargetPosition;
    private TrafficCarPool currentPool;
    private CarBehaviorState currentState = CarBehaviorState.Cruise;
    private float stateEnterTime;
    private CarCommand cachedCommand;
    
    // 组件缓存
    private MaterialPropertyBlock brakeLightBlock;

    #endregion

    #region Unity 生命周期

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // 初始化车道掩码（假设车辆在"Vehicle"层，Layer 编号待设置）
        if (carLayer.value == 0)
        {
            carLayer = LayerMask.GetMask("Vehicle");
        }
        
        // 如果未指定 LaneSystem，尝试自动查找
        if (laneSystem == null)
        {
            laneSystem = FindObjectOfType<LaneSystem>();
        }

        // 初始化尾灯MaterialPropertyBlock
        if (brakeLightRenderer != null)
        {
            brakeLightBlock = new MaterialPropertyBlock();
        }

        ResetStateMachineData(baseSpeed);
    }
    
    void Update()
    {
        // // 更新变道逻辑
        
        // // 平滑移动到目标车道位置
        // if (isChangingLane)
    }
    
    void LateUpdate()
    {
        // 同步所有车轮模型的位置和旋转
        UpdateWheelModels();
    }
    
    void FixedUpdate()
    {
        // 检测前方车辆并调整速度
        CarPerception perception = SenseEnvironment();
        CarBehaviorState desiredState = DecideState(perception);
        if (CanTransitionTo(desiredState))
        {
            EnterState(desiredState, perception);
        }

        CarCommand command = BuildCommand(currentState, perception);
        ApplyCommand(command, Time.fixedDeltaTime);
        cachedCommand = command;
        
        // 应用速度（统一按道路方向移动）
    }

    /// <summary>
    /// 在编辑器中绘制 BoxCast 检测走廊的可视化表示
    /// </summary>
    void OnDrawGizmos()
    {
        // 可视化 BoxCast 检测走廊
        // 获取标准化的道路前进方向向量
        Vector3 direction = GetNormalizedRoadDirection();
        // 获取 BoxCast 的半尺寸
        Vector3 halfExtents = GetBoxHalfExtents();
        // 计算 BoxCast 的起始位置:从车辆中心向上偏移一定高度
        Vector3 boxOrigin = transform.position + Vector3.up * detectHeightOffset;
        // 计算 BoxCast 的中心位置
        Vector3 castCenter = boxOrigin + direction * detectDistance;
        
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.identity;
        // 绘制 BoxCast 的边框
        Gizmos.DrawWireCube(boxOrigin, halfExtents * 2f);
        // 绘制 BoxCast 的中心线
        Gizmos.DrawLine(boxOrigin, castCenter);
        // 绘制 BoxCast 的中心立方体
        Gizmos.DrawWireCube(castCenter, halfExtents * 2f);

        if (isChangingLane)
        {
            Gizmos.color = Color.cyan;
            // 绘制变道目标的线段
            Gizmos.DrawLine(transform.position, laneTargetPosition);
            // 绘制变道目标的球体
            Gizmos.DrawSphere(laneTargetPosition, 0.25f);
        }
    }

    #endregion

    #region 基础工具方法


    /// <summary>
    /// 恢复到巡航速度
    /// </summary>
    void LegacyRestoreSpeed()
    {
        float targetSpeed = Mathf.Min(baseSpeed, maxSpeed);
        float speedChangeRate = Mathf.Max(deceleration * 2f, GetSpeedChangeRate());
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.fixedDeltaTime);
    }

    /// <summary>
    /// 获取归一化后的道路方向，默认回退到 X 轴正方向。
    /// </summary>
    Vector3 GetNormalizedRoadDirection()
    {
        return roadDirection.sqrMagnitude > 0.0001f ? roadDirection.normalized : Vector3.right;
    }

    /// <summary>
    /// 获取 BoxCast 半尺寸，避免检测体出现零尺寸。
    /// </summary>
    Vector3 GetBoxHalfExtents()
    {
        return new Vector3(
            Mathf.Max(0.05f, boxHalfWidth),
            Mathf.Max(0.05f, boxHalfHeight),
            Mathf.Max(0.05f, boxHalfLength));
    }

    /// <summary>
    /// 根据刹车响应时间计算速度变化率（km/h/s）。
    /// </summary>
    float GetSpeedChangeRate()
    {
        float safeResponse = Mathf.Max(0.05f, brakeResponseTime);
        float speedRange = Mathf.Max(1f, baseSpeed - minSpeed);
        return Mathf.Max(deceleration, speedRange / safeResponse);
    }

    #endregion

    #region IPoolable 实现
    
    /// <summary>
    /// 从对象池获取时的初始化
    /// </summary>
    public void OnGetFromPool()
    {        
        
        gameObject.SetActive(true);
        
        // 确保 Rigidbody 引用存在
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            Debug.Log($"[OnGetFromPool] Rigidbody 组件已重新获取：{rb != null}");
        }
        
        // 重置状态
        ResetStateMachineData(baseSpeed);
        
        // 确保物理状态正确
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log($"[OnGetFromPool] 车辆 {gameObject.name} 完成激活，速度：{baseSpeed} km/h, 最终状态：activeSelf={gameObject.activeSelf}");
    }
    
    /// <summary>
    /// 回收到对象池时的清理
    /// </summary>
    public void OnReturnToPool()
    {
        gameObject.SetActive(false);
        
        // 确保 Rigidbody 引用存在
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        
        // 停止所有运动
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // 重置状态
        ResetStateMachineData(0f);
        
        Debug.Log($"车辆 {gameObject.name} 回收到池");
    }
    
    
    /// <summary>
    /// 设置车辆所属的对象池
    /// </summary>
    public void SetPool(TrafficCarPool pool)
    {
        currentPool = pool;
    }
    
    /// <summary>
    /// 主动回收车辆到对象池
    /// </summary>
    public void ReturnToPool()
    {
        if (currentPool != null)
        {
            currentPool.Return(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 初始化车辆（在生成时调用）
    /// </summary>
    public void Initialize(float speedMultiplier = 1f)
    {
        baseSpeed *= speedMultiplier;
        ResetStateMachineData(baseSpeed);
        
    }
    
    /// <summary>
    /// 同步所有车轮模型的位置和旋转
    /// </summary>
    void UpdateWheelModels()
    {
        UpdateWheelModel(frontLeftWheelCollider, frontLeftWheelModel);
        UpdateWheelModel(frontRightWheelCollider, frontRightWheelModel);
        UpdateWheelModel(rearLeftWheelCollider, rearLeftWheelModel);
        UpdateWheelModel(rearRightWheelCollider, rearRightWheelModel);
    }
    
    /// <summary>
    /// 同步轮子视觉模型与物理碰撞体的位置和旋转
    /// </summary>
    void UpdateWheelModel(WheelCollider collider, Transform model)
    {
        if (collider == null || model == null) return;

        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        
        // 同步世界位置与旋转
        model.position = pos;
        model.rotation = rot;

        // 叠加轮子自转（绕本地 X 轴），使用 rpm -> deg/s = rpm*6
        float degPerSec = collider.rpm * 6f;
        model.Rotate(Vector3.right, degPerSec * Time.deltaTime, Space.Self);
    }
    
    #endregion
    
 
}
