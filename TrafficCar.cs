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
    
    [Header("车道设置")]
    [Tooltip("当前车道（-1:左，1:右）")]
    public int currentLane = 1;
    
    [Tooltip("变道概率（0-1）")]
    public float laneChangeProbability = 0.3f;
    
    [Tooltip("变道间隔时间（秒）")]
    public float laneChangeInterval = 3f;
    
    [Header("引用")]
    public LaneSystem laneSystem;

    [Header("State Machine")]
    [Tooltip("Minimum state hold time (seconds)")]
    public float minStateHoldTime = 0.2f;

    [Tooltip("Follow enter distance (meters)")]
    public float followEnterDistance = 14f;

    [Tooltip("Follow exit distance (meters, should be larger than enter distance)")]
    public float followExitDistance = 18f;

    [Tooltip("Emergency brake distance (meters)")]
    public float emergencyBrakeDistance = 5f;

    [Tooltip("Lane change cooldown time (seconds)")]
    public float laneChangeCooldown = 2f;

    [Tooltip("Lane change lateral speed (m/s)")]
    public float laneChangeLateralSpeed = 4f;

    #endregion

    #region 状态机类型定义

    // 私有变量
    #endregion

    #region IPoolable 生命周期

    /// <summary>
    /// Main behavior state for the vehicle.
    /// </summary>
    private enum CarBehaviorState
    {
        Cruise,
        Follow,
        LaneChange,
        EmergencyBrake
    }

    /// <summary>
    /// Longitudinal motion mode.
    /// </summary>
    private enum LongitudinalMode
    {
        Cruise,
        Follow,
        EmergencyBrake
    }

    /// <summary>
    /// Lateral motion mode.
    /// </summary>
    private enum LateralMode
    {
        None,
        LaneChange
    }

    /// <summary>
    /// Read-only perception data built in Sense phase.
    /// </summary>
    private struct CarPerception
    {
        public bool hasFrontCar;
        public float frontCarDistance;
        public bool isCurrentLaneBlocked;
        public bool canChangeLane;
        public int desiredLane;
        public bool shouldAttemptLaneChange;
    }

    /// <summary>
    /// Command built in Decide phase and executed in Act phase.
    /// </summary>
    private struct CarCommand
    {
        public float targetSpeed;
        public int targetLane;
        public LongitudinalMode longitudinalMode;
        public LateralMode lateralMode;
    }

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
    public LayerMask carLayer;

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

        ResetStateMachineData(baseSpeed);
    }
    
    void Update()
    {
        // // 更新变道逻辑
        
        // // 平滑移动到目标车道位置
        // if (isChangingLane)
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

    void OnDrawGizmos()
    {
        // 可视化 BoxCast 检测走廊
        Vector3 direction = GetNormalizedRoadDirection();
        Vector3 halfExtents = GetBoxHalfExtents();
        Vector3 boxOrigin = transform.position + Vector3.up * detectHeightOffset + direction * boxHalfLength;
        Vector3 castCenter = boxOrigin + direction * detectDistance;
        
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(boxOrigin, halfExtents * 2f);
        Gizmos.DrawLine(boxOrigin, castCenter);
        Gizmos.DrawWireCube(castCenter, halfExtents * 2f);

        if (isChangingLane)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, laneTargetPosition);
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
    #endregion
    
    #region Legacy 兼容方法（仅保留，不参与主流程）
    /// <summary>
    /// 更新变道逻辑
    /// </summary>
    void LegacyUpdateLaneChange()
    {
        if (isChangingLane || Time.time < nextLaneChangeTime) return;
        
        // 随机决定是否变道
        if (Random.value < laneChangeProbability)
        {
            LegacyTryChangeLane();
        }
        
        nextLaneChangeTime = Time.time + laneChangeInterval;
    }
    
    /// <summary>
    /// 尝试变换车道
    /// </summary>
    void LegacyTryChangeLane()
    {
        if (laneSystem == null) return;
        
        // 切换到另一个车道
        int newLane = -currentLane;
        
        // 获取新车道的目标位置
        float targetX = laneSystem.GetLanePosition(newLane);
        laneTargetPosition = new Vector3(targetX, transform.position.y, transform.position.z);
        
        // 检查新车道是否安全（简单检测：目标位置附近没有其他车）
        if (!LegacyIsLaneSafe(newLane, targetX))
        {
            return;
        }
        
        // 开始变道
        currentLane = newLane;
        isChangingLane = true;
    }
    
    /// <summary>
    /// 检查新车道是否安全
    /// </summary>
    bool LegacyIsLaneSafe(int laneIndex, float targetX)
    {
        // 简化的安全检测：检查目标点前后是否有车
        float checkDistance = 8f;
        
        RaycastHit hitFront, hitBack;
        bool hasCarFront = Physics.Raycast(transform.position, Vector3.right, out hitFront, checkDistance, carLayer);
        bool hasCarBack = Physics.Raycast(transform.position, Vector3.left, out hitBack, checkDistance, carLayer);
        
        // 如果前后都有车，不变道
        if (hasCarFront || hasCarBack)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 平滑移动到车道目标位置
    /// </summary>
    void LegacySmoothMoveToLaneTarget()
    {
        Vector3 targetPos = laneTargetPosition;
        Vector3 direction = (targetPos - transform.position).normalized;
        
        // 横向移动速度
        float lateralSpeed = 5f;
        transform.position += direction * lateralSpeed * Time.deltaTime;
        
        // 检查是否到达目标
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            transform.position = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            isChangingLane = false;
        }
    }
    
    /// <summary>
    /// 检测前方车辆并调整跟车速度
    /// </summary>
    void LegacyDetectAndAdjustSpeed()
    {
        Vector3 direction = GetNormalizedRoadDirection();
        Vector3 halfExtents = GetBoxHalfExtents();
        Vector3 boxOrigin = transform.position + Vector3.up * detectHeightOffset + direction * boxHalfLength;
        
        RaycastHit hit;
        bool hasHit = Physics.BoxCast(
            boxOrigin,
            halfExtents,
            direction,
            out hit,
            Quaternion.identity,
            detectDistance,
            carLayer,
            QueryTriggerInteraction.Ignore);
        
        // 防止误命中自身或侧后方目标
        bool hasValidFrontCar = hasHit &&
                                hit.collider != null &&
                                hit.collider.gameObject != gameObject &&
                                Vector3.Dot((hit.point - boxOrigin).normalized, direction) > 0f;
        
        if (hasValidFrontCar)
        {
            float distance01 = Mathf.Clamp01(hit.distance / Mathf.Max(0.01f, detectDistance));
            float targetSpeed = Mathf.Lerp(minSpeed, baseSpeed, distance01);
            float speedChangeRate = GetSpeedChangeRate();
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.fixedDeltaTime);
        }
        else
        {
            // 前方无障碍，恢复巡航速度
            LegacyRestoreSpeed();
        }
        
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
    }

    #endregion
    
}
