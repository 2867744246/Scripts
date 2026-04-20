using UnityEngine;

/// <summary>
/// TrafficCar 感知层：环境检测和信息收集。
/// </summary>
public partial class TrafficCar
{
    /// <summary>
    /// 感知环境，生成本帧只读输入。
    /// </summary>
    CarPerception SenseEnvironment()
    {
        bool hasFrontCar = TryGetFrontCarDistance(out float frontDistance);
        // 在两个车道之间切换：0 -> 1, 1 -> 0
        int desiredLane = (currentLane == 0) ? 1 : 0;

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
    /// 前向 BoxCast 检测最近前车。
    /// </summary>
    bool TryGetFrontCarDistance(out float distance)
    {
        // 获取标准化的道路前进方向向量
        Vector3 direction = GetNormalizedRoadDirection();
        // 获取碰撞盒的半尺寸(用于BoxCast)
        Vector3 halfExtents = GetBoxHalfExtents();
        // 计算BoxCast的起始位置:从车辆中心向上偏移一定高度
        Vector3 boxOrigin = transform.position + Vector3.up * detectHeightOffset;

        // 执行盒子投射检测,检测前方是否有其他车辆
        bool hasHit = Physics.BoxCast(
            boxOrigin,           // 投射起点
            halfExtents,         // 盒子半尺寸
            direction,           // 投射方向
            out RaycastHit hit,  // 碰撞信息输出
            Quaternion.identity, // 盒子旋转(无旋转)
            detectDistance,      // 检测距离
            carLayer,            // 检测层级(仅检测车辆)
            QueryTriggerInteraction.Ignore); // 忽略触发器

        // 验证检测结果是否有效:
        // 1. 必须有碰撞发生
        // 2. 碰撞体不能为空
        // 3. 不能是自己(排除自身碰撞)
        // 4. 碰撞点必须在车辆前方(通过点积判断方向)
        bool isValidFrontCar = hasHit &&
                               hit.collider != null &&
                               hit.collider.gameObject != gameObject &&
                               Vector3.Dot((hit.point - boxOrigin).normalized, direction) > 0f;

        // 如果检测到有效的前方车辆,记录实际距离;否则设置为最大检测距离
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
}
