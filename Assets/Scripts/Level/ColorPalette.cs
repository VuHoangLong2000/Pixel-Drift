using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PaletteEntry
{
    [Tooltip("ID ổn định, ví dụ red, blue, cyan")]
    public string id;

    public string displayName;
    public Color color = Color.white;
}

[CreateAssetMenu(fileName = "PixelFlowColorPalette", menuName = "Pixel Flow/Color Palette")]
public class ColorPalette : ScriptableObject
{
    public List<PaletteEntry> entries = new List<PaletteEntry>
        {
            new PaletteEntry
            {
                id = "red",
                displayName = "Red",
                color = new Color(0.90f, 0.20f, 0.20f)
            },
            new PaletteEntry
            {
                id = "blue",
                displayName = "Blue",
                color = new Color(0.20f, 0.45f, 0.95f)
            },
            new PaletteEntry
            {
                id = "yellow",
                displayName = "Yellow",
                color = new Color(1.00f, 0.80f, 0.15f)
            },
            new PaletteEntry
            {
                id = "green",
                displayName = "Green",
                color = new Color(0.20f, 0.75f, 0.35f)
            },
            new PaletteEntry
            {
                id = "purple",
                displayName = "Purple",
                color = new Color(0.60f, 0.30f, 0.85f)
            },
            new PaletteEntry
            {
                id = "orange",
                displayName = "Orange",
                color = new Color(1.00f, 0.50f, 0.15f)
            }
        };

    [Header("Indestructible Block Prefabs")]
    public List<GameObject> indestructiblePrefabs = new List<GameObject>();

    public bool TryGetColor(string id, out Color color)
    {
        foreach (PaletteEntry entry in entries)
        {
            if (entry != null && entry.id == id)
            {
                color = entry.color;
                return true;
            }
        }

        color = Color.white;
        return false;
    }
}
