using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 10f;
    [SerializeField, Min(0.001f)] private float hitDistance = 0.05f;

    [Header("Visual")]
    [SerializeField] private Renderer[] colorRenderers;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _propertyBlock;

    private PixelBlock _target;
    private ShooterUnit _owner;
    private int _ownerShotToken;
    private bool _isActive;

    public bool Initialize(
        Vector3 startPosition,
        PixelBlock target,
        Color bulletColor,
        float speed,
        ShooterUnit owner,
        int ownerShotToken)
    {
        if (target == null
            || owner == null
            || !target.isActiveAndEnabled
            || target.IsDestroyed
            || !target.HasDamageReservation(this))
        {
            Debug.LogWarning(
                "[Bullet] Invalid target, owner or damage reservation.",
                this);

            return false;
        }

        // Reset trạng thái trước khi tái sử dụng từ pool.
        _isActive = false;
        _target = null;
        _owner = null;

        _target = target;
        _owner = owner;
        _ownerShotToken = ownerShotToken;
        moveSpeed = Mathf.Max(0.1f, speed);

        transform.position = startPosition;
        ApplyColor(bulletColor);

        _isActive = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        return true;
    }

    private void Update()
    {
        if (!_isActive)
            return;

        // Target không còn hợp lệ: không gây sát thương,
        // giải phóng reservation và thông báo cho shooter.
        if (_target == null
            || !_target.isActiveAndEnabled
            || _target.IsDestroyed)
        {
            Complete(false);
            return;
        }

        Vector3 targetPosition = _target.transform.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude > 0.0001f)
            transform.forward = direction.normalized;

        if (Vector3.Distance(transform.position, targetPosition) <= hitDistance)
            Complete(true);
    }

    private void Complete(bool tryHit)
    {
        if (!_isActive)
            return;

        // Đánh dấu hoàn tất ngay đầu hàm để chống callback/impact trùng.
        _isActive = false;

        PixelBlock target = _target;
        ShooterUnit owner = _owner;
        int shotToken = _ownerShotToken;

        if (target != null)
        {
            if (tryHit
                && target.isActiveAndEnabled
                && !target.IsDestroyed)
            {
                // Chỉ reservation của projectile này mới được áp dụng.
                target.TryApplyReservedDamage(this);
            }

            // Nếu hit thành công, reservation đã được tiêu thụ.
            // Nếu miss/cancel, thao tác này giải phóng reservation.
            target.ReleaseDamageReservation(this);
        }

        // Xóa tham chiếu trước khi trả về pool.
        _target = null;
        _owner = null;

        if (owner != null)
            owner.NotifyProjectileFinished(shotToken);

        ReleaseToPool();
    }

    private void ReleaseToPool()
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.Release(this);
        else
            Destroy(gameObject);
    }

    private void ApplyColor(Color color)
    {
        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        if (colorRenderers == null || colorRenderers.Length == 0)
            colorRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer targetRenderer in colorRenderers)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(LegacyColorId, color);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private void OnDisable()
    {
        // Nếu bị PoolManager hoặc hệ thống khác tắt trực tiếp trong lúc
        // đang bay, vẫn phải giải phóng reservation và mở khóa shooter.
        if (!_isActive)
            return;

        PixelBlock target = _target;
        ShooterUnit owner = _owner;
        int shotToken = _ownerShotToken;

        _isActive = false;
        _target = null;
        _owner = null;

        if (target != null)
            target.ReleaseDamageReservation(this);

        if (owner != null)
            owner.NotifyProjectileFinished(shotToken);
    }
}