using System;
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

    [SerializeField, Min(0f)] private float blockSpacing = 0.05f;

    [SerializeField, Min(0.1f)] private float cellSize = 1f;

    [SerializeField] private Vector3 boardOrigin = Vector3.zero;

    [Tooltip("Chỉ chứa các block được tạo bởi PixelBlockManager.")]
    [SerializeField] private Transform blockRoot;

    private readonly List<PixelBlock> _blocks = new();
    private readonly List<GameObject> _spawnedObjects = new();

    public IReadOnlyList<PixelBlock> Blocks => _blocks;

    public LevelData CurrentLevelData { get; private set; }
    public int CurrentLevelIndex { get; private set; } = -1;

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
            return false;

        BuildLevel(levelData);

        CurrentLevelData = levelData;
        CurrentLevelIndex = levelIndex;

        Debug.Log($"[PixelBlockManager] Loaded Level {levelIndex + 1}: " + $"{_blocks.Count} blocks.", this);

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

        foreach (PixelBlockData data in levelData.blocks)
        {
            if (data == null)
                continue;

            GameObject prefab = data.customPrefab != null ? data.customPrefab : blockPrefab;

            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab, GridToWorld(data.position), Quaternion.identity, blockRoot);
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);

                if (blockRoot != null)
                    instance.transform.SetParent(blockRoot, true);

                instance.transform.position = GridToWorld(data.position);
            }

            instance.transform.localScale = Vector3.one * cellSize;

            _spawnedObjects.Add(instance);

            PixelBlock pixelBlock = instance.GetComponent<PixelBlock>();

            if (pixelBlock == null)
                pixelBlock = instance.AddComponent<PixelBlock>();

            pixelBlock.Initialize(data, this);
            _blocks.Add(pixelBlock);
        }

        CurrentLevelData = levelData;
    }

    private Vector3 GridToWorld(Vector2Int position)
    {
        float step = cellSize + blockSpacing;

        return boardOrigin + new Vector3(position.x * step, 0f, position.y * step);
    }

    public PixelBlock FindFirstMatchingBlock(Vector3 origin, Vector3 direction, string colorId, float maxDistance, float alignmentTolerance = 0.1f)
    {
        if (string.IsNullOrWhiteSpace(colorId))
            return null;

        if (direction.sqrMagnitude < 0.0001f)
            return null;

        direction.Normalize();

        PixelBlock nearestBlock = null;
        float nearestDistance = maxDistance;

        foreach (PixelBlock block in _blocks)
        {
            if (block == null || block.IsDestroyed)
                continue;

            if (block.IsIndestructible)
                continue;

            if (block.ColorId != colorId)
                continue;

            Vector3 toBlock = block.transform.position - origin;

            float distanceAlongRay = Vector3.Dot(toBlock, direction);

            if (distanceAlongRay < 0f || distanceAlongRay > maxDistance)
                continue;

            Vector3 closestPoint = origin + direction * distanceAlongRay;

            float lateralDistance = Vector3.Distance(closestPoint, block.transform.position);

            if (lateralDistance > alignmentTolerance)
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
            if (spawnedObject != null)
                Destroy(spawnedObject);
        }

        _spawnedObjects.Clear();
        _blocks.Clear();
    }
}
