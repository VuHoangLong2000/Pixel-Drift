#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

/*
 * ICON BUILDER
 *
 * Dùng trong Unity Editor để generate IconAddress.
 *
 * Menu:
 * BaseGame → UI → Build Icon Address
 *
 * Source:
 * IconSO
 *
 * Output:
 * Assets/Data/UI/IconAddress.asset
 *
 * Không chỉnh IconAddress thủ công.
 * Khi IconSO thay đổi → Build lại.
 */
public static class IconBuilder
{
    private const string OutputPath = "Assets/Data/UI/IconAddress.asset";

    [MenuItem("BaseGame/UI/Build Icon Address")]
    public static void Build()
    {
        string[] guids = AssetDatabase.FindAssets("t:IconSO");

        if (guids.Length == 0)
        {
            Debug.LogError("No IconSO found.");
            return;
        }

        if (guids.Length > 1)
        {
            Debug.LogWarning(
                $"Found {guids.Length} IconSO assets. " +
                "Using the first one."
            );
        }

        string sourcePath =
            AssetDatabase.GUIDToAssetPath(guids[0]);

        IconSO source =
            AssetDatabase.LoadAssetAtPath<IconSO>(
                sourcePath
            );

        if (source == null)
        {
            Debug.LogError("Failed to load IconSO.");
            return;
        }

        AddressableAssetSettings settings =
            AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
        {
            Debug.LogError(
                "Addressable Asset Settings not found."
            );

            return;
        }

        List<IconAddressEntry> entries = new();
        HashSet<string> ids = new();

        foreach (IconEntry icon in source.icons)
        {
            if (icon == null)
                continue;

            if (string.IsNullOrWhiteSpace(icon.id))
            {
                Debug.LogError(
                    "Icon contains empty ID."
                );

                continue;
            }

            if (!ids.Add(icon.id))
            {
                Debug.LogError(
                    $"Duplicate Icon ID: [{icon.id}]"
                );

                continue;
            }

            if (icon.reference == null ||
                !icon.reference.RuntimeKeyIsValid())
            {
                Debug.LogError(
                    $"Icon [{icon.id}] has invalid reference."
                );

                continue;
            }

            string assetGuid =
                icon.reference.AssetGUID;

            AddressableAssetEntry addressableEntry =
                settings.FindAssetEntry(assetGuid);

            if (addressableEntry == null)
            {
                Debug.LogError(
                    $"Icon [{icon.id}] is not Addressable."
                );

                continue;
            }

            string address =
                addressableEntry.address;

            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogError(
                    $"Icon [{icon.id}] has empty Addressables address."
                );

                continue;
            }

            entries.Add(
                new IconAddressEntry(
                    icon.id,
                    address
                )
            );
        }

        IconAddress database =
            AssetDatabase.LoadAssetAtPath<IconAddress>(
                OutputPath
            );

        if (database == null)
        {
            database =
                ScriptableObject.CreateInstance<IconAddress>();

            AssetDatabase.CreateAsset(
                database,
                OutputPath
            );
        }

        database.icons.Clear();
        database.icons.AddRange(entries);

        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Icon Address built successfully. " +
            $"Total: {entries.Count}"
        );
    }
}

#endif