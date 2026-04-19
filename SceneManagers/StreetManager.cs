using System.Collections.Generic;
using UnityEngine;

public class StreetManager : MonoBehaviour
{
    [Header("道路参数")]
    public float streetLength = 40f;        // 每个道路块的长度（与预制体一致）

    [Header("移动触发参数")]
    public Transform cameraTransform;       // 相机的Transform（通常是主摄像机）
    
    [Tooltip("触发移动的提前量（相机距离道路边缘多少时触发）")]
    public float moveThreshold = -30f;       // 触发移动的提前量（相机距离道路右边缘多少时触发）

    private Queue<Transform> streetBlocks = new Queue<Transform>(); // 改为队列，更适合循环回收
    private float triggerPositionX;          // 当前最左侧道路块的左边缘X坐标

    void Start()
    {
        // 如果未指定相机，自动获取主摄像机
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
            else
                Debug.LogError("相机未指定，且场景中没有主摄像机！");
        }

        // 从当前游戏对象的子物体中获取所有道路块并入队
        foreach (Transform child in transform)
        {
            streetBlocks.Enqueue(child);
        }

        if (streetBlocks.Count == 0)
        {
            Debug.LogError("没有找到道路块，请将道路块作为本脚本的子物体！");
            return;
        }

        // 计算初始触发位置（最左侧道路块的左边缘）
        // 假设道路块的枢轴点在中心，左边缘 = position.x - roadLength/2
        triggerPositionX = streetBlocks.Peek().position.x + streetLength / 2f;
        Debug.Log("初始触发位置：" + triggerPositionX);
    }

    void Update()
    {
        // 当相机位置超过触发位置 + 道路长度 - 提前量时，移动道路块
        if (cameraTransform.position.x > triggerPositionX - moveThreshold)
        {
            MoveRoadBlock();
        }
    }

    void MoveRoadBlock()
    {
        // 从队头取出当前最左侧的道路块
        Transform blockToMove = streetBlocks.Dequeue();

        // 将其直接移动到最右侧：X坐标增加 道路长度 × 总块数
        blockToMove.position += Vector3.right * (streetLength * (streetBlocks.Count + 1));

        // 将移动后的块放回队尾
        streetBlocks.Enqueue(blockToMove);

        // 更新触发位置为新队头（当前最左侧块）的左边缘
        triggerPositionX = streetBlocks.Peek().position.x + streetLength / 2f;
        Debug.Log("移动道路块，新的触发位置：" + triggerPositionX);
    }
}