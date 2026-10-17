using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Pixel Drift/Level Database")]
public class LevelDatabaseSO : ScriptableObject
{
    [SerializeField]
    private List<LevelData> levels = new();

    public IReadOnlyList<LevelData> Levels => levels;

    public LevelData GetLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Count)
        {
            Debug.LogError($"[LevelDatabaseSO] Level index {levelIndex} is out of range.");
            return null;
        }

        return levels[levelIndex];
    }

    public int LevelCount => levels.Count;
}