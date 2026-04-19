using UnityEngine;

/// <summary>
/// 可池化接口
/// 用于对象从池中获取和回收时的生命周期管理
/// </summary>
public interface IPoolable
{
    void OnGetFromPool();   // 从池获取时调用
    void OnReturnToPool();  // 回收到池时调用
}
