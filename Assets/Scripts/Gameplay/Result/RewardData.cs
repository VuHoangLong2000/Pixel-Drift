using System;
using UnityEngine;
/*
 * RewardData
 *
 * Đại diện cho 1 phần thưởng.
 *
 * type   : loại reward
 * id     : ID của reward (Gold, ItemID, EXP...)
 * amount : số lượng
 *
 * Constructor rỗng:
 * - Dùng khi cần tạo object rồi gán dữ liệu sau.
 * - Hữu ích cho deserialize / dữ liệu từ Backend.
 *
 * Constructor đầy đủ:
 *
 * new RewardData(
 *     RewardType.Currency,
 *     "Gold",
 *     500
 * );
 *
 * Lưu ý:
 * RewardData chỉ chứa DATA.
 * Không tự cộng reward và không gọi hệ thống khác.
 */

public enum RewardType
{
    None,
    Currency,
    Experience,
    Item,
    Character,
    Equipment,
    Custom,
}

[Serializable]
public class RewardData
{
    public RewardType type;
    public string id;
    public int amount;

    public RewardData(RewardType type, string id, int amount)
    {
        this.type = type;
        this.id = id;
        this.amount = amount;
    }
    public RewardData()
    {

    }
}
