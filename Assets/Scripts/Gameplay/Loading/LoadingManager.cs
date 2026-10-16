using System.Collections.Generic;
using UnityEngine;

/*
 * LOADING MANAGER
 *
 * Chức năng:
 * - Quản lý các LoadingTask đang chạy.
 * - Tính Progress tổng của tất cả task.
 * - Không phụ thuộc Addressables, Backend, Data hay UI.
 *
 * FLOW:
 *
 * Feature / BE / Addressables / Data
 *              ↓
 *       CreateTask()
 *              ↓
 *       SetProgress()
 *              ↓
 *         Complete()
 *              ↓
 *       LoadingManager
 *              ↓
 *        Loading UI
 *
 * Cách dùng:
 *
 * LoadingTask task = LoadingManager.Instance.CreateTask(
 *     "LoadPlayerData",
 *     "Loading Player Data"
 * );
 *
 * task.SetProgress(0.5f);
 * task.Complete();
 *
 * Lấy progress tổng:
 *
 * float progress = LoadingManager.Instance.Progress;
 *
 * Kiểm tra đang loading:
 *
 * LoadingManager.Instance.IsLoading
 *
 * Lấy task:
 *
 * LoadingManager.Instance.TryGetTask("LoadPlayerData", out task);
 *
 * Lưu ý:
 * - LoadingManager KHÔNG thực hiện việc load.
 * - Nguồn dữ liệu tự cập nhật LoadingTask.
 * - Task hoàn thành sẽ tự được Manager remove.
 * - Dùng được cho Addressables, Backend, Local Data,
 *   Save, Asset hoặc bất kỳ hệ thống async nào.
 */
public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    private readonly Dictionary<string, LoadingTask> _tasks = new();

    public bool IsLoading => _tasks.Count > 0;

    public float Progress
    {
        get
        {
            if (_tasks.Count == 0)
                return 1;

            float total = 0;

            foreach (LoadingTask task in _tasks.Values)
            {
                total += task.Progress;
            }
            return total / _tasks.Count;
        }
    }
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

    public LoadingTask CreateTask(string id, string displayName = null)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (_tasks.TryGetValue(id, out LoadingTask exising))
            return exising;

        LoadingTask task = new(id, string.IsNullOrEmpty(displayName) ? id : displayName);

        task.OnCompleted += OnTaskCompleted;

        _tasks.Add(id, task);

        return task;
    }
    public bool TryGetTask(
        string id,
        out LoadingTask task)
    {
        return _tasks.TryGetValue(id, out task);
    }

    public void RemoveTask(string id)
    {
        if (!_tasks.TryGetValue(id, out LoadingTask task))
            return;

        task.OnCompleted -= OnTaskCompleted;
        _tasks.Remove(id);
    }

    public void Clear()
    {
        foreach (LoadingTask task in _tasks.Values)
            task.OnCompleted -= OnTaskCompleted;

        _tasks.Clear();
    }

    private void OnTaskCompleted(LoadingTask task)
    {
        if (!task.IsCompleted)
            return;

        RemoveTask(task.ID);
    }
}
