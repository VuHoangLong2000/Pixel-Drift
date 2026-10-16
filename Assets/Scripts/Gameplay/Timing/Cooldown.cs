using UnityEngine;

/*
 * COOLDOWN
 *
 * Dùng để giới hạn thời gian giữa các lần thực hiện một hành động.
 * Cooldown sử dụng TimerManager, không tự quản lý thời gian.
 *
 * ------------------------------------------------------------
 * CÁCH DÙNG
 * ------------------------------------------------------------
 *
 * Tạo cooldown:
 *
 * Cooldown cooldown =
 *     CooldownManager.Instance.Create(
 *         "Skill_Fireball",
 *         5f
 *     );
 *
 * Kiểm tra có thể sử dụng:
 *
 * if (cooldown.IsReady)
 * {
 *     CastFireball();
 *     cooldown.Start();
 * }
 *
 * ------------------------------------------------------------
 * KIỂM TRA TRẠNG THÁI
 * ------------------------------------------------------------
 *
 * cooldown.IsRunning
 * cooldown.IsReady
 * cooldown.Remaining
 * cooldown.Progress
 *
 * Progress:
 *
 * 0 = vừa bắt đầu
 * 1 = cooldown hoàn thành
 *
 * ------------------------------------------------------------
 * DỪNG COOLDOWN
 * ------------------------------------------------------------
 *
 * cooldown.Stop();
 *
 * ------------------------------------------------------------
 * DÙNG CHO
 * ------------------------------------------------------------
 *
 * - Skill
 * - Button
 * - Item
 * - Attack
 * - Gameplay action
 * - Chest
 * - Quest
 * - Bất kỳ hành động nào cần giới hạn thời gian
 *
 * ------------------------------------------------------------
 * LƯU Ý
 * ------------------------------------------------------------
 *
 * Cooldown không biết gameplay là gì.
 * Nó chỉ quản lý thời gian chờ.
 *
 * CooldownManager quản lý các cooldown bằng ID.
 *
 * Ví dụ:
 *
 * "Skill_Fireball"
 * "Skill_Heal"
 * "Chest_001"
 *
 * Không tạo Timer riêng cho Cooldown.
 * Cooldown luôn sử dụng TimerManager.
 */
public class Cooldown
{
    private readonly float _duration;
    private Timer _timer;

    public float Duration => _duration;
    public bool IsRunning => _timer != null && !_timer.IsComplete;
    public bool IsReady => !IsRunning;

    public float Remaining =>
        IsRunning ? _timer.Remaining : 0f;

    public float Progress =>
        IsRunning ? _timer.Progress : 1f;

    public Cooldown(float duration)
    {
        _duration = Mathf.Max(0f, duration);
    }

    public void Start()
    {
        Stop();

        if (_duration <= 0f)
            return;

        _timer = TimerManager.Instance.StartOnce(_duration, OnComplete);
    }

    public void Stop()
    {
        if (_timer == null)
            return;

        TimerManager.Instance.Cancel(_timer);
        _timer = null;
    }

    private void OnComplete()
    {
        _timer = null;
    }
}