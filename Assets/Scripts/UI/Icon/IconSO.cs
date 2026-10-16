using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName ="IconDatabase", menuName ="BaseGame/UI/IconSO")]
public class IconSO : ScriptableObject
{
    public List<IconEntry> icons = new();
}
[Serializable]
public class IconEntry
{
    public string id;
    public AssetReferenceSprite reference;
}
