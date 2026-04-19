using UnityEngine;

/// <summary>
/// TrafficCar 决策层：状态判断和命令生成。
/// </summary>
public partial class TrafficCar
{
    /// <summary>
    /// 依据优先级选择目标状态。
    /// </summary>
    CarBehaviorState DecideState(CarPerception perception)
    {
        if (perception.hasFrontCar && perception.frontCarDistance <= Mathf.Max(0.1f, emergencyBrakeDistance))
        {
            return CarBehaviorState.EmergencyBrake;
        }

        if (isChangingLane)
        {
            return CarBehaviorState.LaneChange;
        }

        if (perception.isCurrentLaneBlocked &&
            perception.canChangeLane &&
            perception.shouldAttemptLaneChange &&
            Time.time >= nextAllowedLaneChangeTime)
        {
            return CarBehaviorState.LaneChange;
        }

        if (ShouldFollow(perception))
        {
            return CarBehaviorState.Follow;
        }

        return CarBehaviorState.Cruise;
    }

    /// <summary>
    /// 跟车进入/退出迟滞，避免状态抖动。
    /// </summary>
    bool ShouldFollow(CarPerception perception)
    {
        if (!perception.hasFrontCar)
        {
            return false;
        }

        float enterDistance = Mathf.Max(0.1f, followEnterDistance);
        float exitDistance = Mathf.Max(enterDistance + 0.1f, followExitDistance);
        float threshold = currentState == CarBehaviorState.Follow ? exitDistance : enterDistance;
        return perception.frontCarDistance <= threshold;
    }

    /// <summary>
    /// 状态切换保护：最短驻留时间 + 变道锁。
    /// </summary>
    bool CanTransitionTo(CarBehaviorState desiredState)
    {
        if (desiredState == currentState)
        {
            return false;
        }

        if (desiredState != CarBehaviorState.EmergencyBrake &&
            Time.time - stateEnterTime < Mathf.Max(0f, minStateHoldTime))
        {
            return false;
        }

        if (currentState == CarBehaviorState.LaneChange &&
            isChangingLane &&
            desiredState != CarBehaviorState.EmergencyBrake)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 进入状态时更新副作用与计时器。
    /// </summary>
    void EnterState(CarBehaviorState nextState, CarPerception perception)
    {
        currentState = nextState;
        stateEnterTime = Time.time;

        if (nextState == CarBehaviorState.LaneChange)
        {
            nextLaneChangeTime = Time.time + laneChangeInterval;
            nextAllowedLaneChangeTime = Time.time + laneChangeCooldown;
            BeginLaneChange(perception.desiredLane);
        }
        else if (nextState == CarBehaviorState.EmergencyBrake)
        {
            isChangingLane = false;
        }
    }

    /// <summary>
    /// 将状态映射为唯一执行命令。
    /// </summary>
    CarCommand BuildCommand(CarBehaviorState state, CarPerception perception)
    {
        CarCommand command = new CarCommand
        {
            targetSpeed = Mathf.Clamp(baseSpeed, minSpeed, maxSpeed),
            targetLane = currentLane,
            longitudinalMode = LongitudinalMode.Cruise,
            lateralMode = LateralMode.None
        };

        if (state == CarBehaviorState.EmergencyBrake)
        {
            command.targetSpeed = minSpeed;
            command.longitudinalMode = LongitudinalMode.EmergencyBrake;
            return command;
        }

        if (state == CarBehaviorState.LaneChange)
        {
            command.targetSpeed = perception.hasFrontCar
                ? ComputeFollowTargetSpeed(perception.frontCarDistance)
                : Mathf.Clamp(baseSpeed, minSpeed, maxSpeed);
            command.longitudinalMode = perception.hasFrontCar ? LongitudinalMode.Follow : LongitudinalMode.Cruise;
            command.lateralMode = LateralMode.LaneChange;
            return command;
        }

        if (state == CarBehaviorState.Follow)
        {
            command.targetSpeed = ComputeFollowTargetSpeed(perception.frontCarDistance);
            command.longitudinalMode = LongitudinalMode.Follow;
        }

        return command;
    }
}
