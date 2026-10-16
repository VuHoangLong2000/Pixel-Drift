using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool : IPool
{
    private readonly GameObject _prefab;
    private readonly Transform _parent;

    private readonly Queue<PooledObject> _objects = new();

    private readonly int _maxSize;

    private int _activeCount;

    public int Count => _objects.Count;

    public Pool(GameObject prefab, Transform parent, int maxSize = 50)
    {
        _prefab = prefab;
        _parent = parent;
        _maxSize = maxSize;
    }
    public PooledObject Get()
    {
        PooledObject obj;

        if (_objects.Count > 0)
        {
            obj = _objects.Dequeue();
        }
        else
        {
            obj = Create();
        }

        _activeCount++;
        obj.OnGet();
        return obj;
    }
    private PooledObject Create()
    {
        GameObject go = Object.Instantiate(_prefab, _parent);

        go.name = _prefab.name;

        PooledObject pooled = go.GetComponent<PooledObject>();

        if (pooled == null)
            pooled = go.AddComponent<PooledObject>();

        pooled.Initalize(this);

        return pooled;
    }
    public void Prewarm(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (_objects.Count >= _maxSize)
                break;

            PooledObject obj = Create();

            obj.OnRelease();

            _objects.Enqueue(obj);
        }
    }
    public void Release(PooledObject obj)
    {
        if (obj == null)
            return;

        _activeCount--;

        obj.OnRelease();

        if (_objects.Count >= _maxSize)
        {
            Object.Destroy(obj.gameObject);
            return;
        }

        _objects.Enqueue(obj);
    }
    public void Clear()
    {
        foreach (var obj in _objects)
        {
            if (obj != null)
                Object.Destroy(obj.gameObject);
        }

        _objects.Clear();
    }

    public int ActiveCount()
    {
        return _activeCount;
    }
}
