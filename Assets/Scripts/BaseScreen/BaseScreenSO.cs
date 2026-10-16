using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName ="BaseScreen", menuName ="BaseScreenSO/BaseScreen")]
public class BaseScreenSO : ScriptableObject
{
    public List<ScreenData> screens = new();
}
[Serializable]
public class ScreenData
{
    public string ScreenID;
    public string displayName;
    public ScreenGroup group;
    public AssetReferenceGameObject prefabReference;
    public bool keepActive = true;
}
public enum ScreenGroup
{
    Core,
    Feature,
    Popup,
    Overlay
}