using UnityEngine;

/// <summary>
/// TrafficCar 状态机流水线：感知、决策、执行。
/// </summary>
public partial class TrafficCar
{
    #region 状态机流水线（Sense -> Decide -> Act）

    /// <summary>
    /// 感知环境，生成本帧只读输入。
    /// </summary>
    CarPerception SenseEnvironment()
    {
        bool hasFrontCar = TryGetFrontCarDistance(out float frontDistance);
        int desiredLane = -currentLane;

        return new CarPerception
        {
            hasFrontCar = hasFrontCar,
            frontCarDistance = hasFrontCar ? frontDistance : detectDistance,
            isCurrentLaneBlocked = hasFrontCar && frontDistance <= Mathf.Max(0.1f, followEnterDistance),
            canChangeLane = laneSystem != null && IsLaneSafe(desiredLane),
            desiredLane = desiredLane,
            shouldAttemptLaneChange = Time.time >= nextLaneChangeTime && Random.value < laneChangeProbability
        };
    }

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

    /// <summary>
    /// 执行命令，统一写入速度与横向位移。
    /// </summary>
    void ApplyCommand(CarCommand command, float dt)
    {
        float speedChangeRate = GetSpeedChangeRate();
        if (command.longitudinalMode == LongitudinalMode.EmergencyBrake)
        {
            speedChangeRate = Mathf.Max(speedChangeRate, deceleration * 3f);
        }

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            Mathf.Clamp(command.targetSpeed, minSpeed, maxSpeed),
            speedChangeRate * dt);
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        rb.velocity = GetNormalizedRoadDirection() * (currentSpeed / 3.6f);

        if (command.lateralMode == LateralMode.LaneChange)
        {
            UpdateLaneChangeMotion(dt);
        }
    }

    /// <summary>
    /// 初始化变道目标。
    /// </summary>
    void BeginLaneChange(int targetLane)
    {
        if (laneSystem == null || isChangingLane || targetLane == currentLane)
        {
            return;
        }

        float targetZ = laneSystem.GetLanePosition(targetLane);
        laneTargetPosition = new Vector3(transform.position.x, transform.position.y, targetZ);
        currentLane = targetLane;
        isChangingLane = true;
    }

    /// <summary>
    /// 变道过程中的横向移动。
    /// </summary>
    void UpdateLaneChangeMotion(float dt)
    {
        if (!isChangingLane)
        {
            return;
        }

        float nextZ = Mathf.MoveTowards(
            transform.position.z,
            laneTargetPosition.z,
            Mathf.Max(0.1f, laneChangeLateralSpeed) * dt);
        transform.position = new Vector3(transform.position.x, transform.position.y, nextZ);

        if (Mathf.Abs(transform.position.z - laneTargetPosition.z) < 0.02f)
        {
            transform.position = laneTargetPosition;
            isChangingLane = false;
        }
    }

    /// <summary>
    /// 前向 BoxCast 检测最近前车。
    /// </summary>
    bool TryGetFrontCarDistance(out float distance)
    {
        Vector3 direction = GetNormalizedRoadDirection();
        Vector3 halfExtents = GetBoxHalfExtents();
        Vector3 boxOrigin = transform.position + Vector3.up * detectHeightOffset + direction * boxHalfLength;

        bool hasHit = Physics.BoxCast(
            boxOrigin,
            halfExtents,
            direction,
            out RaycastHit hit,
            Quaternion.identity,
            detectDistance,
            carLayer,
            QueryTriggerInteraction.Ignore);

        bool isValidFrontCar = hasHit &&
                               hit.collider != null &&
                               hit.collider.gameObject != gameObject &&
                               Vector3.Dot((hit.point - boxOrigin).normalized, direction) > 0f;

        distance = isValidFrontCar ? hit.distance : detectDistance;
        return isValidFrontCar;
    }

    /// <summary>
    /// 检查目标车道是否安全。
    /// </summary>
    bool IsLaneSafe(int targetLane)
    {
        if (laneSystem == null)
        {
            return false;
        }

        float targetLaneZ = laneSystem.GetLanePosition(targetLane);
        Vector3 checkCenter = new Vector3(
            transform.position.x + 4f,
            transform.position.y + detectHeightOffset,
            targetLaneZ);
        Vector3 halfExtents = new Vector3(3f, Mathf.Max(0.5f, boxHalfHeight), 1.3f);

        Collider[] overlaps = Physics.OverlapBox(
            checkCenter,
            halfExtents,
            Quaternion.identity,
            carLayer,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (overlaps[i] != null && overlaps[i].gameObject != gameObject)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 前车距离映射目标跟车速度。
    /// </summary>
    float ComputeFollowTargetSpeed(float frontDistance)
    {
        float distance01 = Mathf.Clamp01(frontDistance / Mathf.Max(0.01f, detectDistance));
        return Mathf.Lerp(minSpeed, Mathf.Clamp(baseSpeed, minSpeed, maxSpeed), distance01);
    }

    /// <summary>
    /// 重置状态机运行时缓存。
    /// </summary>
    void ResetStateMachineData(float initialSpeed)
    {
        currentSpeed = Mathf.Clamp(initialSpeed, minSpeed, maxSpeed);
        currentState = CarBehaviorState.Cruise;
        stateEnterTime = Time.time;
        isChangingLane = false;
        laneTargetPosition = transform.position;
        nextLaneChangeTime = Time.time + laneChangeInterval;
        nextAllowedLaneChangeTime = Time.time;
        cachedCommand = new CarCommand
        {
            targetSpeed = currentSpeed,
            targetLane = currentLane,
            longitudinalMode = LongitudinalMode.Cruise,
            lateralMode = LateralMode.None
        };
    }

    #endregion
}
