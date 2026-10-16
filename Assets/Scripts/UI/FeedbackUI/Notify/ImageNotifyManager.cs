using System;
using System.Collections.Generic;
using UnityEngine;

public class ImageNotifyManager : MonoBehaviour
{
    public static ImageNotifyManager Instance { get; private set; }

    public event Action<ImageNotifyData> OnNotify;

    private readonly List<ImageNotifyData> _queue = new();

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

    public void Show(Sprite image, float duration = 2f, float blinkInterval = 0.25f, int priority = 0)
    {
        if (image == null)
            return;

        ImageNotifyData data = new(image, duration, blinkInterval, priority);

        Enqueue(data);
    }

    public void Show(ImageNotifyData data)
    {
        if (data == null || data.image == null)
            return;

        Enqueue(data);
    }

    private void Enqueue(ImageNotifyData data)
    {
        _queue.Add(data);

        _queue.Sort((a, b) => b.priority.CompareTo(a.priority));

        TryShowNext();
    }

    private void TryShowNext()
    {
        if (IsShowing || _queue.Count == 0)
            return;

        ImageNotifyData data = _queue[0];
        _queue.RemoveAt(0);

        IsShowing = true;

        OnNotify?.Invoke(data);
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