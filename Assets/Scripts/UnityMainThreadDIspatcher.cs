using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    static readonly Queue<Action> _q = new Queue<Action>();
    static UnityMainThreadDispatcher _instance;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
            Debug.LogError("Dispatcher has not been initialised!");
        return _instance;
    }
    public static bool Exists => _instance != null;

    public static void Enqueue(Action a)
    {
        lock (_q) _q.Enqueue(a);
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    void Update()
    {
        lock (_q)
            while (_q.Count > 0)
                _q.Dequeue().Invoke();
    }
}
