using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class ConveyorDropZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ConveyorPath conveyorPath;
    [SerializeField] private PixelBlockManager blockManager;

    [Header("Zone Size")]
    [SerializeField, Min(0.1f)]
    private float zoneWidth = 2f;

    [SerializeField, Min(0.1f)]
    private float zoneDepth = 2f;

    [SerializeField, Min(0.1f)]
    private float zoneHeight = 2f;

    [SerializeField]
    private float centerOffsetForward = 0f;

    private BoxCollider _collider;

    private void OnEnable()
    {
        CacheReferences();
        SubscribeEvents();
        RefreshZone();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        CacheReferences();
        RefreshZone();
    }

    private void CacheReferences()
    {
        if (_collider == null)
            _collider = GetComponent<BoxCollider>();

        if (conveyorPath == null)
            conveyorPath = FindFirstObjectByType<ConveyorPath>();

        if (blockManager == null)
            blockManager = FindFirstObjectByType<PixelBlockManager>();

        if (_collider != null)
            _collider.isTrigger = true;
    }

    private void SubscribeEvents()
    {
        if (blockManager == null)
            return;

        blockManager.OnLevelLoaded -= HandleLevelLoaded;
        blockManager.OnLevelLoaded += HandleLevelLoaded;
    }

    private void UnsubscribeEvents()
    {
        if (blockManager != null)
            blockManager.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void HandleLevelLoaded()
    {
        RefreshZone();
    }

    public void RefreshZone()
    {
        CacheReferences();

        if (_collider == null || conveyorPath == null)
            return;

        conveyorPath.RefreshPath();

        if (!conveyorPath.IsValid)
        {
            _collider.enabled = false;
            return;
        }

        _collider.enabled = true;

        float spacing = 1f;

        if (blockManager != null &&
            blockManager.CurrentLevelData != null)
        {
            spacing = Mathf.Max(
                0.1f,
                blockManager.CurrentLevelData.cellSpacing);
        }

        // Lấy hướng di chuyển tại điểm đầu.
        Vector3 start = conveyorPath.StartPosition;
        Vector3 direction =
            conveyorPath.GetDirectionAtDistance(0f);

        // Đặt vùng ở phía ngoài điểm đầu một chút,
        // để người chơi thả Shooter vào vùng này.
        Vector3 center = start - direction * centerOffsetForward;

        transform.position = center;
        transform.rotation = conveyorPath.GetRotationAtDistance(0f);

        _collider.size = new Vector3(Mathf.Max(zoneWidth, spacing), zoneHeight, Mathf.Max(zoneDepth, spacing));
    }
}
