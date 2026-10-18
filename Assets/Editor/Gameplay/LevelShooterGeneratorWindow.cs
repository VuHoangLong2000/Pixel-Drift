
using UnityEditor;
using UnityEngine;

public class LevelShooterGeneratorWindow : EditorWindow
{
    private LevelDatabaseSO database;

    [MenuItem("Pixel Drift/Level Shooter Generator")]
    private static void Open()
    {
        GetWindow<LevelShooterGeneratorWindow>("Shooter Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Level Shooter Generator",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Generate Shooter configurations from each level's " +
            "destructible block colors. Existing Shooter data will " +
            "be replaced for the levels you generate.",
            MessageType.Info);

        database = (LevelDatabaseSO)EditorGUILayout.ObjectField(
            "Level Database",
            database,
            typeof(LevelDatabaseSO),
            false);

        if (database == null)
            return;

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            $"Total levels: {database.LevelCount}");

        if (GUILayout.Button("Generate All Levels"))
        {
            if (EditorUtility.DisplayDialog(
                "Generate all Shooter lists?",
                "This replaces the existing Shooter list in every level.",
                "Generate",
                "Cancel"))
            {
                GenerateAll();
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Generate individual level",
            EditorStyles.boldLabel);

        for (int i = 0; i < database.LevelCount; i++)
        {
            LevelData level = database.GetLevel(i);

            if (level == null)
                continue;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                $"Level {i + 1}: {level.name}");

            if (GUILayout.Button("Generate", GUILayout.Width(100)))
                GenerateOne(level);

            EditorGUILayout.EndHorizontal();
        }
    }

    private void GenerateAll()
    {
        int generatedCount = 0;

        foreach (LevelData level in database.Levels)
        {
            if (level == null)
                continue;

            GenerateOne(level, false);
            generatedCount++;
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[ShooterGenerator] Generated Shooter data for " +
            $"{generatedCount} levels.");

        EditorUtility.DisplayDialog(
            "Generation complete",
            $"Generated Shooter data for {generatedCount} levels.",
            "OK");
    }

    private void GenerateOne(LevelData level, bool saveAssets = true)
    {
        if (level == null)
            return;

        Undo.RecordObject(level, "Generate Shooter List");

        level.shooters = ShooterListGenerator.Generate(level);

        EditorUtility.SetDirty(level);

        if (saveAssets)
            AssetDatabase.SaveAssets();

        Debug.Log(
            $"[ShooterGenerator] {level.name}: " +
            $"{level.shooters.Count} Shooter groups generated.",
            level);
    }
}
