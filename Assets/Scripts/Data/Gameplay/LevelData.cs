using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PixelBlockData
{
    public Vector2Int position;
    public Color color = Color.red;
    public string colorId;
    public int hitPoints = 1;
    public bool isIndestructible;

    public GameObject customPrefab;
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Pixel Drift/Level Data")]
public class LevelData : ScriptableObject
{
    [Min(1)]
    public int width = 50;

    [Min(1)]
    public int height = 50;

    public List<PixelBlockData> blocks = new List<PixelBlockData>();
}
 