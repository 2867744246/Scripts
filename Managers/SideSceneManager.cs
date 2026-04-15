using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 两侧场景管理器。
/// 负责按照道路管理器的循环逻辑，同时管理道路左右两侧的场景块复用。
/// </summary>
public class SideSceneManager : MonoBehaviour
{
    [Header("场景块参数")]
    [Tooltip("单个场景块沿 X 轴的长度，需与场景实际摆放长度一致。")]
    public float sceneBlockLength = 40f;

    [Header("移动触发参数")]
    [Tooltip("用于触发场景块循环的相机 Transform，未指定时会自动尝试获取主相机。")]
    public Transform cameraTransform;

    [Tooltip("提前触发场景块移动的距离。")]
    public float moveThreshold = -30f;

    [Header("调试参数")]
    [Tooltip("是否启用排查日志，启用后会输出初始化、触发和移动过程信息。")]
    public bool enableDebugLog = false;

    [Header("左右场景父节点")]
    [Tooltip("左侧场景块父节点，脚本会按子物体层级顺序收集场景块。")]
    public Transform leftSceneRoot;

    [Tooltip("右侧场景块父节点，脚本会按子物体层级顺序收集场景块。")]
    public Transform rightSceneRoot;

    /// <summary>
    /// 左侧场景块循环队列。
    /// </summary>
    private readonly Queue<Transform> leftSceneBlocks = new Queue<Transform>();

    /// <summary>
    /// 右侧场景块循环队列。
    /// </summary>
    private readonly Queue<Transform> rightSceneBlocks = new Queue<Transform>();

    /// <summary>
    /// 左侧场景块的触发移动 X 坐标。
    /// 以当前左侧队首场景块的右边缘为基准计算。
    /// </summary>
    private float leftTriggerPositionX;

    /// <summary>
    /// 右侧场景块的触发移动 X 坐标。
    /// 以当前右侧队首场景块的右边缘为基准计算。
    /// </summary>
    private float rightTriggerPositionX;

    /// <summary>
    /// 左侧接近触发阈值时，是否已经输出过提示日志。
    /// 用于避免未触发阶段逐帧刷屏。
    /// </summary>
    private bool hasLoggedLeftNearTrigger;

    /// <summary>
    /// 右侧接近触发阈值时，是否已经输出过提示日志。
    /// 用于避免未触发阶段逐帧刷屏。
    /// </summary>
    private bool hasLoggedRightNearTrigger;

    /// <summary>
    /// 初始化左右场景块数据与触发位置。
    /// </summary>
    private void Start()
    {
        if (!TrySetupCamera())
        {
            return;
        }

        if (!TryCollectSceneBlocks(leftSceneRoot, leftSceneBlocks, "左侧"))
        {
            return;
        }

        if (!TryCollectSceneBlocks(rightSceneRoot, rightSceneBlocks, "右侧"))
        {
            return;
        }

        UpdateTriggerPosition(leftSceneBlocks, true);
        UpdateTriggerPosition(rightSceneBlocks, false);

        LogDebug("初始化完成，sceneBlockLength=" + sceneBlockLength + "，moveThreshold=" + moveThreshold);
        LogQueueSnapshot(leftSceneBlocks, true, leftSceneRoot);
        LogQueueSnapshot(rightSceneBlocks, false, rightSceneRoot);
        LogDebug("初始化触发点，左侧=" + leftTriggerPositionX + "，右侧=" + rightTriggerPositionX);
    }

    /// <summary>
    /// 持续检测相机位置，按需移动左右两侧场景块。
    /// </summary>
    private void Update()
    {
        if (cameraTransform == null)
        {
            return;
        }

        TryMoveSideBlocks(leftSceneBlocks, true);
        TryMoveSideBlocks(rightSceneBlocks, false);
    }

    /// <summary>
    /// 尝试获取相机引用。
    /// </summary>
    /// <returns>是否成功获取可用相机。</returns>
    private bool TrySetupCamera()
    {
        if (cameraTransform != null)
        {
            return true;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("两侧场景管理器未指定相机，且场景中没有主相机！");
            return false;
        }

        cameraTransform = mainCamera.transform;
        return true;
    }

