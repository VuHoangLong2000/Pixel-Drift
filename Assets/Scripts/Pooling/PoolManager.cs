using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private Transform poolRoot;

    [SerializeField] private int defaultMaxSize = 50;

    private readonly Dictionary<GameObject, Pool> _pools = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (poolRoot == null)
        {
            GameObject root = new GameObject("Pool Root");
            poolRoot = root.transform;
        }

        DontDestroyOnLoad(gameObject);
    }

    public T Get<T>(T prefab, Transform parent = null) where T : Component
    {
        PooledObject obj = Get(prefab.gameObject, parent);

        return obj.GetComponent<T>();
    }

    public PooledObject Get(GameObject prefab, Transform parent = null)
    {
        Pool pool = GetOrCreatePool(prefab);

        PooledObject obj = pool.Get();

        if (parent != null)
        {
            obj.transform.SetParent(parent, false);
        }

        return obj;
    }

    private Pool GetOrCreatePool(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out Pool pool))
            return pool;

        Transform parent = new GameObject(prefab.name + "_Pool") .transform;

        parent.SetParent(poolRoot);

        pool = new Pool(prefab, parent, defaultMaxSize);

        _pools.Add(prefab, pool);

        return pool;
    }

    public void Release(Component obj)
    {
        if (obj == null)
            return;

        Release(obj.gameObject);
    }
    public void Release(GameObject obj)
    {
        if (obj == null)
            return;

        PooledObject pooled = obj.GetComponent<PooledObject>();

        if (pooled == null)
        {
            Destroy(obj);
            return;
        }

        pooled.Release();
    }

    public void Prewarm(GameObject prefab, int amount)
    {
        Pool pool = GetOrCreatePool(prefab);

        pool.Prewarm(amount);
    }

    public void Clear()
    {
        foreach (var pool in _pools.Values)
        {
            pool.Clear();
        }

        _pools.Clear();
    }

    public void Clear(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out Pool pool))
        {
            pool.Clear();
            _pools.Remove(prefab);
        }
    }
}