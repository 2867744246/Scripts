using UnityEngine;

/// <summary>
/// TrafficCar 状态机字段声明：枚举、结构体和运行时状态。
/// </summary>
public partial class TrafficCar
{
    #region 状态机枚举

    /// <summary>
    /// 车辆行为状态
    /// </summary>
    private enum CarBehaviorState
    {
        Cruise,      // 巡航
        Follow,      // 跟车
        LaneChange,  // 变道
        EmergencyBrake  // 紧急刹车
    }

    /// <summary>
    /// 纵向行为模式
    /// </summary>
    private enum LongitudinalMode
    {
        Cruise,         // 巡航
        Follow,         // 跟车
        EmergencyBrake  // 紧急刹车
    }

    /// <summary>
    /// 横向行为模式
    /// </summary>
    private enum LateralMode
    {
        None,       // 无
        LaneChange  // 变道
    }

    #endregion

    #region 数据结构

    /// <summary>
    /// 感知信息
    /// </summary>
    private struct CarPerception
    {
        public bool hasFrontCar;              // 是否有前车
        public float frontCarDistance;        // 前车距离
        public bool isCurrentLaneBlocked;     // 当前车道是否阻塞
        public bool canChangeLane;            // 是否可以变道
        public int desiredLane;               // 目标车道
        public bool shouldAttemptLaneChange;  // 是否应尝试变道
    }

    /// <summary>
    /// 命令信息
    /// </summary>
    private struct CarCommand
    {
        public float targetSpeed;                    // 目标速度
        public int targetLane;                       // 目标车道
        public LongitudinalMode longitudinalMode;    // 纵向模式
        public LateralMode lateralMode;              // 横向模式
    }

    #endregion
}
