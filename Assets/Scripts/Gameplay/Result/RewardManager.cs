using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * REWARD SYSTEM
 *
 * MỤC ĐÍCH:
 * - Quản lý kết quả reward sau khi game kết thúc.
 * - Không tự cộng Gold, EXP, Item...
 * - Không phụ thuộc Inventory, Currency, Backend.
 *
 * ------------------------------------------------------------
 * CÁCH DÙNG HIỆN TẠI (LOCAL)
 * ------------------------------------------------------------
 *
 * 1. Tạo RewardResult:
 *
 * RewardResult result =
 *     RewardManager.Instance.CreateResult(GameResult.Win);
 *
 * 2. Thêm reward:
 *
 * result.Add(new RewardData(
 *     RewardType.Currency,
 *     "Gold",
 *     500
 * ));
 *
 * result.Add(new RewardData(
 *     RewardType.Experience,
 *     "EXP",
 *     100
 * ));
 *
 * 3. Submit:
 *
 * RewardManager.Instance.Submit(result);
 *
 *
 * ------------------------------------------------------------
 * TRƯỜNG HỢP THUA KHÔNG CÓ REWARD
 * ------------------------------------------------------------
 *
 * RewardResult result =
 *     RewardManager.Instance.CreateResult(GameResult.Lose);
 *
 * RewardManager.Instance.Submit(result);
 *
 * result.HasReward == false
 *
 * Game có thể tự xử lý:
 *
 * if (!result.HasReward)
 *     HandleLose();
 *
 *
 * ------------------------------------------------------------
 * NHẬN REWARD Ở GAME
 * ------------------------------------------------------------
 *
 * RewardManager.Instance.OnReward += OnReward;
 *
 * private void OnReward(RewardResult result)
 * {
 *     if (!result.HasReward)
 *     {
 *         HandleLose();
 *         return;
 *     }
 *
 *     foreach (RewardData reward in result.Rewards)
 *     {
 *         // Game tự xử lý reward.
 *     }
 * }
 *
 *
 * ------------------------------------------------------------
 * SAU NÀY CHUYỂN SANG BACKEND
 * ------------------------------------------------------------
 *
 * KHÔNG sửa RewardData / RewardResult để gọi API.
 *
 * Flow hiện tại:
 *
 * Game
 *   ↓
 * RewardManager
 *   ↓
 * RewardResult
 *   ↓
 * Local xử lý
 *
 * Flow sau này:
 *
 * Game
 *   ↓
 * Backend API
 *   ↓
 * Backend xác nhận kết quả
 *   ↓
 * Backend trả RewardData
 *   ↓
 * RewardManager
 *   ↓
 * Game xử lý
 *
 * Ví dụ:
 *
 * Win
 *   ↓
 * POST /game/result
 *   ↓
 * BE kiểm tra kết quả
 *   ↓
 * BE tính reward
 *   ↓
 * RewardResult
 *
 * Lúc đó chỉ thay phần tạo/nhận RewardResult
 * bằng Backend Service.
 *
 * RewardManager vẫn giữ vai trò:
 * - Nhận RewardResult
 * - Phát OnReward
 *
 * KHÔNG để RewardManager:
 * - Gọi API
 * - Cộng Gold
 * - Thêm Item
 * - Save Data
 * - Tính reward
 *
 * Backend là nơi quyết định reward khi game online.
 */

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    public event Action<RewardResult> OnReward;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public RewardResult CreateResult(GameResult result)
    {
        return new RewardResult(result);
    }
    public void Submit(RewardResult result)
    {
        if (result == null)
            return;

        OnReward?.Invoke(result);
    }
    public void Clear()
    {
        OnReward = null;
    }
}
