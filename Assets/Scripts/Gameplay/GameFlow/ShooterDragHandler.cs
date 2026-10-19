using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class ShooterDragHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShooterUnit shooter;
    [SerializeField] private ShooterConveyorSpawner spawner;
    [SerializeField] private Camera targetCamera;

    [Header("Drag")]
    [SerializeField, Min(0.01f)]
    private float returnDuration = 0.2f;

    [SerializeField]
    private LayerMask raycastMask = Physics.DefaultRaycastLayers;

    private bool _isDragging;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Vector3 _dragOffset;
    private Plane _dragPlane;

    private int _activeTouchId = -1;

    public void Initialize(ShooterUnit shooterUnit, ShooterConveyorSpawner conveyorSpawner)
    {
        shooter = shooterUnit;
        spawner = conveyorSpawner;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Awake()
    {
        if (shooter == null)
            shooter = GetComponentInParent<ShooterUnit>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (shooter == null || spawner == null || targetCamera == null)
            return;

        if (!_isDragging)
        {
            if (TryGetPointerDown(out Vector2 pointerPosition))
                TryBeginDrag(pointerPosition);

            return;
        }

        if (TryGetPointerPosition(out Vector2 currentPosition))
            UpdateDrag(currentPosition);

        if (TryGetPointerUp(out Vector2 releasePosition))
            EndDrag(releasePosition);
    }

    private void TryBeginDrag(Vector2 pointerPosition)
    {
        if (!spawner.CanDragShooter(shooter))
            return;

        Ray ray = targetCamera.ScreenPointToRay(pointerPosition);

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                Mathf.Infinity,
                raycastMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // Chỉ bắt đầu kéo nếu chính Shooter này được raycast trúng.
        ShooterUnit hitShooter =
            hit.collider.GetComponentInParent<ShooterUnit>();

        if (hitShooter != shooter)
            return;

        _originalPosition = shooter.transform.position;
        _originalRotation = shooter.transform.rotation;

        // Mặt phẳng kéo nằm ngang, giữ nguyên độ cao ban đầu.
        _dragPlane = new Plane(
            Vector3.up,
            new Vector3(0f, _originalPosition.y, 0f));

        if (!_dragPlane.Raycast(ray, out float enter))
            return;

        Vector3 hitPoint = ray.GetPoint(enter);
        _dragOffset = _originalPosition - hitPoint;

        shooter.transform.DOKill();
        _isDragging = true;
    }

    private void UpdateDrag(Vector2 pointerPosition)
    {
        Ray ray = targetCamera.ScreenPointToRay(pointerPosition);

        if (!_dragPlane.Raycast(ray, out float enter))
            return;

        Vector3 position = ray.GetPoint(enter) + _dragOffset;
        shooter.transform.position = position;
    }

    private void EndDrag(Vector2 pointerPosition)
    {
        _isDragging = false;

        if (IsPointerOverDropZone(pointerPosition) &&
            spawner.TryLaunchShooter(shooter))
        {
            // Spawner đã nhận Shooter và đưa về đầu conveyor.
            return;
        }

        ReturnToOriginalSlot();
    }

    private bool IsPointerOverDropZone(Vector2 pointerPosition)
    {
        Ray ray = targetCamera.ScreenPointToRay(pointerPosition);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            Mathf.Infinity,
            raycastMask,
            QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {
            ConveyorDropZone zone =
                hit.collider.GetComponentInParent<ConveyorDropZone>();

            if (zone != null && zone.isActiveAndEnabled)
                return true;
        }

        return false;
    }

    private void ReturnToOriginalSlot()
    {
        shooter.transform.DOKill();

        shooter.transform
            .DOMove(_originalPosition, returnDuration)
            .SetEase(Ease.OutQuad);

        shooter.transform
            .DORotateQuaternion(_originalRotation, returnDuration)
            .SetEase(Ease.OutQuad);
    }

    private bool TryGetPointerDown(out Vector2 position)
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    _activeTouchId = touch.touchId.ReadValue();
                    position = touch.position.ReadValue();
                    return true;
                }
            }
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }

    private bool TryGetPointerPosition(out Vector2 position)
    {
        if (_activeTouchId >= 0 &&
            Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.touchId.ReadValue() == _activeTouchId)
                {
                    position = touch.position.ReadValue();
                    return true;
                }
            }

            position = default;
            return false;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }

    private bool TryGetPointerUp(out Vector2 position)
    {
        if (_activeTouchId >= 0 &&
            Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.touchId.ReadValue() != _activeTouchId)
                    continue;

                position = touch.position.ReadValue();

                bool released =
                    touch.press.wasReleasedThisFrame ||
                    touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled;

                if (released)
                    _activeTouchId = -1;

                return released;
            }

            _activeTouchId = -1;
            position = default;
            return true;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }

    private void OnDisable()
    {
        if (!_isDragging)
            return;

        _isDragging = false;

        // Không để Shooter bị bỏ lại ở vị trí đang kéo
        // nếu component bị tắt giữa thao tác.
        if (shooter != null &&
            spawner != null &&
            spawner.CanDragShooter(shooter))
        {
            shooter.transform.DOKill();
            shooter.transform.position = _originalPosition;
            shooter.transform.rotation = _originalRotation;
        }
    }
}
