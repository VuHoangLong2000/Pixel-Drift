using System.Collections.Generic;
using UnityEngine;

public static class ShooterListGenerator
{
    private static readonly int[] AmmoOptions = { 10, 20, 30 };

    public static List<ShooterSpawnData> Generate(LevelData levelData)
    {
        List<ShooterSpawnData> result = new();

        if (levelData == null)
        {
            Debug.LogError("[ShooterListGenerator] LevelData is null.");
            return result;
        }

        Dictionary<string, int> blocksByColor = new();

        foreach (PixelBlockData block in levelData.blocks)
        {
            if (block == null || block.isIndestructible)
                continue;

            if (string.IsNullOrWhiteSpace(block.colorId))
            {
                Debug.LogWarning("[ShooterListGenerator] Block has an empty colorId.", levelData);
                continue;
            }

            if (!blocksByColor.ContainsKey(block.colorId))
                blocksByColor[block.colorId] = 0;

            blocksByColor[block.colorId]++;
        }

        // Sort color IDs so generation starts from a predictable order.
        List<string> colorIds = new(blocksByColor.Keys);
        colorIds.Sort(System.StringComparer.Ordinal);

        foreach (string colorId in colorIds)
        {
            int blockCount = blocksByColor[colorId];

            // Every shooter has ammo in multiples of 10. Round up so total ammo is sufficient.
            int ammoUnitsNeeded = Mathf.CeilToInt(blockCount / 10f);

            Dictionary<int, int> quantityByAmmo = new()
            {
                { 10, 0 },
                { 20, 0 },
                { 30, 0 }
            };

            // Randomly partition the required units into groups of 1, 2 or 3. Each group corresponds to one shooter with 10, 20 or 30 ammo.
            while (ammoUnitsNeeded > 0)
            {
                int maxUnits = Mathf.Min(3, ammoUnitsNeeded);
                int chosenUnits = Random.Range(1, maxUnits + 1);
                int ammo = chosenUnits * 10;

                quantityByAmmo[ammo]++;
                ammoUnitsNeeded -= chosenUnits;
            }

            foreach (int ammo in AmmoOptions)
            {
                int quantity = quantityByAmmo[ammo];

                if (quantity <= 0)
                    continue;

                result.Add(new ShooterSpawnData
                {
                    colorId = colorId,
                    quantity = quantity,
                    ammo = ammo,
                    spawnOrder = 0
                });
            }
        }

        // Shuffle groups so their appearance order is randomized.
        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (result[i], result[j]) = (result[j], result[i]);    
        }

        for (int i = 0; i < result.Count; i++)
            result[i].spawnOrder = i;

        return result;
    }
}
