using System;
using System.Collections.Concurrent;
using UnityEngine;

public class MainThreadQueue : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();
    private static MainThreadQueue instance;

    public static void Enqueue(Action action)
    {
        if (action == null)
            return;
        Queue.Enqueue(action);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        while (Queue.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError("MainThreadQueue error: " + ex.Message);
            }
        }
    }
}
