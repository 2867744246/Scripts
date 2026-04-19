using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池类
/// 管理对象的创建、获取和回收
/// </summary>
public class ObjectPool<T> where T : Component, IPoolable
{
    private Queue<T> availableObjects;
    private List<T> allObjects;
    private T prefab;
    private Transform parent;
    private int maxSize;
    private bool autoExpand;

    /// <summary>
    /// 初始化对象池
    /// </summary>
    public void Initialize(T prefab, Transform parent, int initialSize, int maxSize, bool autoExpand)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.maxSize = maxSize;
        this.autoExpand = autoExpand;

        availableObjects = new Queue<T>(initialSize);
        allObjects = new List<T>(initialSize);

        // 预加载对象
        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateNewObject();
            availableObjects.Enqueue(obj);
        }

        Debug.Log($"对象池已初始化：{availableObjects.Count}/{maxSize}");
    }

    /// <summary>
    /// 创建新对象
    /// </summary>
    private T CreateNewObject()
    {
        if (prefab == null)
        {
            Debug.LogError("预制体未指定！");
            return null;
        }

        T obj = Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(false);
        allObjects.Add(obj);

        return obj;
    }

    /// <summary>
    /// 从池中获取对象
    /// </summary>
    public T Get()
    {
        T obj;

        if (availableObjects.Count > 0)
        {
            obj = availableObjects.Dequeue();
        }
        else if (autoExpand && allObjects.Count < maxSize)
        {
            obj = CreateNewObject();
            Debug.Log($"对象池扩容：{allObjects.Count}/{maxSize}");
        }
        else
        {
            Debug.LogWarning("对象池已满！");
            return null;
        }

        obj.OnGetFromPool();
        return obj;
    }

    /// <summary>
    /// 回收对象到池中
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null || !allObjects.Contains(obj))
        {
            Debug.LogError("尝试回收不属于该池的对象！");
            return;
        }

        obj.OnReturnToPool();
        availableObjects.Enqueue(obj);
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void Clear()
    {
        foreach (T obj in allObjects)
        {
            if (obj != null)
                Object.Destroy(obj.gameObject);
        }

        allObjects.Clear();
        availableObjects.Clear();

        Debug.Log("对象池已清空");
    }

    /// <summary>
    /// 获取所有对象列表（用于回收器）
    /// </summary>
    public List<T> GetAllCars()
    {
        return allObjects;
    }

    /// <summary>
    /// 获取可用对象数量
    /// </summary>
    public int GetAvailableCount()
    {
        return availableObjects.Count;
    }

    /// <summary>
    /// 获取池状态信息
    /// </summary>
    public string GetPoolStatus()
    {
        int activeCount = 0;
        foreach (T obj in allObjects)
        {
            if (obj != null && obj.gameObject.activeSelf)
                activeCount++;
        }

        return $"对象池：可用={availableObjects.Count}, 使用中={activeCount}, 总计={allObjects.Count}/{maxSize}";
    }
}