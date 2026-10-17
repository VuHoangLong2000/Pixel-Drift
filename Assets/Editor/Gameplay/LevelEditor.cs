using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelEditor : EditorWindow
{
    // -------------------- GRID CONFIG --------------------

    private const int DefaultGridWidth = 12;
    private const int DefaultGridHeight = 16;
    private const int MaxGridDimension = 100;

    private const float MinCellButtonSize = 12f;
    private const float MaxCellButtonSize = 40f;
    private const float DefaultCellButtonSize = 22f;

    // -------------------- BOARD FIT CONFIG --------------------

    private const float ReferenceScreenWidth = 1080f;
    private const float BoardWidthRatio = 0.90f;
    private const float BoardHeightRatio = 0.52f;
    private const float MinRuntimeCellPixels = 24f;

    private static readonly string[] AspectNames =
    {
        "9:16",
        "9:19.5",
        "9:20"
    };

    private static readonly float[] ReferenceScreenHeights =
    {
        1920f,
        2340f,
        2400f
    };

    // -------------------- LEVEL DATA --------------------

    private LevelData levelData;
    private LevelData previousLevelData;

    private ColorPalette colorPalette;
    private ColorPalette previousColorPalette;

    // -------------------- PAINT SETTINGS --------------------

    private bool isIndestructible;
    private int selectedColor;
    private int hitPoints = 1;

    private bool placeIndestructiblePrefab;
    private int selectedPrefabIndex;

    private int paintControlId;
    private int paintUndoGroup;

    private bool isMousePainting;
    private bool isMouseErasing;

    private Vector2Int lastPaintedPosition = new Vector2Int(-1, -1);

    // -------------------- GRID UI --------------------

    private float cellButtonSize = DefaultCellButtonSize;

    private Vector2 gridScroll;
    private Vector2 windowScroll;

    private readonly Dictionary<Vector2Int, PixelBlockData> blockLookup = new Dictionary<Vector2Int, PixelBlockData>();

    // -------------------- SCENE PREVIEW --------------------

    private bool showScenePreview = true;
    private bool showGrid = true;

    private float cellSpacing = 1.1f;
    private float blockSize = 1f;
    private float blockHeight = 1f;

    private Vector3 previewOrigin = Vector3.zero;

    // -------------------- SCREEN PREVIEW --------------------

    private int selectedAspectIndex = 1;

    // -------------------- MENU --------------------

    [MenuItem("Tools/Pixel Flow/Level Editor")]
    private static void OpenWindow()
    {
        GetWindow<LevelEditor>("Pixel Flow Editor");
    }

    // -------------------- LIFECYCLE --------------------

    private void OnEnable()
    {
        SceneView.duringSceneGui -= DrawScenePreview;
        SceneView.duringSceneGui += DrawScenePreview;

        RebuildLookup();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawScenePreview;
        SceneView.RepaintAll();
    }

    // -------------------- MAIN GUI --------------------

    private void OnGUI()
    {
        HandleMouseRelease();

        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);

        EditorGUILayout.LabelField("Pixel Flow - Level Editor", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        DrawAssetSettings();
        DrawLevelButtons();

        if (levelData == null)
        {
            EditorGUILayout.HelpBox("Tạo Level Data mới hoặc gán một LevelData asset.", MessageType.Info);

            EditorGUILayout.EndScrollView();
            return;
        }

        EnsureLevelData();

        DrawBoardSettings();
        DrawBoardFitValidation();
        DrawTools();
        DrawGrid();
        DrawPalette();
        DrawPrefabPalette();
        DrawPreviewSettings();

        EditorGUILayout.EndScrollView();
    }

    private void DrawAssetSettings()
    {
        LevelData selectedLevel = (LevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(LevelData), false);

        if (selectedLevel != levelData)
        {
            levelData = selectedLevel;
            previousLevelData = selectedLevel;

            RebuildLookup();

            Repaint();
            SceneView.RepaintAll();
        }

        ColorPalette selectedPalette = (ColorPalette)EditorGUILayout.ObjectField("Color Palette", colorPalette, typeof(ColorPalette), false);

        if (selectedPalette != colorPalette)
        {
            colorPalette = selectedPalette;
            previousColorPalette = selectedPalette;
            selectedColor = 0;

            Repaint();
            SceneView.RepaintAll();
        }
    }

    private void DrawLevelButtons()
    {
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

            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.65f, 0.65f);

            if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
            {
                ClearLevel();
            }

            GUI.backgroundColor = previousBackground;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);
    }

    private void EnsureLevelData()
    {
        if (levelData.blocks == null)
        {
            Undo.RecordObject(levelData, "Initialize Level Blocks");
            levelData.blocks = new List<PixelBlockData>();
            EditorUtility.SetDirty(levelData);
        }

        levelData.width = Mathf.Clamp(levelData.width, 1, MaxGridDimension);
        levelData.height = Mathf.Clamp(levelData.height, 1, MaxGridDimension);
    }

    // -------------------- BOARD SETTINGS --------------------

    private void DrawBoardSettings()
    {
        EditorGUILayout.LabelField("Board Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        int newWidth = EditorGUILayout.IntField("Width", levelData.width);

        int newHeight = EditorGUILayout.IntField("Height", levelData.height);

        EditorGUILayout.EndHorizontal();

        newWidth = Mathf.Clamp(newWidth, 1, MaxGridDimension);
        newHeight = Mathf.Clamp(newHeight, 1, MaxGridDimension);

        if (newWidth != levelData.width ||
            newHeight != levelData.height)
        {
            Undo.RecordObject(levelData, "Resize Level Grid");

            levelData.width = newWidth;
            levelData.height = newHeight;

            EditorUtility.SetDirty(levelData);

            RebuildLookup();
            Repaint();
            SceneView.RepaintAll();
        }

        int outOfBoundsCount = CountOutOfBoundsBlocks();

        EditorGUILayout.LabelField($"Dimensions: {levelData.width} x {levelData.height}", EditorStyles.miniBoldLabel);

        if (outOfBoundsCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"{outOfBoundsCount} block nằm ngoài biên board. " +
                "Các block này chưa bị xóa.",
                MessageType.Warning
            );

            if (GUILayout.Button("Select Out-of-Bounds Blocks"))
            {
                Debug.Log("Các block ngoài biên vẫn được giữ nguyên trong LevelData.");
            }

            if (GUILayout.Button("Remove Out-of-Bounds Blocks"))
            {
                RemoveOutOfBoundsBlocks();
            }
        }

        EditorGUILayout.Space(6);
    }

    private int CountOutOfBoundsBlocks()
    {
        if (levelData == null || levelData.blocks == null)
            return 0;

        int count = 0;

        foreach (PixelBlockData block in levelData.blocks)
        {
            if (block == null || !IsInsideGrid(block.position))
                count++;
        }

        return count;
    }

    private bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < levelData.width &&
               position.y >= 0 &&
               position.y < levelData.height;
    }

    private void RemoveOutOfBoundsBlocks()
    {
        if (levelData == null || levelData.blocks == null)
            return;

        bool confirm = EditorUtility.DisplayDialog(
            "Remove Out-of-Bounds Blocks",
            "Xóa các block nằm ngoài Width/Height hiện tại?",
            "Xóa",
            "Hủy"
        );

        if (!confirm)
            return;

        Undo.RecordObject(levelData, "Remove Out-of-Bounds Blocks");

        levelData.blocks.RemoveAll(block => block == null || !IsInsideGrid(block.position));

        RebuildLookup();

        EditorUtility.SetDirty(levelData);
        Repaint();
        SceneView.RepaintAll();
    }

    // -------------------- BOARD FIT VALIDATION --------------------

    private void DrawBoardFitValidation()
    {
        EditorGUILayout.LabelField("Board Fit Validation", EditorStyles.boldLabel);

        selectedAspectIndex = EditorGUILayout.Popup(
            "Preview Aspect Ratio",
            selectedAspectIndex,
            AspectNames
        );

        bool fitsAllScreens = true;

        for (int i = 0; i < ReferenceScreenHeights.Length; i++)
        {
            float cellPixels = CalculateCellPixels(
                levelData.width,
                levelData.height,
                ReferenceScreenWidth,
                ReferenceScreenHeights[i]
            );

            bool fits = cellPixels >= MinRuntimeCellPixels;

            if (!fits)
                fitsAllScreens = false;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(AspectNames[i], GUILayout.Width(70));

            EditorGUILayout.LabelField($"{cellPixels:F1} px", GUILayout.Width(65));

            EditorGUILayout.LabelField(fits ? "Đạt" : "Quá nhỏ", fits ? EditorStyles.miniLabel : EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();
        }

        if (fitsAllScreens)
        {
            EditorGUILayout.HelpBox(
                "Board đạt ngưỡng kích thước ô tối thiểu " +
                "trên các cấu hình đang kiểm tra.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Board quá lớn để giữ kích thước ô tối thiểu. " +
                "Hãy giảm Width hoặc Height. Dữ liệu block " +
                "không bị tự động xóa.",
                MessageType.Warning
            );
        }

        DrawScreenPreview();

        EditorGUILayout.Space(8);
    }

    private float CalculateCellPixels(int width, int height, float screenWidth, float screenHeight)
    {
        float availableWidth = screenWidth * BoardWidthRatio;
        float availableHeight = screenHeight * BoardHeightRatio;

        float cellWidth = availableWidth / Mathf.Max(1, width);
        float cellHeight = availableHeight / Mathf.Max(1, height);

        return Mathf.Min(cellWidth, cellHeight);
    }

    private void DrawScreenPreview()
    {
        EditorGUILayout.LabelField(
            "Screen Layout Preview",
            EditorStyles.miniBoldLabel
        );

        float screenHeightRatio = ReferenceScreenHeights[selectedAspectIndex] / ReferenceScreenWidth;

        float previewHeight = 190f;
        float previewWidth = previewHeight / screenHeightRatio;

        Rect area = GUILayoutUtility.GetRect(
            200f,
            215f,
            GUILayout.ExpandWidth(true)
        );

        Rect screenRect = new Rect(
            area.center.x - previewWidth * 0.5f,
            area.y + 5f,
            previewWidth,
            previewHeight
        );

        EditorGUI.DrawRect(
            screenRect,
            new Color(0.12f, 0.12f, 0.12f)
        );

        // Vùng gameplay dành cho board.
        float boardWidth = screenRect.width * BoardWidthRatio;
        float boardHeight = screenRect.height * BoardHeightRatio;

        Rect boardRect = new Rect(
            screenRect.center.x - boardWidth * 0.5f,
            screenRect.center.y - boardHeight * 0.5f,
            boardWidth,
            boardHeight
        );

        EditorGUI.DrawRect(
            boardRect,
            new Color(0.20f, 0.38f, 0.24f)
        );

        Color borderColor = new Color(0.35f, 0.9f, 0.45f);

        EditorGUI.DrawRect(
            new Rect(boardRect.x, boardRect.y, boardRect.width, 1f),
            borderColor
        );

        EditorGUI.DrawRect(
            new Rect(boardRect.x, boardRect.yMax - 1f, boardRect.width, 1f),
            borderColor
        );

        EditorGUI.DrawRect(
            new Rect(boardRect.x, boardRect.y, 1f, boardRect.height),
            borderColor
        );

        EditorGUI.DrawRect(
            new Rect(boardRect.xMax - 1f, boardRect.y, 1f, boardRect.height),
            borderColor
        );

        GUI.Label(
            new Rect(
                screenRect.x - 15f,
                screenRect.yMax + 2f,
                screenRect.width + 30f,
                18f
            ),
            AspectNames[selectedAspectIndex],
            EditorStyles.centeredGreyMiniLabel
        );
    }

    // -------------------- TOOLS --------------------

    private void DrawTools()
    {
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        hitPoints = Mathf.Max(
            1,
            EditorGUILayout.IntField("New Block HP", hitPoints)
        );

        isIndestructible = EditorGUILayout.Toggle(
            "New Block Indestructible",
            isIndestructible
        );

        if (isIndestructible)
        {
            EditorGUILayout.HelpBox(
                "Block màu mới đặt sẽ không thể bị phá trong gameplay.",
                MessageType.Info
            );
        }

        int blockCount = levelData.blocks != null
            ? levelData.blocks.Count
            : 0;

        EditorGUILayout.LabelField(
            $"Blocks: {blockCount}",
            EditorStyles.miniBoldLabel
        );

        EditorGUILayout.HelpBox(
            "Chuột trái: đặt block và kéo để vẽ.\n" +
            "Chuột phải: xóa block và kéo để xóa.",
            MessageType.Info
        );
    }

    // -------------------- GRID --------------------

    private void DrawGrid()
    {
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField(
            $"Grid: {levelData.width} x {levelData.height}",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "Ctrl + lăn chuột: zoom grid",
            EditorStyles.miniLabel
        );

        Event e = Event.current;

        paintControlId = GUIUtility.GetControlID(FocusType.Passive);

        gridScroll = EditorGUILayout.BeginScrollView(
            gridScroll,
            GUILayout.MinHeight(300f),
            GUILayout.ExpandHeight(true)
        );

        for (int y = levelData.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < levelData.width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);

                bool hasBlock = blockLookup.TryGetValue(
                    position,
                    out PixelBlockData block
                );

                Color cellColor = hasBlock
                    ? GetBlockColor(block)
                    : new Color(0.25f, 0.25f, 0.25f);

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

                bool isCenterColumn =
                    x == centerX ||
                    (levelData.width % 2 == 0 && x == centerX + 1);

                bool isCenterRow =
                    y == centerY ||
                    (levelData.height % 2 == 0 && y == centerY + 1);

                if (isCenterColumn || isCenterRow)
                {
                    cellColor = Color.Lerp(
                        cellColor,
                        new Color(1f, 0.8f, 0.1f),
                        0.25f
                    );
                }

                if (isCenterColumn && isCenterRow)
                {
                    cellColor = new Color(1f, 0.65f, 0.05f);
                }

                EditorGUI.DrawRect(rect, cellColor);

                if (hasBlock)
                {
                    string label = block.isIndestructible
                        ? "∞"
                        : block.hitPoints.ToString();

                    GUI.Label(
                        rect,
                        label,
                        EditorStyles.centeredGreyMiniLabel
                    );
                }

                // Ctrl + cuộn chuột để zoom grid.
                if (e.type == EventType.ScrollWheel &&
                    e.control &&
                    rect.Contains(e.mousePosition))
                {
                    float factor = e.delta.y > 0f ? 0.9f : 1.1f;

                    cellButtonSize = Mathf.Clamp(
                        cellButtonSize * factor,
                        MinCellButtonSize,
                        MaxCellButtonSize
                    );

                    e.Use();
                    Repaint();
                    GUIUtility.ExitGUI();
                }

                if (e.type == EventType.MouseDown &&
                    rect.Contains(e.mousePosition) &&
                    (e.button == 0 || e.button == 1))
                {
                    Undo.IncrementCurrentGroup();

                    paintUndoGroup = Undo.GetCurrentGroup();

                    Undo.SetCurrentGroupName(
                        e.button == 0
                            ? "Paint Pixel Flow Level"
                            : "Erase Pixel Flow Level"
                    );

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

        if (e.type == EventType.MouseDrag &&
            (isMousePainting || isMouseErasing))
        {
            Repaint();
        }
    }

    private void HandleMouseRelease()
    {
        Event e = Event.current;

        if (e.type != EventType.MouseUp ||
            (!isMousePainting && !isMouseErasing))
        {
            return;
        }

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

    // -------------------- PAINT / ERASE --------------------

    private void PaintCell(Vector2Int position, bool erase)
    {
        if (levelData == null || !IsInsideGrid(position))
            return;

        if (erase)
        {
            if (!blockLookup.ContainsKey(position))
                return;

            Undo.RecordObject(levelData, "Erase Pixel Flow Block");

            levelData.blocks.RemoveAll(
                block => block != null && block.position == position
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

            GameObject prefab =
                colorPalette.indestructiblePrefabs[selectedPrefabIndex];

            if (prefab == null)
            {
                Debug.LogWarning("Prefab đang chọn bị thiếu.");
                return;
            }

            Undo.RecordObject(levelData, "Place Indestructible Block");

            if (blockLookup.TryGetValue(position, out PixelBlockData existing))
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
            if (colorPalette == null ||
                colorPalette.entries == null ||
                selectedColor < 0 ||
                selectedColor >= colorPalette.entries.Count)
            {
                return;
            }

            PaletteEntry entry = colorPalette.entries[selectedColor];

            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                return;

            Undo.RecordObject(levelData, "Paint Pixel Flow Block");

            if (blockLookup.TryGetValue(position, out PixelBlockData existing))
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

    // -------------------- COLOR PALETTE --------------------

    private void DrawPalette()
    {
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField(
            "Color Palette",
            EditorStyles.boldLabel
        );

        if (colorPalette == null || colorPalette.entries == null)
        {
            EditorGUILayout.HelpBox(
                "Hãy tạo và gán ColorPalette asset.",
                MessageType.Warning
            );
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

                Color previousBackground = GUI.backgroundColor;

                GUI.backgroundColor = entry.color;

                string label = selectedColor == j
                    ? "> " + entry.displayName
                    : entry.displayName;

                if (GUILayout.Button(label, GUILayout.Height(28)))
                {
                    selectedColor = j;
                    placeIndestructiblePrefab = false;
                }

                GUI.backgroundColor = previousBackground;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    // -------------------- PREFAB PALETTE --------------------

    private void DrawPrefabPalette()
    {
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField(
            "Indestructible Prefabs",
            EditorStyles.boldLabel
        );

        if (colorPalette == null ||
            colorPalette.indestructiblePrefabs == null ||
            colorPalette.indestructiblePrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Thêm prefab vào Indestructible Prefabs trong ColorPalette.",
                MessageType.Info
            );
            return;
        }

        List<GameObject> prefabs =
            colorPalette.indestructiblePrefabs;

        List<string> names = new List<string>();

        foreach (GameObject prefab in prefabs)
        {
            names.Add(
                prefab != null
                    ? prefab.name
                    : "(Missing Prefab)"
            );
        }

        selectedPrefabIndex = Mathf.Clamp(
            selectedPrefabIndex,
            0,
            prefabs.Count - 1
        );

        selectedPrefabIndex = EditorGUILayout.Popup(
            "Selected Prefab",
            selectedPrefabIndex,
            names.ToArray()
        );

        placeIndestructiblePrefab = EditorGUILayout.Toggle(
            "Place Prefab Mode",
            placeIndestructiblePrefab
        );

        if (placeIndestructiblePrefab)
        {
            EditorGUILayout.HelpBox(
                "Chuột trái đặt prefab. Chuột phải xóa block.",
                MessageType.None
            );
        }
    }

    // -------------------- SCENE PREVIEW SETTINGS --------------------

    private void DrawPreviewSettings()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "3D Scene Preview",
            EditorStyles.boldLabel
        );

        showScenePreview = EditorGUILayout.Toggle(
            "Show Preview",
            showScenePreview
        );

        using (new EditorGUI.DisabledScope(!showScenePreview))
        {
            showGrid = EditorGUILayout.Toggle(
                "Show Grid",
                showGrid
            );

            cellSpacing = Mathf.Max(
                0.1f,
                EditorGUILayout.FloatField("Cell Spacing", cellSpacing)
            );

            blockSize = Mathf.Clamp(
                EditorGUILayout.FloatField("Block Size", blockSize),
                0.01f,
                cellSpacing
            );

            blockHeight = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField("Block Height", blockHeight)
            );

            previewOrigin = EditorGUILayout.Vector3Field(
                "Preview Origin",
                previewOrigin
            );

            if (GUILayout.Button("Frame Preview"))
            {
                FramePreview();
            }
        }

        SceneView.RepaintAll();
    }

    // -------------------- SCENE VIEW DRAWING --------------------

    private void DrawScenePreview(SceneView sceneView)
    {
        if (!showScenePreview ||
            levelData == null ||
            levelData.blocks == null)
        {
            return;
        }

        DrawGridLines();
        DrawCenterMarker();

        foreach (PixelBlockData block in levelData.blocks)
        {
            if (block == null || !IsInsideGrid(block.position))
                continue;

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

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);

            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            string label = block.isIndestructible
                ? "∞"
                : block.hitPoints.ToString();

            Handles.Label(
                position + Vector3.up * (blockHeight * 0.5f + 0.1f),
                label,
                labelStyle
            );
        }
    }

    private void DrawGridLines()
    {
        if (!showGrid || levelData == null)
            return;

        float width = levelData.width * cellSpacing;
        float depth = levelData.height * cellSpacing;

        Vector3 bottomLeft = previewOrigin +
            new Vector3(-width * 0.5f, 0.01f, -depth * 0.5f);

        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        for (int x = 0; x <= levelData.width; x++)
        {
            Vector3 start = bottomLeft +
                Vector3.right * (x * cellSpacing);

            Handles.DrawLine(
                start,
                start + Vector3.forward * depth
            );
        }

        for (int z = 0; z <= levelData.height; z++)
        {
            Vector3 start = bottomLeft +
                Vector3.forward * (z * cellSpacing);

            Handles.DrawLine(
                start,
                start + Vector3.right * width
            );
        }
    }

    private void DrawCenterMarker()
    {
        if (levelData == null)
            return;

        float width = levelData.width * cellSpacing;
        float depth = levelData.height * cellSpacing;

        Handles.color = Color.yellow;

        Handles.DrawLine(
            previewOrigin + Vector3.left * (width * 0.5f),
            previewOrigin + Vector3.right * (width * 0.5f)
        );

        Handles.DrawLine(
            previewOrigin + Vector3.back * (depth * 0.5f),
            previewOrigin + Vector3.forward * (depth * 0.5f)
        );

        Handles.color = new Color(1f, 0.45f, 0f);

        Handles.SphereHandleCap(
            0,
            previewOrigin + Vector3.up * 0.05f,
            Quaternion.identity,
            cellSpacing * 0.35f,
            EventType.Repaint
        );

        Handles.Label(
            previewOrigin + Vector3.up * 0.3f,
            "CENTER",
            EditorStyles.boldLabel
        );
    }

    private Vector3 GridToWorld(Vector2Int position)
    {
        float xOffset =
            (position.x - (levelData.width - 1) * 0.5f)
            * cellSpacing;

        float zOffset =
            (position.y - (levelData.height - 1) * 0.5f)
            * cellSpacing;

        return previewOrigin + new Vector3(xOffset, 0f, zOffset);
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

    // -------------------- LOOKUP / COLOR --------------------

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

    private Color GetBlockColor(PixelBlockData block)
    {
        if (block == null)
            return Color.white;

        if (colorPalette != null &&
            !string.IsNullOrEmpty(block.colorId) &&
            colorPalette.TryGetColor(
                block.colorId,
                out Color paletteColor))
        {
            return paletteColor;
        }

        return block.color;
    }

    // -------------------- CREATE / SAVE / CLEAR --------------------

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

        if (CountOutOfBoundsBlocks() > 0)
        {
            bool saveAnyway = EditorUtility.DisplayDialog(
                "Blocks Outside Board",
                "Có block nằm ngoài biên. Vẫn lưu level?",
                "Lưu",
                "Hủy"
            );

            if (!saveAnyway)
                return;
        }

        EditorUtility.SetDirty(levelData);
        AssetDatabase.SaveAssets();

        Debug.Log($"Saved level: {levelData.name}");
    }

    private void ClearLevel()
    {
        if (levelData == null ||
            levelData.blocks == null ||
            levelData.blocks.Count == 0)
        {
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Clear Level",
            "Xóa toàn bộ block trong level này?",
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
}
