using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConveyorVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ConveyorPath conveyorPath;
    [SerializeField] private PixelBlockManager blockManager;

    [Header("Track Appearance")]
    [SerializeField, Min(0.1f)]
    private float trackWidth = 0.8f;

    [SerializeField, Min(0.01f)]
    private float stripeWorldLength = 0.8f;

    [SerializeField, Min(0f)]
    private float surfaceOffset = 0f;

    [Header("Animated Surface")]
    [SerializeField, Min(0f)]
    private float scrollSpeed = 0.8f;

    [SerializeField]
    private Color trackColor = new Color(0.08f, 0.12f, 0.16f, 1f);

    [SerializeField]
    private Color stripeColor = new Color(0.1f, 0.85f, 1f, 1f);

    [Header("Texture")]
    [SerializeField, Min(4)]
    private int textureWidth = 64;

    [SerializeField, Min(4)]
    private int textureHeight = 16;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private Mesh _generatedMesh;
    private Material _runtimeMaterial;
    private Texture2D _generatedTexture;

    private bool _isSubscribed;

    private void OnEnable()
    {
        CacheComponents();
        SubscribeEvents();
        Rebuild();
    }

    private void Start()
    {
        Rebuild();
    }

    private void Update()
    {
        if (Application.isPlaying && _runtimeMaterial != null) 
        {
            Vector2 offset = _runtimeMaterial.mainTextureOffset;

            offset.x = Mathf.Repeat(offset.x - scrollSpeed * Time.deltaTime, 1f);

            _runtimeMaterial.mainTextureOffset = offset;
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        ReleaseGeneratedResources();
    }

    private void OnValidate()
    {
        trackWidth = Mathf.Max(0.1f, trackWidth);
        stripeWorldLength = Mathf.Max(0.01f, stripeWorldLength);
        textureWidth = Mathf.Max(4, textureWidth);
        textureHeight = Mathf.Max(4, textureHeight);

        if (!isActiveAndEnabled)
            return;

        CacheComponents();
        Rebuild();
    }

    private void CacheComponents()
    {
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();

        if (conveyorPath == null)
            conveyorPath = FindFirstObjectByType<ConveyorPath>();

        if (blockManager == null)
            blockManager = FindFirstObjectByType<PixelBlockManager>();
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed || blockManager == null)
            return;

        blockManager.OnLevelLoaded -= HandleLevelLoaded;
        blockManager.OnLevelLoaded += HandleLevelLoaded;

        _isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed)
            return;

        if (blockManager != null)
            blockManager.OnLevelLoaded -= HandleLevelLoaded;

        _isSubscribed = false;
    }

    private void HandleLevelLoaded()
    {
        if (conveyorPath != null)
            conveyorPath.RefreshPath();

        Rebuild();
    }

    public void Rebuild()
    {
        CacheComponents();

        if (_meshFilter == null || _meshRenderer == null)
            return;

        if (conveyorPath == null || !conveyorPath.IsValid)
        {
            ClearGeneratedMesh();
            return;
        }

        int pointCount = conveyorPath.PointCount;

        if (pointCount < 3)
        {
            ClearGeneratedMesh();
            return;
        }

        Vector3[] points = new Vector3[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            points[i] = conveyorPath.GetPathPoint(i);
            points[i].y += surfaceOffset;
        }

        BuildMesh(points);
        EnsureMaterial();
    }

    private void BuildMesh(Vector3[] points)
    {
        int count = points.Length;
        int vertexCount = (count + 1) * 2;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[count * 6];

        float[] distances = new float[count + 1];
        distances[0] = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 next = points[(i + 1) % count];
            distances[i + 1] =
                distances[i] + Vector3.Distance(points[i], next);
        }

        float halfWidth = trackWidth * 0.5f;

        for (int i = 0; i <= count; i++)
        {
            int pointIndex = i % count;

            Vector3 current = points[pointIndex];
            Vector3 previous = points[(pointIndex - 1 + count) % count];
            Vector3 next = points[(pointIndex + 1) % count];

            Vector3 incoming = (current - previous).normalized;
            Vector3 outgoing = (next - current).normalized;

            Vector3 incomingNormal =
                new Vector3(-incoming.z, 0f, incoming.x);

            Vector3 outgoingNormal =
                new Vector3(-outgoing.z, 0f, outgoing.x);

            Vector3 miter = incomingNormal + outgoingNormal;

            if (miter.sqrMagnitude < 0.0001f)
                miter = outgoingNormal;
            else
                miter.Normalize();

            float denominator =
                Vector3.Dot(miter, outgoingNormal);

            float miterLength = halfWidth;

            if (Mathf.Abs(denominator) > 0.001f)
            {
                miterLength = halfWidth / denominator;
            }

            // Tránh miter quá dài ở những góc nhọn.
            miterLength = Mathf.Clamp(
                miterLength,
                -halfWidth * 3f,
                halfWidth * 3f);

            Vector3 left = current + miter * miterLength;
            Vector3 right = current - miter * miterLength;

            int vertexIndex = i * 2;

            vertices[vertexIndex] =
                transform.InverseTransformPoint(left);

            vertices[vertexIndex + 1] =
                transform.InverseTransformPoint(right);

            float u = distances[i] / stripeWorldLength;

            uvs[vertexIndex] = new Vector2(u, 0f);
            uvs[vertexIndex + 1] = new Vector2(u, 1f);
        }

        for (int i = 0; i < count; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;

            int triangleIndex = i * 6;

            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = c;
            triangles[triangleIndex + 2] = b;

            triangles[triangleIndex + 3] = b;
            triangles[triangleIndex + 4] = c;
            triangles[triangleIndex + 5] = d;
        }

        ClearGeneratedMesh();

        _generatedMesh = new Mesh
        {
            name = "Generated Conveyor Mesh"
        };

        _generatedMesh.vertices = vertices;
        _generatedMesh.uv = uvs;
        _generatedMesh.triangles = triangles;
        _generatedMesh.RecalculateNormals();
        _generatedMesh.RecalculateBounds();

        _meshFilter.sharedMesh = _generatedMesh;
    }

    private void EnsureMaterial()
    {
        if (_runtimeMaterial == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                Debug.LogError(
                    "[ConveyorVisual] Cannot find a supported shader.",
                    this);
                return;
            }

            _runtimeMaterial = new Material(shader)
            {
                name = "Generated Conveyor Material"
            };

            _generatedTexture = CreateStripeTexture();

            _runtimeMaterial.mainTexture = _generatedTexture;
            _runtimeMaterial.mainTextureOffset = Vector2.zero;
            _runtimeMaterial.mainTextureScale = Vector2.one;

            _meshRenderer.sharedMaterial = _runtimeMaterial;
        }
        else
        {
            // Refresh texture colors when rebuilding in the editor.
            if (_generatedTexture != null)
            {
                DestroyObject(_generatedTexture);
                _generatedTexture = null;
            }

            _generatedTexture = CreateStripeTexture();
            _runtimeMaterial.mainTexture = _generatedTexture;
        }
    }

    private Texture2D CreateStripeTexture()
    {
        Texture2D texture = new Texture2D(
            textureWidth,
            textureHeight,
            TextureFormat.RGBA32,
            false);

        texture.name = "Generated Conveyor Stripes";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[textureWidth * textureHeight];

        // Mỗi tile có một vạch sáng và một khoảng tối.
        int stripePixels = Mathf.Clamp(
            Mathf.RoundToInt(textureWidth * 0.65f),
            1,
            textureWidth - 1);

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                bool isStripe = x < stripePixels;

                pixels[y * textureWidth + x] =
                    isStripe ? stripeColor : trackColor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    private void ClearGeneratedMesh()
    {
        if (_meshFilter != null &&
            _meshFilter.sharedMesh == _generatedMesh)
        {
            _meshFilter.sharedMesh = null;
        }

        DestroyObject(_generatedMesh);
        _generatedMesh = null;
    }

    private void ReleaseGeneratedResources()
    {
        ClearGeneratedMesh();

        if (_meshRenderer != null &&
            _meshRenderer.sharedMaterial == _runtimeMaterial)
        {
            _meshRenderer.sharedMaterial = null;
        }

        DestroyObject(_runtimeMaterial);
        DestroyObject(_generatedTexture);

        _runtimeMaterial = null;
        _generatedTexture = null;
    }

    private static void DestroyObject(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }
}
