using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelEditor : EditorWindow
{
    private const int DefaultGridWidth = 50;
    private const int DefaultGridHeight = 50;
    private const float MinCellButtonSize = 12f;
    private const float MaxCellButtonSize = 40f;
    private float cellButtonSize = 22f;

    private float gridContentWidth;

    private LevelData levelData;
    private LevelData previousLevelData;

    private ColorPalette colorPalette;
    private bool isIndestructible;

    private int selectedColor;
    private int hitPoints = 1;
    private int paintControlId;

    private bool placeIndestructiblePrefab;
    private int selectedPrefabIndex;

    private bool isMousePainting;
    private bool isMouseErasing;
    private Vector2Int lastPaintedPosition = new Vector2Int(-1, -1);

    private int paintUndoGroup;

    private bool showScenePreview = true;
    private bool showGrid = true;

    [SerializeField] private float cellSpacing = 1.1f;
    [SerializeField] private float blockSize = 1f;
    [SerializeField] private float blockHeight = 1f;
    [SerializeField] private Vector3 previewOrigin = Vector3.zero;

    private Vector2 gridScroll;
    private Vector2 windowScroll;

    private Dictionary<Vector2Int, PixelBlockData> blockLookup = new Dictionary<Vector2Int, PixelBlockData>();

    [MenuItem("Tools/Pixel Flow/Level Editor")]
    private static void OpenWindow()
    {
        GetWindow<LevelEditor>("Pixel Flow Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += DrawScenePreview;
        RebuildLookup();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawScenePreview;
        SceneView.RepaintAll();
    }


    private void OnGUI()
    {
        Event e = Event.current;

        // Kết thúc thao tác vẽ/xóa khi thả chuột.
        if (e.type == EventType.MouseUp && (isMousePainting || isMouseErasing))
        {
            Undo.CollapseUndoOperations(paintUndoGroup);

            isMousePainting = false;
            isMouseErasing = false;
            lastPaintedPosition = new Vector2Int(-1, -1);

            if (GUIUtility.hotControl == paintControlId)
            {
                GUIUtility.hotControl = 0;
            }

            e.Use();
        }

        // Scroll tổng cho toàn bộ cửa sổ.
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);

        EditorGUILayout.LabelField("Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        levelData = (LevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelData), false);

        if (levelData != previousLevelData)
        {
            previousLevelData = levelData;
            RebuildLookup();
            SceneView.RepaintAll();
        }

        ColorPalette previousPalette = colorPalette;

        colorPalette = (ColorPalette)EditorGUILayout.ObjectField("Color Palette", colorPalette, typeof(ColorPalette), false);

        if (colorPalette != previousPalette)
        {
            selectedColor = 0;
            Repaint();
            SceneView.RepaintAll();
        }


        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("New Level", GUILayout.Height(30)))
        {
            CreateNewLevel();
        }

        using (new EditorGUI.DisabledScope(levelData == null))
        {
            if (GUILayout.Button("Save Level", GUILayout.Height(30)))
            {
                SaveLevel();
            }

            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);

            if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
            {
                ClearLevel();
            }

            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Space(8);

        if (levelData == null)
        {
            EditorGUILayout.HelpBox("Create a new LevelData asset or assign an existing one.", MessageType.Info);

            EditorGUILayout.EndScrollView();
            return;
        }

        DrawTools();
        DrawGrid();
        DrawPalette();
        DrawPrefabPalette();
        DrawPreviewSettings();

        // Đóng scroll tổng.
        EditorGUILayout.EndScrollView();
    }


    private void DrawPreviewSettings()
    {
        EditorGUILayout.LabelField("3D Scene Preview", EditorStyles.boldLabel);

        showScenePreview = EditorGUILayout.Toggle("Show Preview", showScenePreview);

        using (new EditorGUI.DisabledScope(!showScenePreview))
        {
            showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);

            cellSpacing = Mathf.Max(0.1f, EditorGUILayout.FloatField("Cell Spacing", cellSpacing));

            blockSize = Mathf.Clamp(EditorGUILayout.FloatField("Block Size", blockSize), 0.01f, cellSpacing);

            blockHeight = Mathf.Max(0.01f, EditorGUILayout.FloatField("Block Height", blockHeight));

            previewOrigin = EditorGUILayout.Vector3Field("Preview Origin", previewOrigin);

            if (GUILayout.Button("Frame Preview"))
            {
                FramePreview();
            }
        }

        SceneView.RepaintAll();
        EditorGUILayout.Space(8);
    }


    private void DrawTools()
    {
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        hitPoints = Mathf.Max(1, EditorGUILayout.IntField("New Block HP", hitPoints));

        isIndestructible = EditorGUILayout.Toggle("New Block Indestructible", isIndestructible);

        if (isIndestructible)
        {
            EditorGUILayout.HelpBox("Block màu được đặt mới sẽ không thể bị phá trong gameplay.", MessageType.Info);
        }

        int blockCount = levelData.blocks != null ? levelData.blocks.Count : 0;

        EditorGUILayout.LabelField($"Blocks: {blockCount}", EditorStyles.miniBoldLabel);

        EditorGUILayout.HelpBox(
            "Chuột trái: đặt block và kéo để vẽ.\n" +
            "Chuột phải: xóa block và kéo để xóa nhiều ô.",
            MessageType.Info
        );

        EditorGUILayout.Space(5);
    }



    private void DrawPalette()
    {
        EditorGUILayout.LabelField("Color Palette", EditorStyles.boldLabel);

        if (colorPalette == null || colorPalette.entries == null)
        {
            EditorGUILayout.HelpBox("Hãy tạo và gán PixelFlowColorPalette.", MessageType.Warning);
            return;
        }

        for (int i = 0; i < colorPalette.entries.Count; i += 3)
        {
            EditorGUILayout.BeginHorizontal();

            for (int j = i; j < Mathf.Min(i + 3, colorPalette.entries.Count); j++)
            {
                PaletteEntry entry = colorPalette.entries[j];

                if (entry == null)
                    continue;

                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = entry.color;

                string label = selectedColor == j ? "> " + entry.displayName : entry.displayName;

                if (GUILayout.Button(label, GUILayout.Height(28)))
                {
                    selectedColor = j;
                    placeIndestructiblePrefab = false;
                }

                GUI.backgroundColor = previousColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8);
    }




    private void DrawGrid()
    {
        EditorGUILayout.LabelField($"Grid: {levelData.width} x {levelData.height}", EditorStyles.boldLabel);

        Event e = Event.current;

        paintControlId = GUIUtility.GetControlID(FocusType.Passive);

        gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.MinHeight(300f), GUILayout.ExpandHeight(true));

        for (int y = levelData.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < levelData.width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                bool hasBlock = blockLookup.TryGetValue(position, out PixelBlockData block);

                Color cellColor = hasBlock ? GetBlockColor(block) : new Color(0.25f, 0.25f, 0.25f);

                if (hasBlock && block.customPrefab != null)
                {
                    cellColor = new Color(0.65f, 0.45f, 0.85f);
                }

                Rect rect = GUILayoutUtility.GetRect(
                    cellButtonSize,
                    cellButtonSize,
                    GUILayout.Width(cellButtonSize),
                    GUILayout.Height(cellButtonSize)
                );


                int centerX = (levelData.width - 1) / 2;
                int centerY = (levelData.height - 1) / 2;

                bool isCenterColumn = x == centerX || (levelData.width % 2 == 0 && x == centerX + 1);

                bool isCenterRow = y == centerY || (levelData.height % 2 == 0 && y == centerY + 1);

                if (isCenterColumn || isCenterRow)
                {
                    cellColor = Color.Lerp(cellColor, new Color(1f, 0.8f, 0.1f), 0.35f);
                }

                if (isCenterColumn && isCenterRow)
                {
                    cellColor = new Color(1f, 0.65f, 0.05f);
                }


                EditorGUI.DrawRect(rect, cellColor);

                if (hasBlock)
                {
                    string label = block.isIndestructible ? "∞" : block.hitPoints.ToString();

                    GUI.Label(rect, label, EditorStyles.centeredGreyMiniLabel);
                }

                // Ctrl + cuộn chuột để zoom grid.
                if (e.type == EventType.ScrollWheel && e.control && rect.Contains(e.mousePosition))
                {
                    float factor = e.delta.y > 0f ? 0.9f : 1.1f;

                    cellButtonSize = Mathf.Clamp(cellButtonSize * factor, MinCellButtonSize, MaxCellButtonSize);

                    e.Use();
                    Repaint();
                    GUIUtility.ExitGUI();
                }

                // Chuột trái vẽ, chuột phải xóa.
                if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && (e.button == 0 || e.button == 1))
                {
                    Undo.IncrementCurrentGroup();
                    paintUndoGroup = Undo.GetCurrentGroup();

                    Undo.SetCurrentGroupName(e.button == 0 ? "Paint Pixel Flow Level" : "Erase Pixel Flow Level");

                    isMousePainting = e.button == 0;
                    isMouseErasing = e.button == 1;

                    GUIUtility.hotControl = paintControlId;

                    PaintCell(position, isMouseErasing);

                    lastPaintedPosition = position;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag &&
                         rect.Contains(e.mousePosition) &&
                         (isMousePainting || isMouseErasing) &&
                         position != lastPaintedPosition)
                {
                    PaintCell(position, isMouseErasing);

                    lastPaintedPosition = position;
                    e.Use();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (e.type == EventType.MouseDrag && (isMousePainting || isMouseErasing))
        {
            Repaint();
        }
    }


    private void PaintCell(Vector2Int position, bool erase)
    {
        if (levelData == null)
            return;

        if (erase)
        {
            if (!blockLookup.ContainsKey(position))
                return;

            Undo.RecordObject(levelData, "Erase Pixel Flow Block");

            levelData.blocks.RemoveAll(
                block => block.position == position
            );
        }
        else if (placeIndestructiblePrefab)
        {
            if (colorPalette == null ||
                colorPalette.indestructiblePrefabs == null ||
                selectedPrefabIndex < 0 ||
                selectedPrefabIndex >= colorPalette.indestructiblePrefabs.Count)
            {
                return;
            }

            GameObject prefab = colorPalette.indestructiblePrefabs[selectedPrefabIndex];

            if (prefab == null)
            {
                Debug.LogWarning("Prefab đang chọn bị thiếu.");
                return;
            }

            Undo.RecordObject(levelData, "Place Indestructible Block");

            PixelBlockData existing = blockLookup.ContainsKey(position) ? blockLookup[position] : null;

            if (existing != null)
            {
                existing.customPrefab = prefab;

                existing.colorId = string.Empty;
                existing.color = Color.white;

                existing.isIndestructible = true;
                existing.hitPoints = 0;
            }
            else
            {
                levelData.blocks.Add(new PixelBlockData
                {
                    position = position,
                    customPrefab = prefab,
                    isIndestructible = true,
                    hitPoints = 0,
                    color = Color.white,
                    colorId = string.Empty
                });
            }
        }
        else
        {
            if (colorPalette == null || colorPalette.entries == null || selectedColor < 0 || selectedColor >= colorPalette.entries.Count)
                return;

            PaletteEntry entry = colorPalette.entries[selectedColor];

            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                return;

            Undo.RecordObject(levelData, "Paint Pixel Flow Block");

            if (blockLookup.TryGetValue(
                position,
                out PixelBlockData existing))
            {
                existing.colorId = entry.id;
                existing.color = entry.color;
                existing.hitPoints = hitPoints;

                existing.customPrefab = null;
                existing.isIndestructible = isIndestructible;
            }
            else
            {
                levelData.blocks.Add(new PixelBlockData
                {
                    position = position,
                    colorId = entry.id,
                    color = entry.color,
                    hitPoints = hitPoints,
                    isIndestructible = isIndestructible,
                    customPrefab = null
                });
            }
        }

        RebuildLookup();

        EditorUtility.SetDirty(levelData);

        Repaint();
        SceneView.RepaintAll();
    }

    private void DrawScenePreview(SceneView sceneView)
    {
        if (!showScenePreview || levelData == null)
            return;

        if (levelData.blocks == null)
            return;

        DrawGridLines();
        DrawCenterMarker();

        foreach (PixelBlockData block in levelData.blocks)
        {
            if (block == null)
                continue;

            if (block.position.x < 0 ||
                block.position.x >= levelData.width ||
                block.position.y < 0 ||
                block.position.y >= levelData.height)
            {
                continue;
            }

            Vector3 position = GridToWorld(block.position);

            position.y += blockHeight * 0.5f;

            Handles.color = GetBlockColor(block);

            Handles.CubeHandleCap(
                0,
                position,
                Quaternion.identity,
                blockSize,
                EventType.Repaint
            );

            // Small HP label above the block.
            GUIStyle labelStyle = new GUIStyle(
                EditorStyles.boldLabel
            );

            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            string blockLabel = block.isIndestructible ? "∞" : block.hitPoints.ToString();

            Handles.Label(position + Vector3.up * (blockHeight * 0.5f + 0.1f), blockLabel, labelStyle);
        }
    }

    private void DrawGridLines()
    {
        if (!showGrid)
            return;

        float width = levelData.width * cellSpacing;
        float depth = levelData.height * cellSpacing;

        Vector3 bottomLeft = previewOrigin
            + new Vector3(-width * 0.5f, 0.01f, -depth * 0.5f);

        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        for (int x = 0; x <= levelData.width; x++)
        {
            Vector3 start = bottomLeft
                + Vector3.right * (x * cellSpacing);

            Vector3 end = start + Vector3.forward * depth;

            Handles.DrawLine(start, end);
        }

        for (int z = 0; z <= levelData.height; z++)
        {
            Vector3 start = bottomLeft
                + Vector3.forward * (z * cellSpacing);

            Vector3 end = start + Vector3.right * width;

            Handles.DrawLine(start, end);
        }
    }

    private Vector3 GridToWorld(Vector2Int position)
    {
        float xOffset =
            (position.x - (levelData.width - 1) * 0.5f)
            * cellSpacing;

        float zOffset =
            (position.y - (levelData.height - 1) * 0.5f)
            * cellSpacing;

        return previewOrigin + new Vector3(
            xOffset,
            0f,
            zOffset
        );
    }

    private void FramePreview()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null || levelData == null)
            return;

        sceneView.pivot = previewOrigin;
        sceneView.rotation = Quaternion.Euler(35f, 45f, 0f);
        sceneView.size = Mathf.Max(
            levelData.width,
            levelData.height
        ) * cellSpacing * 0.8f;

        sceneView.orthographic = true;
        sceneView.Repaint();
    }

    private void RebuildLookup()
    {
        blockLookup.Clear();

        if (levelData == null || levelData.blocks == null)
            return;

        foreach (PixelBlockData block in levelData.blocks)
        {
            if (block == null)
                continue;

            blockLookup[block.position] = block;
        }
    }

    private void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Pixel Flow Level",
            "Level_001",
            "asset",
            "Choose where to save the level."
        );

        if (string.IsNullOrEmpty(path))
            return;

        LevelData newLevel = CreateInstance<LevelData>();

        newLevel.width = DefaultGridWidth;
        newLevel.height = DefaultGridHeight;

        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();

        levelData = newLevel;
        previousLevelData = newLevel;

        RebuildLookup();
        EditorUtility.SetDirty(newLevel);

        Repaint();
        SceneView.RepaintAll();
    }

    private void SaveLevel()
    {
        if (levelData == null)
            return;

        EditorUtility.SetDirty(levelData);
        AssetDatabase.SaveAssets();

        Debug.Log($"Saved level: {levelData.name}");
    }

    private Color GetBlockColor(PixelBlockData block)
    {
        if (colorPalette != null &&
            !string.IsNullOrEmpty(block.colorId) &&
            colorPalette.TryGetColor(block.colorId, out Color paletteColor))
        {
            return paletteColor;
        }

        // Fallback cho level cũ hoặc ID màu không còn tồn tại.
        return block.color;
    }


    private void DrawPrefabPalette()
    {
        EditorGUILayout.LabelField("Indestructible Prefabs", EditorStyles.boldLabel);

        if (colorPalette == null || colorPalette.indestructiblePrefabs == null || colorPalette.indestructiblePrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Hãy thêm prefab vào mục Indestructible Block Prefabs " +
                "trong Color Palette asset.",
                MessageType.Info
            );
            return;
        }

        List<GameObject> prefabs = colorPalette.indestructiblePrefabs;

        List<string> names = new List<string>();

        foreach (GameObject prefab in prefabs)
        {
            names.Add(prefab != null ? prefab.name : "(Missing Prefab)");
        }

        selectedPrefabIndex = Mathf.Clamp(selectedPrefabIndex, 0, prefabs.Count - 1);

        selectedPrefabIndex = EditorGUILayout.Popup("Selected Prefab", selectedPrefabIndex, names.ToArray());

        placeIndestructiblePrefab = EditorGUILayout.Toggle("Place Prefab Mode", placeIndestructiblePrefab );

        if (placeIndestructiblePrefab)
        {
            EditorGUILayout.HelpBox("Chuột trái để đặt prefab. Chuột phải để xóa block.", MessageType.None);
        }
    }

    private void HandleGridZoom()
    {
        Event e = Event.current;

        if (e.type != EventType.ScrollWheel || !e.control)
        {
            return;
        }

        // Chỉ zoom khi con trỏ nằm trong vùng grid.
        Rect gridRect = GUILayoutUtility.GetLastRect();

        if (!gridRect.Contains(e.mousePosition))
            return;

        float zoomFactor = e.delta.y > 0f ? 0.9f : 1.1f;

        cellButtonSize = Mathf.Clamp(cellButtonSize * zoomFactor, MinCellButtonSize, MaxCellButtonSize);

        e.Use();
        Repaint();
    }

    private void ClearLevel()
    {
        if (levelData == null || levelData.blocks == null || levelData.blocks.Count == 0)
        {
            return;
        }

        bool confirm = EditorUtility.DisplayDialog("Clear Level",
            "có chắc muốn xóa toàn bộ block trong level này không?",
            "Xóa toàn bộ",
            "Hủy"
        );

        if (!confirm)
            return;

        Undo.RecordObject(levelData, "Clear Pixel Flow Level");

        levelData.blocks.Clear();

        RebuildLookup();

        EditorUtility.SetDirty(levelData);

        Repaint();
        SceneView.RepaintAll();
    }

    private void DrawCenterMarker()
    {
        if (levelData == null)
            return;

        float width = levelData.width * cellSpacing;
        float depth = levelData.height * cellSpacing;

        Vector3 center = previewOrigin;

        Handles.color = Color.yellow;

        // Trục giữa theo chiều ngang.
        Handles.DrawLine(center + Vector3.left * (width * 0.5f), center + Vector3.right * (width * 0.5f));

        // Trục giữa theo chiều sâu.
        Handles.DrawLine(center + Vector3.back * (depth * 0.5f), center + Vector3.forward * (depth * 0.5f));

        // Điểm tâm.
        Handles.color = new Color(1f, 0.45f, 0f);
        Handles.SphereHandleCap(
            0,
            center + Vector3.up * 0.05f,
            Quaternion.identity,
            cellSpacing * 0.35f,
            EventType.Repaint
        );

        Handles.Label(center + Vector3.up * 0.3f, "CENTER", EditorStyles.boldLabel);
    }

}
