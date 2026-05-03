using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 跨线程主线程调度器
/// 直接挂载到 MastManager 即可，无需自动创建新对象
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _queue = new Queue<Action>();
    private readonly object _lock = new object();

    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            // 不自动创建新对象，只返回已有实例
            if (_instance == null)
                Debug.LogWarning("[MAST] UnityMainThreadDispatcher not found. Make sure it is attached to MastManager.");
            return _instance;
        }
    }

    void Awake()
    {
        // 如果场景中已有另一个实例，销毁自身（防重复）
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
        // 不调用 DontDestroyOnLoad，跟随 MastManager 的生命周期即可
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_lock) { _queue.Enqueue(action); }
    }

    void Update()
    {
        lock (_lock)
        {
            while (_queue.Count > 0)
            {
                try { _queue.Dequeue()?.Invoke(); }
                catch (Exception e) { Debug.LogError("[Dispatcher] Error: " + e.Message); }
            }
        }
    }
}
