using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Paging
/// ------------------------------------------------------------
/// Chỉ chịu trách nhiệm phát tín hiệu khi ScrollRect
/// đạt đến ngưỡng để yêu cầu load thêm dữ liệu.
/// ------------------------------------------------------------
/// Sau khi xử lý xong callback, nhớ gọi Unlock().
/// </summary>
public enum PagingDirection
{
    Vertical,
    Horizontal
}
[RequireComponent(typeof(ScrollRect))]
public class Paging : MonoBehaviour
{
    [Header("Reference:")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Setting:")]
    [SerializeField] private PagingDirection direction = PagingDirection.Vertical;

    [SerializeField, Range(0, 1)]
    [Tooltip("Spawn khi đạt giá trị.")]
    private float threshold = 0.1f;

    [SerializeField]
    [Tooltip("Chống spam vấn đề spawn khi đang chờ load.")]
    private bool autoLock = true;

    [SerializeField]
    [Tooltip("Phải rời khỏi threshold mới được trigger lần tiếp theo.")]
    private bool requireLeaveThreshold = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onReachThreshold;

    private bool _waiting;
    private bool _paused;
    private bool _hasTriggered;

    private float _lastPosition;

    public bool IsWaiting => _waiting;
    public bool IsPaused => _paused;

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        ResetPaging();
    }

    private void OnEnable()
    {
        scrollRect.onValueChanged.AddListener(OnScroll);
    }
    private void OnDisable()
    {
        scrollRect.onValueChanged.RemoveListener(OnScroll);
    }
    private void OnScroll(Vector2 value)
    {
        if (_paused || _waiting)
            return;

        switch (direction)
        {
            case PagingDirection.Vertical:
                CheckVertical(value.y);
                break;

            case PagingDirection.Horizontal:
                CheckHorizontal(value.x);
                break;
        }
    }
    private void CheckVertical(float value)
    {
        // Chỉ trigger khi kéo xuống
        if (value > _lastPosition)
        {
            _lastPosition = value;

            if (value > threshold)
                _hasTriggered = false;

            return;
        }

        _lastPosition = value;

        if (value <= threshold)
        {
            if (requireLeaveThreshold && _hasTriggered)
                return;

            Trigger();
        }
    }

    private void CheckHorizontal(float value)
    {
        // Chỉ trigger khi kéo sang phải
        if (value < _lastPosition)
        {
            _lastPosition = value;

            if (value < 1f - threshold)
                _hasTriggered = false;

            return;
        }

        _lastPosition = value;

        if (value >= 1f - threshold)
        {
            if (requireLeaveThreshold && _hasTriggered)
                return;

            Trigger();
        }
    }
    private void Trigger()
    {
        if (_paused)
            return;

        _hasTriggered = true;

        if (autoLock)
            _waiting = true;

        onReachThreshold?.Invoke();
    }
    public void ForceTrigger()
    {
        if (_paused)
            return;

        Trigger();
    }
    public void Unlock()
    {
        _waiting = false;
    }
    public void Lock()
    {
        _waiting = true;
    }
    public void Resume()
    {
        _paused = false;
    }
    public void Pause()
    {
        _paused = true;
    }
    public void ResetPaging()
    {
        scrollRect.StopMovement();

        _waiting = false;
        _paused = false;
        _hasTriggered = false;

        _lastPosition = direction == PagingDirection.Vertical
             ? scrollRect.verticalNormalizedPosition
             : scrollRect.horizontalNormalizedPosition;
    }
}
