using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelBlockManager : MonoBehaviour
{
    [Header("Level Database")]
    [SerializeField] private LevelDatabaseSO levelDatabase;

    [Tooltip("Index bắt đầu từ 0: index 0 là Level 1.")]
    [SerializeField, Min(0)] private int startingLevelIndex = 0;

    [Header("Block Setup")]
    [Tooltip("Prefab mặc định nếu block không có customPrefab.")]
    [SerializeField] private GameObject blockPrefab;

    [Tooltip("Kích thước thực tế của mỗi block.")]
    [SerializeField, Min(0.1f)] private float cellSize = 1f;

    [Tooltip("Tâm của board trong World Space.")]
    [SerializeField] private Vector3 boardOrigin = Vector3.zero;

    [Tooltip("Chỉ chứa các block được tạo bởi PixelBlockManager.")]
    [SerializeField] private Transform blockRoot;

    private readonly List<PixelBlock> _blocks = new();
    private readonly List<GameObject> _spawnedObjects = new();

    public IReadOnlyList<PixelBlock> Blocks => _blocks;

    public LevelData CurrentLevelData { get; private set; }
    public int CurrentLevelIndex { get; private set; } = -1;

    // Các property dùng chung cho ConveyorPath và hệ thống khác.
    public Vector3 BoardOrigin => boardOrigin;
    public float CellSize => cellSize;

    public float CellSpacing => CurrentLevelData != null ? Mathf.Max(0.1f, CurrentLevelData.cellSpacing) : 1.1f;

    public event Action OnLevelLoaded;

    private void Start()
    {
        LoadLevel(startingLevelIndex);
    }

    public bool LoadLevel(int levelIndex)
    {
        if (levelDatabase == null)
        {
            Debug.LogError("[PixelBlockManager] LevelDatabaseSO is missing.", this);
            return false;
        }

        LevelData levelData = levelDatabase.GetLevel(levelIndex);

        if (levelData == null)
        {
            Debug.LogError($"[PixelBlockManager] Level index {levelIndex} was not found.", this);
            return false;
        }

        // Gán Level trước để GridToWorld dùng đúng dữ liệu mới.
        CurrentLevelData = levelData;
        CurrentLevelIndex = levelIndex;

        BuildLevel(levelData);

        Debug.Log($"[PixelBlockManager] Loaded Level {levelIndex + 1}: {_blocks.Count} blocks.", this);

        OnLevelLoaded?.Invoke();

        return true;
    }

    public void BuildLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("[PixelBlockManager] Cannot build a null LevelData.", this);
            return;
        }

        ClearBlocks();
        CurrentLevelData = levelData;

        if (levelData.blocks == null)
            return;

        foreach (PixelBlockData data in levelData.blocks)
        {
            if (data == null)
                continue;

            GameObject prefab = data.customPrefab != null ? data.customPrefab : blockPrefab;

            Vector3 worldPosition = GridToWorld(data.position);
            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab, worldPosition, Quaternion.identity, blockRoot);
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);

                if (blockRoot != null)
                    instance.transform.SetParent(blockRoot, true);

                instance.transform.position = worldPosition;
            }

            instance.transform.localScale = Vector3.one * cellSize;

            _spawnedObjects.Add(instance);

            PixelBlock pixelBlock = instance.GetComponent<PixelBlock>();

            if (pixelBlock == null)
                pixelBlock = instance.AddComponent<PixelBlock>();

            pixelBlock.Initialize(data, this);
            _blocks.Add(pixelBlock);
        }
    }

    /// <summary>
    /// Chuyển tọa độ ô thành World Position.
    /// Board được căn giữa quanh BoardOrigin.
    /// </summary>
    public Vector3 GridToWorld(Vector2Int position)
    {
        if (CurrentLevelData == null)
            return boardOrigin;

        float spacing = CellSpacing;

        float xOffset = (position.x - (CurrentLevelData.width - 1) * 0.5f) * spacing;

        float zOffset = (position.y - (CurrentLevelData.height - 1) * 0.5f) * spacing;

        return boardOrigin + new Vector3(xOffset, 0f, zOffset);
    }


    public PixelBlock FindFirstBlock(Vector3 origin, Vector3 direction, float maxDistance, float alignmentTolerance = 0.1f)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return null;

        direction.Normalize();

        PixelBlock nearestBlock = null;
        float nearestDistance = maxDistance;

        foreach (PixelBlock block in _blocks)
        {
            if (block == null || block.IsDestroyed || !block.isActiveAndEnabled)
                continue;

            // Block bất hoại vẫn chắn đường bắn.
            Vector3 toBlock = block.transform.position - origin;
            toBlock.y = 0f;

            float distanceAlongRay = Vector3.Dot(toBlock, direction);

            if (distanceAlongRay < 0f || distanceAlongRay > maxDistance)
                continue;

            Vector3 closestPoint = origin + direction * distanceAlongRay;
            Vector3 lateralOffset = block.transform.position - closestPoint;
            lateralOffset.y = 0f;

            if (lateralOffset.magnitude > alignmentTolerance)
                continue;

            if (distanceAlongRay < nearestDistance)
            {
                nearestDistance = distanceAlongRay;
                nearestBlock = block;
            }
        }

        return nearestBlock;
    }


    public void UnregisterBlock(PixelBlock block)
    {
        _blocks.Remove(block);
    }

    public void ClearBlocks()
    {
        foreach (GameObject spawnedObject in _spawnedObjects)
        {
            if (spawnedObject == null)
                continue;

            PixelBlock block = spawnedObject.GetComponent<PixelBlock>();

            if (block != null)
                block.Invalidate();

            if (Application.isPlaying)
                Destroy(spawnedObject);
            else
                DestroyImmediate(spawnedObject);
        }

        _spawnedObjects.Clear();
        _blocks.Clear();
    }
}
