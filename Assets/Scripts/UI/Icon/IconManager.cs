using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/*
 * ICON SYSTEM
 *
 * Flow:
 * IconSO
 *     ↓
 * IconBuilder
 *     ↓
 * IconAddress
 *     ↓
 * IconManager
 *     ↓
 * Addressables
 *     ↓
 * Sprite
 *
 * Cách dùng:
 *
 * Sprite icon =
 *     await IconManager.Instance.Load("SKILL_FIRE");
 *
 * Không gán Sprite trực tiếp vào gameplay/UI.
 * Chỉ sử dụng Icon ID.
 *
 * IconSO:
 * - Nơi khai báo ID + AssetReferenceSprite.
 * - Đây là source data.
 *
 * IconBuilder:
 * - Unity Editor tool.
 * - Build IconSO thành IconAddress.
 * - Menu:
 *   BaseGame → UI → Build Icon Address
 *
 * IconAddress:
 * - Database được generate từ IconSO.
 * - Không chỉnh thủ công.
 *
 * IconManager:
 * - API runtime để load icon.
 * - Có cache.
 * - Có Release() và ReleaseAll().
 *
 * Khi thêm icon:
 * 1. Đặt Sprite thành Addressable.
 * 2. Thêm ID + Reference vào IconSO.
 * 3. Build Icon Address.
 * 4. Code chỉ cần gọi IconManager bằng ID.
 *
 * Ví dụ:
 * await IconManager.Instance.Load("ITEM_SWORD");
 *
 * Sau này nếu icon/data đến từ BE:
 * BE chỉ cần trả về Icon ID.
 * Game vẫn resolve icon thông qua IconManager.
 */
public class IconManager : MonoBehaviour
{
    public static IconManager Instance { get; private set; }
    [SerializeField] private IconAddress data;

    private readonly Dictionary<string, string> _addresses = new();
    private readonly Dictionary<string, Sprite> _cache = new();
    private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _handles = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookup();
    }

    private void BuildLookup()
    {
        _addresses.Clear();

        if (data == null)
            return;

        foreach (IconAddressEntry entry in data.icons)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id) || string.IsNullOrEmpty(entry.address))
                continue;

            if (_addresses.ContainsKey(entry.id))
                continue;

            _addresses.Add(entry.id, entry.address);
        }
    }

    public async Task<Sprite> Load(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        if (_cache.TryGetValue(id, out Sprite cached))
            return cached;
        if (!_addresses.TryGetValue(id, out string address))
            return null;

        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);

        await handle.Task;

        if(handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load Icon [{id}].");

            Addressables.Release(handle);
            return null;
        }
        Sprite sprite = handle.Result;

        _cache[id] = sprite;
        _handles[id] = handle;

        return sprite;
    }
    public bool Contains(string id)
    {
        return _addresses.ContainsKey(id);
    }

    public bool IsLoaded(string id)
    {
        return _cache.ContainsKey(id);
    }

    public Sprite GetCached(string id)
    {
        _cache.TryGetValue(id, out Sprite sprite);
        return sprite;
    }
    public void Release(string id)
    {
        if (!_handles.TryGetValue(id, out AsyncOperationHandle<Sprite> handle))
        {
            return;
        }

        Addressables.Release(handle);

        _handles.Remove(id);
        _cache.Remove(id);
    }

    public void ReleaseAll()
    {
        foreach (AsyncOperationHandle<Sprite> handle in _handles.Values)
            Addressables.Release(handle);

        _handles.Clear();
        _cache.Clear();
    }

    private void OnDestroy()
    {
        ReleaseAll();

        _addresses.Clear();

        if (Instance == this)
            Instance = null;
    }
}