    /// <summary>
    /// 从指定父节点收集场景块。
    /// </summary>
    /// <param name="root">场景块父节点。</param>
    /// <param name="targetQueue">用于保存场景块的目标队列。</param>
    /// <param name="sideName">当前侧边名称，仅用于日志提示。</param>
    /// <returns>是否成功收集到至少一个场景块。</returns>
    private bool TryCollectSceneBlocks(Transform root, Queue<Transform> targetQueue, string sideName)
    {
        if (root == null)
        {
            Debug.LogError(sideName + "场景父节点未指定，请在 Inspector 中绑定对应节点！");
            return false;
        }

        targetQueue.Clear();

        foreach (Transform child in root)
        {
            targetQueue.Enqueue(child);
        }

        if (targetQueue.Count == 0)
        {
            Debug.LogError(sideName + "场景父节点下没有找到任何场景块，请检查子物体配置！");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 尝试移动单侧场景块。
    /// </summary>
    /// <param name="sceneBlocks">待检测的场景块队列。</param>
    /// <param name="isLeftSide">是否为左侧场景。</param>
    private void TryMoveSideBlocks(Queue<Transform> sceneBlocks, bool isLeftSide)
    {
        string sideName = GetSideName(isLeftSide);
        float triggerPositionX = isLeftSide ? leftTriggerPositionX : rightTriggerPositionX;
        float cameraPositionX = cameraTransform.position.x;
        float triggerCheckPositionX = triggerPositionX - moveThreshold;

        if (cameraPositionX <= triggerCheckPositionX)
        {
            TryLogNearTrigger(sceneBlocks, isLeftSide, sideName, cameraPositionX, triggerPositionX, triggerCheckPositionX);
            return;
        }

        ResetNearTriggerLogState(isLeftSide);
        LogTriggerHit(sceneBlocks, isLeftSide, sideName, cameraPositionX, triggerPositionX, triggerCheckPositionX);

        MoveSingleSideBlock(sceneBlocks, isLeftSide, sideName);
        UpdateTriggerPosition(sceneBlocks, isLeftSide);

        float newTriggerPositionX = isLeftSide ? leftTriggerPositionX : rightTriggerPositionX;
        LogDebug("[SideSceneManager][" + sideName + "] 场景块已循环移动，新的触发位置=" + newTriggerPositionX + "，新的队首=" + sceneBlocks.Peek().name);
    }

    /// <summary>
    /// 移动单侧的一个场景块。
    /// </summary>
    /// <param name="sceneBlocks">待移动的场景块队列。</param>
    /// <param name="isLeftSide">是否为左侧场景。</param>
    /// <param name="sideName">当前侧边名称。</param>
    private void MoveSingleSideBlock(Queue<Transform> sceneBlocks, bool isLeftSide, string sideName)
    {
        Transform blockToMove = sceneBlocks.Dequeue();
        float oldPositionX = blockToMove.position.x;
        float moveDistance = sceneBlockLength * (sceneBlocks.Count + 1);

        blockToMove.position += Vector3.right * moveDistance;
        sceneBlocks.Enqueue(blockToMove);

        LogMoveResult(sceneBlocks, isLeftSide, sideName, blockToMove, oldPositionX, moveDistance);
    }

    /// <summary>
    /// 根据当前队首场景块更新对应侧边的下一次触发位置。
    /// </summary>
    /// <param name="sceneBlocks">待更新触发点的场景块队列。</param>
    /// <param name="isLeftSide">是否为左侧场景。</param>
    private void UpdateTriggerPosition(Queue<Transform> sceneBlocks, bool isLeftSide)
    {
        float triggerPositionX = sceneBlocks.Peek().position.x + (sceneBlockLength / 2f);

        if (isLeftSide)
        {
            leftTriggerPositionX = triggerPositionX;
            return;
        }

        rightTriggerPositionX = triggerPositionX;
    }

    /// <summary>
    /// 输出统一格式的调试日志。
    /// </summary>
    /// <param name="message">日志内容。</param>
    private void LogDebug(string message)
    {
        if (!enableDebugLog)
        {
            return;
        }

        Debug.Log("[SideSceneManager] " + message);
    }

    /// <summary>
    /// 获取当前侧边名称。
    /// </summary>
    /// <param name="isLeftSide">是否为左侧场景。</param>
    /// <returns>侧边名称。</returns>
    private string GetSideName(bool isLeftSide)
    {
        return isLeftSide ? "左侧" : "右侧";
    }

    /// <summary>
    /// 输出指定侧边的队列快照，用于确认块顺序与坐标。
    /// </summary>
    /// <param name="sceneBlocks">场景块队列。</param>
    /// <param name="isLeftSide">是否为左侧场景。</param>
    /// <param name="root">对应父节点。</param>
    private void LogQueueSnapshot(Queue<Transform> sceneBlocks, bool isLeftSide, Transform root)
    {
        if (!enableDebugLog)
        {
            return;
        }

        string sideName = GetSideName(isLeftSide);
        Transform[] blockArray = sceneBlocks.ToArray();
        LogDebug("[" + sideName + "] 父节点=" + root.name + "，块数量=" + blockArray.Length + "。第 0 个块为当前队首。");

        for (int i = 0; i < blockArray.Length; i++)
        {
            Transform block = blockArray[i];
            LogDebug("[" + sideName + "] 队列索引=" + i + "，块名=" + block.name + "，X=" + block.position.x + "，层级索引=" + block.GetSiblingIndex());
        }
    }

    /// <summary>
    /// 在接近触发阈值但尚未触发时输出一次提示日志。
    /// </summary>
    /// <param name="sceneBlocks">场景块队列。</param>
    /// <param name="isLeftSide">是否为左侧场景。</param>
    /// <param name="sideName">侧边名称。</param>
    /// <param name="cameraPositionX">当前相机 X。</param>
    /// <param name="triggerPositionX">当前触发点。</param>
    /// <param name="triggerCheckPositionX">当前触发判断阈值。</param>
    private void TryLogNearTrigger(Queue<Transform> sceneBlocks, bool isLeftSide, string sideName, float cameraPositionX, float triggerPositionX, float triggerCheckPositionX)
    {
        if (!enableDebugLog)
        {
            return;
        }

        if (cameraPositionX < triggerCheckPositionX - (sceneBlockLength * 0.25f))
        {
            return;
        }

        bool hasLoggedNearTrigger = isLeftSide ? hasLoggedLeftNearTrigger : hasLoggedRightNearTrigger;
        if (hasLoggedNearTrigger)
        {
            return;
        }

        Transform headBlock = sceneBlocks.Peek();
        LogDebug("[" + sideName + "] 接近触发阈值，cameraX=" + cameraPositionX + "，triggerX=" + triggerPositionX + "，checkX=" + triggerCheckPositionX + "，当前队首=" + headBlock.name + "，队首X=" + headBlock.position.x);

        if (isLeftSide)
        {
            hasLoggedLeftNearTrigger = true;
            return;
        }

        hasLoggedRightNearTrigger = true;
    }

    /// <summary>
    /// 触发移动时输出关键判定日志。
    /// </summary>
    /// <param name="sceneBlocks">场景块队列。</param>
    /// <param name="isLeftSide">是否为左侧场景。</param>
    /// <param name="sideName">侧边名称。</param>
    /// <param name="cameraPositionX">当前相机 X。</param>
    /// <param name="triggerPositionX">当前触发点。</param>
    /// <param name="triggerCheckPositionX">当前触发判断阈值。</param>
    private void LogTriggerHit(Queue<Transform> sceneBlocks, bool isLeftSide, string sideName, float cameraPositionX, float triggerPositionX, float triggerCheckPositionX)
    {
        if (!enableDebugLog)
        {
            return;
        }

        Transform headBlock = sceneBlocks.Peek();
        LogDebug("[" + sideName + "] 触发移动，cameraX=" + cameraPositionX + "，triggerX=" + triggerPositionX + "，checkX=" + triggerCheckPositionX + "，当前队首=" + headBlock.name + "，队首X=" + headBlock.position.x);
    }

    /// <summary>
    /// 输出单次移动前后结果。
    /// </summary>
    /// <param name="sceneBlocks">移动后的场景块队列。</param>
    /// <param name="isLeftSide">是否为左侧场景。</param>
    /// <param name="sideName">侧边名称。</param>
    /// <param name="blockToMove">本次被移动的块。</param>
    /// <param name="oldPositionX">移动前 X。</param>
    /// <param name="moveDistance">本次移动距离。</param>
    private void LogMoveResult(Queue<Transform> sceneBlocks, bool isLeftSide, string sideName, Transform blockToMove, float oldPositionX, float moveDistance)
    {
        if (!enableDebugLog)
        {
            return;
        }

        Transform newHeadBlock = sceneBlocks.Peek();
        LogDebug("[" + sideName + "] 已移动块=" + blockToMove.name + "，移动前X=" + oldPositionX + "，移动距离=" + moveDistance + "，移动后X=" + blockToMove.position.x + "，新队首=" + newHeadBlock.name + "，新队首X=" + newHeadBlock.position.x);
    }

    /// <summary>
    /// 重置接近触发阈值的日志状态。
    /// </summary>
    /// <param name="isLeftSide">是否为左侧场景。</param>
    private void ResetNearTriggerLogState(bool isLeftSide)
    {
        if (isLeftSide)
        {
            hasLoggedLeftNearTrigger = false;
            return;
        }

        hasLoggedRightNearTrigger = false;
    }
}
