using UnityEngine;

public class BlockAreaGenerator : MonoBehaviour
{
    [Header("Area Settings")]
    [SerializeField] private int rows;
    [SerializeField] private int columns;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float spacing = 0.1f;

    [Header("Tile Settings")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform tilesRoot;

    [ContextMenu("Generate Area")]
    public void GenerateArea()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("[BlockAreaGenerator] Chưa gán Tile Prefab!", this);
            return;
        }

        ClearArea();

        if (tilesRoot == null)
        {
            GameObject root = new GameObject("TilesRoot");
            root.transform.SetParent(transform, false);
            tilesRoot = root.transform;
        }

        float step = tileSize + spacing;

        // Căn giữa khu vực tại vị trí của BlockArea
        float startX = -((columns - 1) * step) / 2f;
        float startZ = -((rows - 1) * step) / 2f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 localPosition = new Vector3(startX + col * step, 0f, startZ + row * step);

                GameObject tile = Instantiate(tilePrefab, tilesRoot);

                tile.name = $"Tile_{row}_{col}";
                tile.transform.localPosition = localPosition;
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = Vector3.one * tileSize;
            }
        }

        Debug.Log($"[BlockAreaGenerator] Đã tạo {rows * columns} ô nền.");
    }

    [ContextMenu("Clear Area")]
    public void ClearArea()
    {
        if (tilesRoot == null) return;

        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = tilesRoot.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}
