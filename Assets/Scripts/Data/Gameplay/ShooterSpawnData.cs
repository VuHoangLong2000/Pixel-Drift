using System;
using UnityEngine;

[Serializable]
public class ShooterSpawnData
{
    [Min(1)]
    public int quantity = 1;

    [Min(1)]
    public int ammo = 10;

    [Tooltip("ID màu của Shooter, phải khớp với ColorPalette.")]
    public string colorId;

    [Tooltip("Thứ tự xuất hiện trong hàng chờ. Số nhỏ xuất hiện trước.")]
    [Min(0)]
    public int spawnOrder;
}
