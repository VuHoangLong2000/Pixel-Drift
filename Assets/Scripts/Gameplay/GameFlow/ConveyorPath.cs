using UnityEngine;

[ExecuteAlways]
public class ConveyorPath : MonoBehaviour
{
    [Header("Board Reference")]
    [SerializeField] private PixelBlockManager blockManager;

    [Header("Path Settings")]
    [SerializeField, Min(0f)]
    private float pathMargin = 2f;

    [SerializeField, Min(0.01f)]
    private float cornerCut = 0.5f;

    [SerializeField]
    private float pathHeight = 0.1f;

    [SerializeField]
    private bool showPath = true;

    [Header("Gizmos")]
    [SerializeField, Min(0.01f)]
    private float gizmoPointRadius = 0.12f;

    private Vector3[] _points;
    private float[] _cumulativeDistances;
    private float _pathLength;
    private bool _isValid;

    public float PathLength => _pathLength;
    public bool IsValid => _isValid;
    public int PointCount => _points == null ? 0 : _points.Length;

    private void OnEnable()
    {
        SubscribeToLevelEvents();
        RefreshPath();
    }

    private void Start()
    {
        // Phòng trường hợp Level được load trước khi component khởi tạo.
        RefreshPath();
    }

    private void OnDisable()
    {
        UnsubscribeFromLevelEvents();
    }

    private void OnValidate()
    {
        pathMargin = Mathf.Max(0f, pathMargin);
        cornerCut = Mathf.Max(0.01f, cornerCut);
        gizmoPointRadius = Mathf.Max(0.01f, gizmoPointRadius);

        RefreshPath();
    }

    private void SubscribeToLevelEvents()
    {
        if (blockManager != null)
        {
            blockManager.OnLevelLoaded -= HandleLevelLoaded;
            blockManager.OnLevelLoaded += HandleLevelLoaded;
        }
    }

    private void UnsubscribeFromLevelEvents()
    {
        if (blockManager != null)
            blockManager.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void HandleLevelLoaded()
    {
        RefreshPath();
    }

    /// <summary>
    /// Sinh đường vòng quanh board theo dữ liệu Level hiện tại.
    /// Gọi lại sau khi đổi Level hoặc thay đổi layout.
    /// </summary>
    public void RefreshPath()
    {
        _isValid = false;
        _pathLength = 0f;
        _points = null;
        _cumulativeDistances = null;

        if (blockManager == null)
            return;

        LevelData level = blockManager.CurrentLevelData;

        if (level == null || level.width < 1 || level.height < 1)
        {
            return;
        }

        float spacing = Mathf.Max(0.1f, level.cellSpacing);

        float halfWidth = level.width * spacing * 0.5f + pathMargin;

        float halfDepth = level.height * spacing * 0.5f + pathMargin;

        float cut = Mathf.Min(cornerCut, Mathf.Min(halfWidth, halfDepth) * 0.5f);

        Vector3 center = blockManager.BoardOrigin;
        center.y += pathHeight;

        // Thứ tự đi vòng quanh board trong mặt phẳng XZ.
        // Các góc được vát để giảm đoạn rẽ gấp.
        _points = new Vector3[]
        {
            center + new Vector3(-halfWidth + cut, 0f, -halfDepth),
            center + new Vector3( halfWidth - cut, 0f, -halfDepth),

            center + new Vector3( halfWidth, 0f, -halfDepth + cut),
            center + new Vector3( halfWidth, 0f,  halfDepth - cut),

            center + new Vector3( halfWidth - cut, 0f,  halfDepth),
            center + new Vector3(-halfWidth + cut, 0f,  halfDepth),

            center + new Vector3(-halfWidth, 0f,  halfDepth - cut),
            center + new Vector3(-halfWidth, 0f, -halfDepth + cut)
        };

        _cumulativeDistances = new float[_points.Length + 1];
        _cumulativeDistances[0] = 0f;

        for (int i = 0; i < _points.Length; i++)
        {
            Vector3 start = _points[i];
            Vector3 end = _points[(i + 1) % _points.Length];

            float segmentLength = Vector3.Distance(start, end);

            if (segmentLength <= 0.0001f)
            {
                _points = null;
                _cumulativeDistances = null;
                return;
            }

            _pathLength += segmentLength;
            _cumulativeDistances[i + 1] = _pathLength;
        }

        _isValid = _pathLength > 0f;
    }

    /// <summary>
    /// Lấy vị trí trên đường vòng theo khoảng cách từ điểm bắt đầu.
    /// Khoảng cách vượt quá chiều dài đường sẽ quay về đầu vòng.
    /// </summary>
    public Vector3 GetPositionAtDistance(float distance)
    {
        if (!_isValid)
            RefreshPath();

        if (!_isValid)
            return transform.position;

        distance = Mathf.Repeat(distance, _pathLength);

        int segmentIndex = FindSegmentIndex(distance);

        Vector3 start = _points[segmentIndex];
        Vector3 end = _points[(segmentIndex + 1) % _points.Length];

        float segmentStartDistance = _cumulativeDistances[segmentIndex];

        float segmentLength = _cumulativeDistances[segmentIndex + 1] - segmentStartDistance;

        float t = (distance - segmentStartDistance) / segmentLength;

        return Vector3.Lerp(start, end, t);
    }

    public Vector3 GetDirectionAtDistance(float distance)
    {
        if (!_isValid)
            RefreshPath();

        if (!_isValid)
            return transform.forward;

        distance = Mathf.Repeat(distance, _pathLength);

        int segmentIndex = FindSegmentIndex(distance);

        Vector3 direction = _points[(segmentIndex + 1) % _points.Length] - _points[segmentIndex];

        return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
    }

    public Quaternion GetRotationAtDistance(float distance)
    {
        Vector3 direction = GetDirectionAtDistance(distance);
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return transform.rotation;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private int FindSegmentIndex(float distance)
    {
        for (int i = 0; i < _points.Length; i++)
        {
            if (distance < _cumulativeDistances[i + 1])
                return i;
        }

        return _points.Length - 1;
    }

    private void OnDrawGizmos()
    {
        if (!showPath)
            return;

        if (!_isValid)
            RefreshPath();

        if (!_isValid || _points == null)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < _points.Length; i++)
        {
            Vector3 start = _points[i];
            Vector3 end = _points[(i + 1) % _points.Length];

            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(start, gizmoPointRadius);
        }
    }
}
