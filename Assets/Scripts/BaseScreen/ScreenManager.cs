using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance { get; private set; }

    [SerializeField] private BaseScreenSO database;

    private readonly Dictionary<string, BaseScreen> cache = new();
    private readonly Dictionary<string, ScreenData> screenInfos = new();
    private readonly HashSet<string> loadingScreens = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (database == null)
        {
            Debug.LogError("ScreenManager database is missing.");
            return;
        }

        foreach (ScreenData screen in database.screens)
        {
            if (screen == null)
                continue;

            if (string.IsNullOrWhiteSpace(screen.ScreenID))
            {
                Debug.LogWarning("Screen contains an empty ScreenID.");
                continue;
            }

            if (screenInfos.ContainsKey(screen.ScreenID))
            {
                Debug.LogWarning($"Duplicate ScreenID [{screen.ScreenID}].");
                continue;
            }

            screenInfos.Add(screen.ScreenID, screen);
        }
    }

    public async void ShowScreen(string screenID)
    {
        if (string.IsNullOrWhiteSpace(screenID))
        {
            Debug.LogError("Screen ID cannot be empty.");
            return;
        }

        // Already loaded → just show.
        if (cache.TryGetValue(screenID, out BaseScreen cachedScreen))
        {
            cachedScreen.Show();
            return;
        }

        // Screen does not exist in database.
        if (!screenInfos.TryGetValue(screenID, out ScreenData data))
        {
            Debug.LogError($"Screen [{screenID}] not found.");
            return;
        }

        // Already loading.
        if (!loadingScreens.Add(screenID))
            return;

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(data.prefabReference);

        await handle.Task;

        // Loading finished regardless of success/failure.
        loadingScreens.Remove(screenID);

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Load Screen [{screenID}] failed.");
            return;
        }

        GameObject instance = handle.Result;

        if (instance == null)
        {
            Debug.LogError($"Screen [{screenID}] returned null instance.");
            return;
        }

        BaseScreen screen = instance.GetComponent<BaseScreen>();

        if (screen == null)
        {
            Debug.LogError($"Screen [{screenID}] doesn't have BaseScreen.");

            Addressables.ReleaseInstance(instance);
            return;
        }

        cache[screenID] = screen;

        screen.Show();
    }

    public void HideScreen(string screenID)
    {
        if (!cache.TryGetValue(screenID, out BaseScreen screen))
            return;

        screen.Hide();
    }

    public void ReleaseScreen(string screenID)
    {
        if (!cache.TryGetValue(screenID, out BaseScreen screen))
            return;

        if (screenInfos.TryGetValue(screenID,  out ScreenData data))
        {
            if (data.keepActive)
            {
                screen.Hide();
                return;
            }
        }

        GameObject instance = screen.gameObject;

        cache.Remove(screenID);

        Addressables.ReleaseInstance(instance);
    }

    public bool IsLoaded(string screenID)
    {
        return cache.ContainsKey(screenID);
    }

    public bool IsLoading(string screenID)
    {
        return loadingScreens.Contains(screenID);
    }

    public bool Exists(string screenID)
    {
        return screenInfos.ContainsKey(screenID);
    }

    public BaseScreen GetScreen(string screenID)
    {
        cache.TryGetValue(screenID, out BaseScreen screen);
        return screen;
    }

    public void ReleaseAll()
    {
        foreach (BaseScreen screen in cache.Values)
        {
            if (screen == null)
                continue;

            Addressables.ReleaseInstance(screen.gameObject);
        }
        cache.Clear();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        ReleaseAll();

        loadingScreens.Clear();
        screenInfos.Clear();

        Instance = null;
    }
}