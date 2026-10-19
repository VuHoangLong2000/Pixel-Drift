using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ShooterConveyorSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PixelBlockManager blockManager;
    [SerializeField] private ConveyorPath conveyorPath;
    [SerializeField] private ShooterUnit shooterPrefab;
    [SerializeField] private ColorPalette colorPalette;

    [Header("Tray Slots - World Space")]
    [SerializeField] private Transform[] traySlots;

    [Header("Waiting Slots - World Space")]
    [SerializeField] private Transform[] waitingSlots;

    [Header("Movement")]
    [SerializeField, Min(0.01f)]
    private float moveSpeed = 2f;

    [SerializeField, Min(0.01f)]
    private float slotTweenDuration = 0.2f;

    [Header("Hierarchy")]
    [SerializeField] private Transform shooterRoot;

    private readonly Queue<ShooterSpawnData> _spawnQueue = new();
    private readonly List<ShooterUnit> _trayShooters = new();
    private readonly List<ShooterUnit> _activeShooters = new();
    private readonly List<ShooterUnit> _waitingShooters = new();

    private bool _isReady;

    public int QueuedCount => _spawnQueue.Count;
    public int TrayCount => _trayShooters.Count;
    public int ActiveCount => _activeShooters.Count;
    public int WaitingCount => _waitingShooters.Count;

    private const int MaxConveyorShooters = 5;

    private void OnEnable()
    {
        SubscribeToLevelEvents();
    }

    private IEnumerator Start()
    {
        if (blockManager == null)
        {
            Debug.LogError("[ShooterSpawner] PixelBlockManager is missing.", this);
            yield break;
        }

        while (blockManager != null &&
               blockManager.CurrentLevelData == null)
        {
            yield return null;
        }

        if (isActiveAndEnabled)
            RebuildForCurrentLevel();
    }

    private void OnDisable()
    {
        UnsubscribeFromLevelEvents();
        ClearShooters();
    }

    private void SubscribeToLevelEvents()
    {
        if (blockManager == null)
            return;

        blockManager.OnLevelLoaded -= HandleLevelLoaded;
        blockManager.OnLevelLoaded += HandleLevelLoaded;
    }

    private void UnsubscribeFromLevelEvents()
    {
        if (blockManager != null)
            blockManager.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void HandleLevelLoaded()
    {
        RebuildForCurrentLevel();
    }

    public void RebuildForCurrentLevel()
    {
        ClearShooters();

        if (blockManager == null || conveyorPath == null || shooterPrefab == null || colorPalette == null)
        {
            Debug.LogError("[ShooterSpawner] Missing required references.", this);
            return;
        }

        if (traySlots == null || traySlots.Length == 0)
        {
            Debug.LogError("[ShooterSpawner] Assign at least one Tray Slot.", this);
            return;
        }

        LevelData level = blockManager.CurrentLevelData;

        if (level == null)
        {
            Debug.LogWarning("[ShooterSpawner] No Level is loaded.", this);
            return;
        }

        conveyorPath.RefreshPath();

        if (!conveyorPath.IsValid || conveyorPath.PathLength <= 0f)
        {
            Debug.LogError("[ShooterSpawner] ConveyorPath is invalid.", this);
            return;
        }

        BuildSpawnQueue(level);

        _isReady = true;
        RefillTray();

        Debug.Log(
            $"[ShooterSpawner] Level " +
            $"{blockManager.CurrentLevelIndex + 1} initialized. " +
            $"Tray={TrayCount}, Queued={QueuedCount}",
            this);
    }

    private void BuildSpawnQueue(LevelData level)
    {
        _spawnQueue.Clear();

        if (level.shooters == null)
            return;

        List<ShooterSpawnData> groups = new();

        foreach (ShooterSpawnData data in level.shooters)
        {
            if (data == null || data.quantity <= 0 || data.ammo <= 0 || string.IsNullOrWhiteSpace(data.colorId))
            {
                continue;
            }

            if (!colorPalette.TryGetColor(data.colorId, out _))
            {
                Debug.LogWarning(
                    $"[ShooterSpawner] Unknown color ID " +
                    $"'{data.colorId}'. This group is skipped.", this);
                continue;
            }

            groups.Add(data);
        }

        // Sort ổn định: các group cùng spawnOrder giữ thứ tự dữ liệu gốc.
        groups.Sort((a, b) => a.spawnOrder.CompareTo(b.spawnOrder));

        foreach (ShooterSpawnData group in groups)
        {
            for (int i = 0; i < group.quantity; i++)
            {
                _spawnQueue.Enqueue(new ShooterSpawnData
                {
                    colorId = group.colorId,
                    ammo = group.ammo,
                    quantity = 1,
                    spawnOrder = group.spawnOrder
                });
            }
        }
    }

    private ShooterUnit GetShooterFromPool()
    {
        if (PoolManager.Instance == null)
        {
            Debug.LogError("[ShooterSpawner] PoolManager.Instance is missing.", this);
            return null;
        }

        ShooterUnit shooter = PoolManager.Instance.Get(shooterPrefab);

        if (shooter != null && shooterRoot != null)
            shooter.transform.SetParent(shooterRoot, true);

        return shooter;
    }

    private void ReleaseShooter(ShooterUnit shooter)
    {
        if (shooter == null)
            return;

        // Tránh tween cũ tiếp tục tác động lên object đã được pool.
        shooter.transform.DOKill();

        if (PoolManager.Instance != null)
            PoolManager.Instance.Release(shooter);
        else
            Destroy(shooter.gameObject);
    }

    private void RefillTray()
    {
        if (!_isReady)
            return;

        while (_trayShooters.Count < traySlots.Length &&
               _spawnQueue.Count > 0)
        {
            int slotIndex = _trayShooters.Count;
            ShooterSpawnData data = _spawnQueue.Dequeue();

            ShooterUnit shooter = GetShooterFromPool();

            if (shooter == null)
            {
                _spawnQueue.Enqueue(data);
                return;
            }

            // Khởi tạo Shooter với dữ liệu của level.
            shooter.Initialize(conveyorPath, blockManager, this, data.colorId, data.ammo, 0f, moveSpeed);

            // Truyền reference cho DragHandler sau khi lấy Shooter từ Pool.
            ShooterDragHandler dragHandler = shooter.GetComponentInChildren<ShooterDragHandler>(true);

            if (dragHandler != null)
            {
                dragHandler.Initialize(shooter, this);
            }
            else
            {
                Debug.LogWarning($"[ShooterSpawner] Shooter '{shooter.name}' " + "is missing ShooterDragHandler.", shooter);
            }

            shooter.SetTrayPosition(traySlots[slotIndex].position);
            shooter.transform.rotation = traySlots[slotIndex].rotation;

            _trayShooters.Add(shooter);

        }
    }

    /// <summary>
    /// Được gọi bởi drag handler khi người chơi thả
    /// Shooter hợp lệ vào ConveyorDropZone.
    /// </summary>

    public bool TryLaunchShooter(ShooterUnit shooter)
    {
        if (!_isReady || shooter == null)
            return false;

        // Giới hạn tối đa 5 Shooter đang chạy conveyor.
        if (_activeShooters.Count >= MaxConveyorShooters)
            return false;

        if (conveyorPath == null || !conveyorPath.IsValid)
            return false;

        bool wasInTray = _trayShooters.Contains(shooter);
        bool wasWaiting = _waitingShooters.Contains(shooter);

        // Chỉ cho phép lấy Shooter từ Tray hoặc hàng chờ.
        if (!wasInTray && !wasWaiting)
            return false;

        if (shooter.Ammo <= 0)
            return false;

        // Nếu lấy từ hàng chờ, phải chuẩn bị lại trạng thái vòng chạy.
        if (wasWaiting && !shooter.PrepareForNextLap())
            return false;

        shooter.transform.DOKill();

        if (wasInTray)
        {
            _trayShooters.Remove(shooter);
            RepositionTrayShooters();
        }

        if (wasWaiting)
        {
            _waitingShooters.Remove(shooter);
            RepositionWaitingShooters();
        }

        _activeShooters.Add(shooter);

        shooter.PlaceAtConveyorStart();
        shooter.StartConveyorMovement();

        // Chỉ lấp Tray nếu Shooter được lấy từ Tray.
        if (wasInTray)
            RefillTray();

        Debug.Log(
            $"[ShooterSpawner] Launched {shooter.ColorId}. " +
            $"Tray={TrayCount}, Active={ActiveCount}, " +
            $"Waiting={WaitingCount}, Queued={QueuedCount}",
            this);

        return true;
    }


    /// <summary>
    /// Shooter đã hoàn thành một vòng.
    /// Còn đạn thì chuyển sang hàng chờ; hết đạn thì thu hồi
    /// qua NotifyShooterFinished().
    /// </summary>
    public void NotifyShooterCompletedLap(ShooterUnit shooter)
    {
        if (shooter == null)
            return;

        if (!_activeShooters.Remove(shooter))
            return;

        if (shooter.Ammo <= 0)
        {
            // Phòng trường hợp callback được gọi không đúng thứ tự.
            NotifyShooterFinished(shooter);
            return;
        }

        _waitingShooters.Add(shooter);

        MoveShooterToWaitingSlot(shooter);
        RefillTray();

        Debug.Log(
            $"[ShooterSpawner] {shooter.ColorId} entered waiting queue. " +
            $"Ammo={shooter.Ammo}, Waiting={WaitingCount}",
            this);
    }

    private void MoveShooterToWaitingSlot(ShooterUnit shooter)
    {
        int index = _waitingShooters.IndexOf(shooter);

        if (index < 0)
            return;

        if (waitingSlots == null || index >= waitingSlots.Length || waitingSlots[index] == null)
        {
            Debug.LogWarning(
                "[ShooterSpawner] Waiting slot is missing or full. " +
                "Assign enough Waiting Slots in the Inspector.",
                this);
            return;
        }

        Transform slot = waitingSlots[index];

        shooter.transform.DOKill();

        shooter.transform
            .DOMove(slot.position, slotTweenDuration)
            .SetEase(Ease.OutQuad);

        shooter.transform
            .DORotateQuaternion(slot.rotation, slotTweenDuration)
            .SetEase(Ease.OutQuad);
    }

    private void RepositionTrayShooters()
    {
        for (int i = 0; i < _trayShooters.Count; i++)
        {
            ShooterUnit shooter = _trayShooters[i];

            if (shooter == null || traySlots[i] == null)
                continue;

            shooter.transform.DOKill();

            Transform slot = traySlots[i];

            shooter.transform
                .DOMove(slot.position, slotTweenDuration)
                .SetEase(Ease.OutQuad);

            shooter.transform
                .DORotateQuaternion(slot.rotation, slotTweenDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    private void RepositionWaitingShooters()
    {
        for (int i = 0; i < _waitingShooters.Count; i++)
        {
            ShooterUnit shooter = _waitingShooters[i];

            if (shooter == null || waitingSlots == null || i >= waitingSlots.Length || waitingSlots[i] == null)
            {
                continue;
            }

            Transform slot = waitingSlots[i];

            shooter.transform.DOKill();

            shooter.transform
                .DOMove(slot.position, slotTweenDuration)
                .SetEase(Ease.OutQuad);

            shooter.transform
                .DORotateQuaternion(slot.rotation, slotTweenDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Shooter hết đạn sau khi đã hoàn thành vòng,
    /// hoặc được yêu cầu thu hồi.
    /// </summary>
    public void NotifyShooterFinished(ShooterUnit shooter)
    {
        if (shooter == null)
            return;

        bool wasInTray = _trayShooters.Remove(shooter);
        bool wasActive = _activeShooters.Remove(shooter);
        bool wasWaiting = _waitingShooters.Remove(shooter);

        if (!wasInTray && !wasActive && !wasWaiting)
            return;

        ReleaseShooter(shooter);

        if (wasInTray)
            RepositionTrayShooters();

        if (wasWaiting)
            RepositionWaitingShooters();

        RefillTray();

        Debug.Log(
            $"[ShooterSpawner] Shooter released. " +
            $"Tray={TrayCount}, Active={ActiveCount}, " +
            $"Waiting={WaitingCount}, Queued={QueuedCount}",
            this);
    }

    private void ClearShooters()
    {
        _isReady = false;

        HashSet<ShooterUnit> allShooters = new();

        foreach (ShooterUnit shooter in _trayShooters)
        {
            if (shooter != null)
                allShooters.Add(shooter);
        }

        foreach (ShooterUnit shooter in _activeShooters)
        {
            if (shooter != null)
                allShooters.Add(shooter);
        }

        foreach (ShooterUnit shooter in _waitingShooters)
        {
            if (shooter != null)
                allShooters.Add(shooter);
        }

        _trayShooters.Clear();
        _activeShooters.Clear();
        _waitingShooters.Clear();
        _spawnQueue.Clear();

        foreach (ShooterUnit shooter in allShooters)
            ReleaseShooter(shooter);
    }
    public bool CanDragShooter(ShooterUnit shooter)
    {
        return _isReady && shooter != null && (_trayShooters.Contains(shooter) || _waitingShooters.Contains(shooter));
    }
}
