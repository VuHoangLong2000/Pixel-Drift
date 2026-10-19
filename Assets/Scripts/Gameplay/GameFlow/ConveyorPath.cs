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

    public Vector3 StartPosition => IsValid ? _points[0] : transform.position;

    private void OnEnable()
    {
        SubscribeToLevelEvents();
        RefreshPath();
    }

    private void Start()
    {
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
        if (!IsValid)
            return transform.position;

        // Không wrap về đầu đường khi đi hết một vòng.
        distance = Mathf.Clamp(distance, 0f, _pathLength);

        // Trả chính xác về điểm đầu/cuối tại hai biên.
        if (distance <= 0f)
            return _points[0];

        if (distance >= _pathLength)
            return _points[0];

        int segmentIndex = FindSegmentIndex(distance);

        float segmentStart = _cumulativeDistances[segmentIndex];
        float segmentEnd = _cumulativeDistances[segmentIndex + 1];

        float segmentLength = segmentEnd - segmentStart;
        float t = segmentLength > Mathf.Epsilon ? (distance - segmentStart) / segmentLength : 0f;

        Vector3 start = _points[segmentIndex];
        Vector3 end = _points[(segmentIndex + 1) % _points.Length];

        return Vector3.Lerp(start, end, t);
    }

    public Vector3 GetDirectionAtDistance(float distance)
    {
        if (!IsValid)
            return Vector3.forward;

        distance = Mathf.Clamp(distance, 0f, _pathLength);

        // Cuối vòng: hướng theo đoạn cuối cùng, không quay hướng về đầu.
        if (distance >= _pathLength)
        {
            Vector3 lastDirection = _points[0] - _points[_points.Length - 1];

            return lastDirection.sqrMagnitude > Mathf.Epsilon ? lastDirection.normalized : Vector3.forward;
        }

        int segmentIndex = FindSegmentIndex(distance);

        Vector3 direction = _points[(segmentIndex + 1) % _points.Length] - _points[segmentIndex];

        return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.forward;
    }

    public Quaternion GetRotationAtDistance(float distance)
    {
        Vector3 direction = GetDirectionAtDistance(distance);

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return Quaternion.identity;

        return Quaternion.LookRotation(direction, Vector3.up);
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
    public Vector3 GetPathPoint(int index)
    {
        if (!IsValid || _points == null || _points.Length == 0)
            return transform.position;

        if (index < 0 || index >= _points.Length)
            return transform.position;

        return _points[index];
    }
}
