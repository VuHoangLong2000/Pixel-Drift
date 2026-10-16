using UnityEngine;

/*
 * SAVE SYSTEM
 *
 * Chức năng:
 * - Lưu dữ liệu local bằng JSON.
 * - Không phụ thuộc gameplay hoặc Backend.
 * - Có thể mở rộng thêm nhiều loại SaveData.
 *
 * Cách dùng:
 *
 * 1. Lấy dữ liệu:
 *    var data = SaveManager.Instance.Get<AudioSaveData>();
 *
 * 2. Thay đổi:
 *    data.musicVolume = 0.5f;
 *
 * 3. Cập nhật vào SaveManager:
 *    SaveManager.Instance.Set(data);
 *
 * 4. Ghi xuống file:
 *    SaveManager.Instance.Save();
 *
 * Flow:
 *    Get<T>() → Modify → Set(data) → Save()
 *
 * Khi thêm loại dữ liệu mới:
 * - Tạo class implement ISaveData.
 * - Tạo Key riêng.
 * - Không cần sửa SaveManager.
 *
 * Ví dụ:
 *    public class GraphicSaveData : ISaveData
 *    {
 *        public string Key => "Graphic";
 *        public int quality;
 *    }
 *
 * Lưu ý:
 * - SaveManager chỉ quản lý SaveData.
 * - Không đọc/ghi file trực tiếp từ gameplay.
 * - JsonSaveProvider chịu trách nhiệm lưu file.
 */
public class SaveTest : MonoBehaviour
{
    [ContextMenu("Save Test")]
    private void SaveTestData()
    {
        LanguageSaveData language = new()
        {
            language = 1
        };

        AudioSaveData audio = new()
        {
            masterVolume = 0.8f,
            musicVolume = 0.5f,
            sfxVolume = 1f
        };

        SaveManager.Instance.Set(language);
        SaveManager.Instance.Set(audio);
        SaveManager.Instance.Save();

        Debug.Log("Save test completed.");
    }

    [ContextMenu("Load Test")]
    private void LoadTestData()
    {
        LanguageSaveData language =
            SaveManager.Instance.Get<LanguageSaveData>();

        AudioSaveData audio =
            SaveManager.Instance.Get<AudioSaveData>();

        Debug.Log(
            $"Language: {language.language}"
        );

        Debug.Log(
            $"Audio: " +
            $"Master={audio.masterVolume}, " +
            $"Music={audio.musicVolume}, " +
            $"SFX={audio.sfxVolume}"
        );
    }

    [ContextMenu("Delete Save")]
    private void DeleteSave()
    {
        SaveManager.Instance.Delete();

        Debug.Log("Save deleted.");
    }
}