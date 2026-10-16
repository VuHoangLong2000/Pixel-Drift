using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * LOADING TASK
 *
 * Đại diện cho một tác vụ loading.
 *
 * Task KHÔNG thực hiện việc load.
 * Nó chỉ nhận và lưu trạng thái progress.
 *
 * Ví dụ:
 *
 * LoadingTask task = LoadingManager.Instance.CreateTask(
 *     "LoadData",
 *     "Loading Data"
 * );
 *
 * task.SetProgress(0.5f);
 * task.SetProgress(1f);
 *
 * Khi Progress đạt 1:
 * - Task tự Complete().
 * - LoadingManager tự remove task.
 */
public class LoadingTask : MonoBehaviour
{
    private readonly string _id;
    private readonly string _displayName;

    private float _progress;
    private bool _completed;

    public string ID => _id;
    public string DisplayName => _displayName;
    public float Progress => _progress;
    public bool IsCompleted => _completed;

    public event Action<LoadingTask> OnProgressChanged;
    public event Action<LoadingTask> OnCompleted;

    internal LoadingTask(string id, string displayName)
    {
        _id = id;
        _displayName = displayName;
    }
    public void SetProgress(float progress)
    {
        if (_completed)
            return;

       _progress = Mathf.Clamp(progress, 0f, 1f);

        OnProgressChanged?.Invoke(this);

        if (_progress >= 1f)
            Complete();
    }
    public void Complete()
    {
        if (_completed)
            return;

        _progress = 1f;
        _completed = true;

        OnProgressChanged?.Invoke(this);
        OnCompleted?.Invoke(this);
    }
    public void Reset()
    {
        _progress = 0f;
        _completed = false;
    }
}
