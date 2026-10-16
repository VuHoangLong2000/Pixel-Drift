using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
 * TIMER MANAGER
 *
 * Chức năng:
 * - Quản lý toàn bộ Timer runtime của game.
 * - Hỗ trợ timer chạy 1 lần hoặc lặp lại.
 * - Hỗ trợ Scaled và Unscaled time.
 * - Hỗ trợ Pause, Resume, Cancel.
 *
 * Cách dùng:
 *
 * Timer 1 lần:
 * Timer timer = TimerManager.Instance.Start(
 *     5f,
 *     OnComplete
 * );
 *
 * Timer lặp:
 * Timer timer = TimerManager.Instance.StartRepeat(
 *     1f,
 *     OnTick
 * );
 *
 * Unscaled time:
 * TimerManager.Instance.Start(
 *     5f,
 *     OnComplete,
 *     TimerTimeMode.Unscaled
 * );
 *
 * Pause / Resume:
 * timer.Pause();
 * timer.Resume();
 *
 * Cancel:
 * TimerManager.Instance.Cancel(timer);
 *
 * Cancel toàn bộ:
 * TimerManager.Instance.CancelAll();
 *
 * Dùng cho:
 * - Skill cooldown
 * - Buff / Debuff
 * - Gameplay countdown
 * - Spawn delay
 * - Repeat action
 * - UI countdown
 * - Respawn
 * - Combo timeout
 * - Các logic thời gian runtime khác
 *
 * Lưu ý:
 * - Không dùng cho thời gian cần lưu khi thoát game.
 * - Không dùng cho thời gian phụ thuộc server / Backend.
 * - Timer chỉ quản lý thời gian runtime.
 */

public enum TimerMode
{
    Once, //Chế độ 1 lần
    Repeat, //Chế độ lặp lại
}
public enum TimerTimeMode
{
    Scaled,
    Unscaled,
}
public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance;

    private readonly List<Timer> _timers = new();

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

    private void Update()
    {
        float scaleDelta = Time.deltaTime;
        float unscaleDelta = Time.unscaledDeltaTime;

        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            Timer timer = _timers[i];

            if (timer.IsComplete)
            {
                _timers.RemoveAt(i);
                continue;
            }
            timer.Tick(timer.TimeMode == TimerTimeMode.Scaled ? scaleDelta : unscaleDelta);
        }
    }
    public Timer StartOnce(float duration, Action onComplete, TimerTimeMode timeMode = TimerTimeMode.Scaled)
    {
        Timer timer = new(duration, TimerMode.Once, timeMode, onComplete, null);

        _timers.Add(timer);
        return timer;
    }
    public Timer StartRepeat(float interval, Action onTick, TimerTimeMode timeMode = TimerTimeMode.Scaled)
    {
        Timer timer = new(interval, TimerMode.Repeat, timeMode, null, onTick);
        _timers.Add(timer);
        return timer;
    }
    public void Cancel(Timer timer)
    {
        if (timer == null)
            return;

        timer.Cancel();
        _timers.Remove(timer);
    }

    public void CancelAll()
    {
        for (int i = 0; i < _timers.Count; i++)
            _timers[i].Cancel();

        _timers.Clear();
    }
}
