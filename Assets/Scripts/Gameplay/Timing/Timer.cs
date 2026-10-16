using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * TIMER
 * Đại diện cho một Timer đang chạy trong TimerManager.
 * Thông tin có thể lấy:
 * timer.Remaining
 *     → Thời gian còn lại.
 * timer.Duration
 *     → Thời gian ban đầu.
 * timer.Progress
 *     → Tiến trình từ 0 → 1.
 * timer.IsComplete
 *     → Timer đã hoàn thành / bị cancel.
 * timer.IsPaused
 *     → Timer đang pause.
 * Điều khiển:
 * timer.Pause();
 *     → Tạm dừng.
 * timer.Resume();
 *     → Tiếp tục.
 * timer.Cancel();
 *     → Hủy Timer.
 * Timer được tạo thông qua TimerManager.
 * Không tạo Timer trực tiếp từ gameplay.
 */
public class Timer
{
    private readonly float _duration;
    private readonly TimerMode _mode;
    private readonly Action _onComplete;
    private readonly Action _onTick;

    private float _remaining;
    private bool _paused;

    public TimerTimeMode TimeMode { get; }
    public bool IsComplete { get; private set; }
    public bool IsPaused => _paused;

    public float Remaining => _remaining;
    public float Duration => _duration;
    public float Progress => _duration <= 0f ? 1f : 1f - (_remaining / _duration);

    internal Timer(float duration, TimerMode mode, TimerTimeMode timeMode, Action onComplete, Action onTick)
    {
        _duration = Mathf.Max(0f, duration);
        _remaining = _duration;
        _mode = mode;
        TimeMode = timeMode;
        _onComplete = onComplete;
        _onTick = onTick;
    }
    internal void Tick(float deltaTime)
    {
        if (IsComplete || _paused)
            return;

        _remaining -= deltaTime;

        if (_remaining > 0f)
            return;

        switch (_mode)
        {
            case TimerMode.Once:
                Complete();
                break;

            case TimerMode.Repeat:
                _onTick?.Invoke();

                _remaining += _duration;

                if (_remaining <= 0f)
                    _remaining = _duration;

                break;
        }
    }
    public void Pause()
    {
        _paused = true;
    }

    public void Resume()
    {
        _paused = false;
    }

    public void Cancel()
    {
        IsComplete = true;
    }

    private void Complete()
    {
        if (IsComplete)
            return;

        IsComplete = true;
        _onComplete?.Invoke();
    }
}
