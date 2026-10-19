using UnityEngine;
using DG.Tweening;

public class ShooterUnit : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2f;

    [Header("Shooting")]
    [SerializeField, Min(0.05f)] private float fireInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float bulletSpeed = 10f;
    [SerializeField] private BulletProjectile bulletPrefab;
    [SerializeField] private Transform muzzleTransform;

    [Header("Color Palette")]
    [SerializeField] private ColorPalette colorPalette;

    [Header("Visual")]
    [SerializeField] private Renderer[] colorRenderers;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _propertyBlock;

    private ConveyorPath _path;
    private PixelBlockManager _blockManager;
    private ShooterConveyorSpawner _spawner;

    private bool _isInitialized;
    private bool _isFiring;
    private float _shotTimer;
    private int _shotToken;

    private bool _hasCompletedLap;
    private bool _isMoving;

    [Header("Out Of Ammo Effect")]
    [SerializeField, Min(0.05f)]
    private float ejectDuration = 0.55f;

    [SerializeField, Min(0f)]
    private float ejectDistance = 2.5f;

    [SerializeField, Min(0f)]
    private float ejectHeight = 0.6f;

    [SerializeField, Min(1)]
    private int spinRotations = 3;

    [SerializeField]
    private Ease ejectEase = Ease.OutCubic;

    private bool _isEjecting;
    private Vector3 _lastShootDirection;
    private Vector3 _initialScale;

    public string ColorId { get; private set; }
    public int Ammo { get; private set; }
    public float DistanceAlongPath { get; private set; }

    public void Initialize(ConveyorPath path, PixelBlockManager blockManager, ShooterConveyorSpawner spawner, string colorId, int ammo, float startDistance, float speed)
    {
        transform.DOKill();

        _isEjecting = false;
        transform.localScale = _initialScale;

        // Tăng token để callback từ lượt sử dụng cũ bị bỏ qua.
        _shotToken++;
        _isInitialized = false;
        _isFiring = false;

        _path = path;
        _blockManager = blockManager;
        _spawner = spawner;
        ColorId = colorId;
        Ammo = Mathf.Max(0, ammo);
        DistanceAlongPath = Mathf.Max(0f, startDistance);
        moveSpeed = Mathf.Max(0f, speed);
        _shotTimer = 0f;

        _hasCompletedLap = false;
        _isMoving = false;

        if (_path == null || _blockManager == null || _spawner == null)
        {
            Debug.LogError("[ShooterUnit] Missing path, manager or spawner.", this);
            return;
        }

        if (bulletPrefab == null)
        {
            Debug.LogError("[ShooterUnit] Bullet prefab is missing.", this);
            return;
        }

        if (colorPalette == null)
        {
            Debug.LogError("[ShooterUnit] ColorPalette is not assigned.", this);
            return;
        }

        if (!colorPalette.TryGetColor(colorId, out Color color))
        {
            Debug.LogError($"[ShooterUnit] Unknown color ID '{colorId}'.", this);
            return;
        }

        ApplyColor(color);
        UpdatePosition();
        _isInitialized = true;
    }
    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void Update()
    {
        if (_isEjecting || !_isInitialized || _path == null || !_path.IsValid)
            return;

        if (!_isInitialized || _path == null || !_path.IsValid)
            return;

        // Chỉ di chuyển sau khi được thả lên băng chuyền.
        if (!_hasCompletedLap && _isMoving)
        {
            DistanceAlongPath += moveSpeed * Time.deltaTime;

            if (DistanceAlongPath >= _path.PathLength)
            {
                DistanceAlongPath = _path.PathLength;
                UpdatePosition();
                CompleteLap();
            }
            else
            {
                UpdatePosition();
            }
        }

        // Không bắn khi còn ở khay hoặc đã hoàn thành vòng.
        if (!_isMoving || _hasCompletedLap || _isFiring || Ammo <= 0)
            return;

        _shotTimer -= Time.deltaTime;

        if (_shotTimer <= 0f)
        {
            _shotTimer = fireInterval;
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (!_isInitialized || _blockManager == null || Ammo <= 0)
            return;

        if (PoolManager.Instance == null)
        {
            Debug.LogError("[ShooterUnit] PoolManager.Instance is missing.", this);
            return;
        }

        if (colorPalette == null ||
            !colorPalette.TryGetColor(ColorId, out Color bulletColor))
        {
            return;
        }

        LevelData level = _blockManager.CurrentLevelData;

        if (level == null)
            return;

        Vector3 shootDirection = GetInwardShootDirection();
        _lastShootDirection = shootDirection;

        float maxDistance =
            Vector3.Distance(transform.position, _blockManager.BoardOrigin)
            + Mathf.Sqrt(
                level.width * level.width +
                level.height * level.height)
            * _blockManager.CellSpacing;

        float alignmentTolerance =
            Mathf.Max(0.05f, _blockManager.CellSpacing * 0.45f);

        PixelBlock target = _blockManager.FindFirstBlock(
            transform.position,
            shootDirection,
            maxDistance,
            alignmentTolerance);

        // Block đầu tiên chắn đường vẫn quyết định việc có bắn được hay không.
        if (target == null || target.ColorId != ColorId)
            return;

        Vector3 muzzlePosition = muzzleTransform != null
            ? muzzleTransform.position
            : transform.position;

        BulletProjectile bullet = PoolManager.Instance.Get(bulletPrefab);

        if (bullet == null)
        {
            Debug.LogError("[ShooterUnit] Failed to get bullet from pool.", this);
            return;
        }

        // BƯỚC 1: Đặt trước sát thương trước khi phóng viên đạn.
        if (!target.TryReserveDamage(bullet, 1))
        {
            // HP khả dụng không đủ. Không tiêu hao đạn.
            PoolManager.Instance.Release(bullet);
            return;
        }

        int currentShotToken = _shotToken;

        // BƯỚC 2: Chỉ khởi chạy projectile nếu reservation đã tồn tại.
        bool initialized = bullet.Initialize(
            muzzlePosition,
            target,
            bulletColor,
            bulletSpeed,
            this,
            currentShotToken);

        if (!initialized)
        {
            target.ReleaseDamageReservation(bullet);
            PoolManager.Instance.Release(bullet);
            return;
        }

        // BƯỚC 3: Chỉ tiêu hao đạn sau khi khởi tạo thành công.
        _isFiring = true;
        Ammo--;

        Debug.Log(
            $"[ShooterUnit] {ColorId} fired. " +
            $"Ammo remaining: {Ammo}. " +
            $"Target HP: {target.HitPoints}, " +
            $"Reserved: {target.ReservedDamage}.",
            this);
    }


    public void NotifyProjectileFinished(int shotToken)
    {
        // Bỏ qua callback từ lượt sử dụng cũ.
        if (!_isInitialized || shotToken != _shotToken)
            return;

        _isFiring = false;

        // Hết đạn: dừng conveyor và bắt đầu hiệu ứng văng.
        if (Ammo <= 0)
        {
            PlayOutOfAmmoEffect();
            return;
        }

        // Vẫn giữ nguyên logic khi chạy hết vòng.
        if (_hasCompletedLap)
            HandleAfterMovementCompleted();
    }

    private void PlayOutOfAmmoEffect()
    {
        if (!_isInitialized || _isEjecting)
            return;

        _isEjecting = true;
        _isMoving = false;
        _hasCompletedLap = true;
        _isFiring = false;

        // Hướng văng ngược với hướng viên đạn cuối.
        Vector3 backward = -_lastShootDirection;
        backward.y = 0f;

        if (backward.sqrMagnitude < 0.001f)
            backward = -transform.forward;

        backward.Normalize();

        Vector3 startPosition = transform.position;

        Vector3 endPosition =
            startPosition
            + backward * ejectDistance
            + Vector3.up * ejectHeight;

        // Hướng xoay quanh trục đứng của game.
        Vector3 endRotation =
            transform.eulerAngles
            + Vector3.up * (360f * spinRotations);

        transform.DOKill();

        Sequence sequence = DOTween.Sequence();

        sequence.Join(transform.DOJump(endPosition, 1.5f, 1, ejectDuration).SetEase(Ease.OutQuad));

        sequence.Join(
            transform.DORotate(
                endRotation,
                ejectDuration,
                RotateMode.FastBeyond360
            ).SetEase(Ease.OutQuad)
        );

        sequence.Join(
            transform.DOScale(
                Vector3.zero,
                ejectDuration
            ).SetEase(Ease.InBack)
        );

        sequence.OnComplete(() =>
        {
            if (this == null || !_isInitialized)
                return;

            Finish();
        });
    }


    private Vector3 GetInwardShootDirection()
    {
        Vector3 offset = transform.position - _blockManager.BoardOrigin;
        LevelData level = _blockManager.CurrentLevelData;

        float halfWidth = Mathf.Max(
            0.5f,
            (level.width - 1) * _blockManager.CellSpacing * 0.5f
            + _blockManager.CellSize * 0.5f
        );

        float halfDepth = Mathf.Max(
            0.5f,
            (level.height - 1) * _blockManager.CellSpacing * 0.5f
            + _blockManager.CellSize * 0.5f
        );

        float normalizedX = Mathf.Abs(offset.x) / halfWidth;
        float normalizedZ = Mathf.Abs(offset.z) / halfDepth;

        if (normalizedX > normalizedZ)
            return offset.x > 0f ? Vector3.left : Vector3.right;

        return offset.z > 0f ? Vector3.back : Vector3.forward;
    }

    private void Finish()
    {
        if (!_isInitialized)
            return;

        _isInitialized = false;
        _isFiring = false;
        _isEjecting = false;
        _shotToken++;

        if (_spawner != null)
            _spawner.NotifyShooterFinished(this);
    }

    private void UpdatePosition()
    {
        if (_path == null || !_path.IsValid)
            return;

        transform.position = _path.GetPositionAtDistance(DistanceAlongPath);
        transform.rotation = _path.GetRotationAtDistance(DistanceAlongPath);
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

    public void SetAmmo(int amount)
    {
        Ammo = Mathf.Max(0, amount);
    }

    private void OnDisable()
    {
        _isInitialized = false;
        _isFiring = false;
        _isEjecting = false;
        _shotToken++;
    }
    public void PlaceAtConveyorStart()
    {
        if (!_isInitialized || _path == null || !_path.IsValid)
            return;

        DistanceAlongPath = 0f;
        UpdatePosition();
    }

    public void StartConveyorMovement()
    {
        if (!_isInitialized || _hasCompletedLap || _isMoving)
            return;

        _isMoving = true;
    }

    public void SetTrayPosition(Vector3 position)
    {
        if (!_isInitialized || _isMoving)
            return;

        transform.position = position;
        transform.rotation = Quaternion.identity;
    }

    private void CompleteLap()
    {
        if (_hasCompletedLap)
            return;

        _hasCompletedLap = true;
        _isMoving = false;

        if (!_isFiring)
            HandleAfterMovementCompleted();
    }

    private void HandleAfterMovementCompleted()
    {
        if (!_isInitialized || !_hasCompletedLap || _isFiring)
            return;

        if (Ammo <= 0)
        {
            Finish();
            return;
        }

        if (_spawner != null)
            _spawner.NotifyShooterCompletedLap(this);
    }
    public bool PrepareForNextLap()
    {
        if (!_isInitialized || _isEjecting || _isFiring || Ammo <= 0)
        {
            return false;
        }

        _hasCompletedLap = false;
        _isMoving = false;

        DistanceAlongPath = 0f;
        _shotTimer = 0f;

        return true;
    }
}
