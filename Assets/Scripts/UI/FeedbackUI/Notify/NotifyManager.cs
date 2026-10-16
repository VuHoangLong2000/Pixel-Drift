using System;
using System.Collections.Generic;
using UnityEngine;

public class NotifyManager : MonoBehaviour
{
    public static NotifyManager Instance { get; private set; }

    public event Action<NotifyData> OnNontify;

    private readonly List<NotifyData> _queue = new();

    public bool IsShowing { get; private set; }
    public int QueueCount => _queue.Count;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Show(string message, float duration = 2f, int priority = 0)
    {
        if (string.IsNullOrEmpty(message))
            return;

        NotifyData data = new(message, duration, priority);

        Enqueue(data);
    }

    public void Show(NotifyData data)
    {
        if (data == null | string.IsNullOrEmpty(data.message))
        {
            return;
        }

        Enqueue(data);
    }

    private void Enqueue(NotifyData data)
    {
        _queue.Add(data);

        _queue.Sort((a, b) => b.priority.CompareTo(a.priority));

        TryShowNext();
    }

    private void TryShowNext()
    {
        if (IsShowing || _queue.Count == 0)
            return;

        NotifyData data = _queue[0];
        _queue.RemoveAt(0);

        IsShowing = true;

        OnNontify?.Invoke(data);
    }

    public void Complete()
    {
        if (!IsShowing)
            return;

        IsShowing = false;

        TryShowNext();
    }

    public void Clear()
    {
        _queue.Clear();
    }
    public void ClearAndComplete()
    {
        _queue.Clear();
        IsShowing = false;
    }
}
