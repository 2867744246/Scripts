using UnityEngine;

/// <summary>
/// TrafficCar 行为层：执行具体动作和运动控制。
/// </summary>
public partial class TrafficCar
{
    /// <summary>
    /// 执行命令，统一写入速度与横向位移。
    /// </summary>
    void ApplyCommand(CarCommand command, float dt)
    {
        float speedChangeRate = GetSpeedChangeRate();
        bool isBraking = false;
        if (command.longitudinalMode == LongitudinalMode.EmergencyBrake)
        {
            speedChangeRate = Mathf.Max(speedChangeRate, deceleration * 3f);
            isBraking = true;
        }

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            Mathf.Clamp(command.targetSpeed, minSpeed, maxSpeed),
            speedChangeRate * dt);
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        rb.velocity = GetNormalizedRoadDirection() * (currentSpeed / 3.6f);

        // 控制尾灯Emission
        if (brakeLightRenderer != null && brakeLightBlock != null)
        {
            brakeLightBlock.SetColor("_EmissionColor", isBraking ? Color.white : Color.black);
            brakeLightRenderer.SetPropertyBlock(brakeLightBlock, brakeLightMaterialIndex);
        }

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
}
