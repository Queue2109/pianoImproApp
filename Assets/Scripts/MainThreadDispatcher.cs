using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows code running on background threads to safely
/// enqueue actions for execution on Unity's main thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;

    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                // Look for an existing instance in the scene
                // using the newer, recommended API:
                _instance = FindAnyObjectByType<MainThreadDispatcher>();

                // If none found, create a new one
                if (_instance == null)
                {
                    var obj = new GameObject("MainThreadDispatcher");
                    _instance = obj.AddComponent<MainThreadDispatcher>();
                }
            }
            return _instance;
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
    void Update()
    {
        // Move all queued actions into a temporary list 
        // to execute them safely without locking the queue for a long time.
        Action[] actionsToRun = null;

        lock (_executionQueue)
        {
            if (_executionQueue.Count > 0)
            {
                actionsToRun = new Action[_executionQueue.Count];
                _executionQueue.CopyTo(actionsToRun, 0);
                _executionQueue.Clear();
            }
        }

        // Execute each action outside the lock
        if (actionsToRun != null)
        {
            foreach (var action in actionsToRun)
            {
                action.Invoke();
            }
        }
    }
}
