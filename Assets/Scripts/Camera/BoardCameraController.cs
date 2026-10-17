using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BoardCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PixelBlockManager blockManager;

    [Header("Camera Setup")]
    [SerializeField, Min(1f)] private float padding = 1.2f;
    [SerializeField, Min(1f)] private float heightMultiplier = 1.2f;
    [SerializeField, Min(1f)] private float minHeight = 5f;

    [Header("View Angle")]
    [SerializeField, Range(0f, 80f)]
    private float angle = 45f;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        SubscribeToLevelLoaded();
    }

    private void Start()
    {
        // Handle the case where the level loaded before subscription.
        if (blockManager != null &&
            blockManager.CurrentLevelData != null)
        {
            FitToBoard();
        }
    }

    private void OnDisable()
    {
        if (blockManager != null)
            blockManager.OnLevelLoaded -= FitToBoard;
    }

    private void SubscribeToLevelLoaded()
    {
        if (blockManager == null)
            return;

        blockManager.OnLevelLoaded -= FitToBoard;
        blockManager.OnLevelLoaded += FitToBoard;
    }

    public void FitToBoard()
    {
        if (blockManager == null)
        {
            Debug.LogWarning("[BoardCameraController] Missing PixelBlockManager.", this);
            return;
        }

        var blocks = blockManager.Blocks;

        if (blocks == null || blocks.Count == 0)
        {
            Debug.LogWarning("[BoardCameraController] No blocks to frame.", this);
            return;
        }

        bool hasBounds = false;
        Bounds bounds = new Bounds();

        foreach (PixelBlock block in blocks)
        {
            if (block == null)
                continue;

            if (!hasBounds)
            {
                bounds = new Bounds(block.transform.position, block.transform.lossyScale);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(block.transform.position);
            }
        }

        if (!hasBounds)
            return;

        float halfBlock = 0.5f;
        bounds.Expand(halfBlock * 2f);

        Vector3 center = bounds.center;

        float boardWidth = bounds.size.x;
        float boardDepth = bounds.size.z;

        float verticalFov = _camera.fieldOfView * Mathf.Deg2Rad;
        float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * _camera.aspect);

        float distanceForWidth = (boardWidth * 0.5f) / Mathf.Tan(horizontalFov * 0.5f);

        float distanceForDepth = (boardDepth * 0.5f) / Mathf.Tan(verticalFov * 0.5f);

        float distance = Mathf.Max(distanceForWidth, distanceForDepth);

        distance = Mathf.Max(distance * padding, minHeight);
        distance *= heightMultiplier;

        float radians = angle * Mathf.Deg2Rad;

        Vector3 viewDirection = new Vector3(0f, -Mathf.Sin(radians), -Mathf.Cos(radians));

        transform.position = center - viewDirection * distance;
        transform.LookAt(center);
    }
}
