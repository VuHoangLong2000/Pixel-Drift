using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System;

[CustomEditor(typeof(BaseScreenSO))]
public class BaseScreenSOEditor : Editor
{
    private GameObject prefab;

    private ScreenGroup group = ScreenGroup.Feature;

    private bool keepActive = true;


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(20);

        EditorGUILayout.LabelField("Screen Tool",  EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

        group = (ScreenGroup)EditorGUILayout.EnumPopup("Group", group);

        keepActive = EditorGUILayout.Toggle("Keep Active", keepActive);

        GUILayout.Space(10);

        GUI.enabled = prefab != null;

        if (GUILayout.Button("Add Screen"))
        {
            AddScreen();
        }

        GUI.enabled = true;

        GUILayout.Space(10);

        if (GUILayout.Button("Scan Folder"))
        {
            ScanFolder();
        }

        if (GUILayout.Button("Remove Missing"))
        {
            RemoveMissing();
        }

        if (GUILayout.Button("Sort"))
        {
            Sort();
        }
    }

    private void AddScreen()
    {
        BaseScreenSO database = (BaseScreenSO)target;

        string id = GetScreenID(prefab.name);

        if (database.screens.Exists(x => x.ScreenID == id))
        {
            EditorUtility.DisplayDialog("Duplicate", $"Screen [{id}] already exists.", "OK");
            return;
        }

        string guid =  AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath(prefab));

        ScreenData data = new ScreenData();

        data.ScreenID = id;
        data.displayName = prefab.name;

        data.group = DetectGroup(prefab);

        data.keepActive = DetectKeepAlive(data.group);

        data.prefabReference = new AssetReferenceGameObject(guid);

        database.screens.Add(data);

        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();

        prefab = null;
    }
    private void ScanFolder()
    {
        BaseScreenSO database = (BaseScreenSO)target;

        string folder = EditorUtility.OpenFolderPanel("Select Screen Folder", "Assets", "");

        if (string.IsNullOrEmpty(folder))
            return;

        folder = "Assets" + folder.Replace(Application.dataPath, "");

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { folder });

        foreach (string guid in guids)
        {

            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));

            if (obj == null)
                continue;

            string id = GetScreenID(obj.name);

            if (database.screens.Exists(x => x.ScreenID == id))
                continue;

            ScreenGroup detectedGroup = DetectGroup(obj);

            database.screens.Add(new ScreenData
                {
                    ScreenID = id,
                    displayName = obj.name,
                    group = detectedGroup,
                    keepActive = DetectKeepAlive(detectedGroup),
                    prefabReference = new AssetReferenceGameObject(guid)
                });
        }

        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();

        Debug.Log("Scan Screen Complete");
    }

    private void RemoveMissing()
    {
        BaseScreenSO database = (BaseScreenSO)target;

        database.screens.RemoveAll(x => x.prefabReference == null || string.IsNullOrEmpty(x.prefabReference.AssetGUID));

        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
    }

    private void Sort()
    {
        BaseScreenSO database = (BaseScreenSO)target;

        database.screens.Sort((a, b) => string.Compare(a.ScreenID, b.ScreenID, StringComparison.Ordinal));

        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
    }

    private string GetScreenID(string name)
    {
        return name
            .Replace("Screen", "")
            .Replace("Popup", "")
            .Trim();
    }

    private ScreenGroup DetectGroup(GameObject obj)
    {
        string path = AssetDatabase.GetAssetPath(obj).ToLower();

        if (path.Contains("/core/"))
            return ScreenGroup.Core;


        if (path.Contains("/popup/"))
            return ScreenGroup.Popup;


        if (path.Contains("/overlay/"))
            return ScreenGroup.Overlay;


        return ScreenGroup.Feature;
    }
    private bool DetectKeepAlive(ScreenGroup group)
    {
        switch (group)
        {
            case ScreenGroup.Core:
                return true;


            case ScreenGroup.Popup:
                return false;


            default:
                return true;
        }
    }
}