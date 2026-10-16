using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PooledObject : MonoBehaviour
{
    private IPool _pool;
    private IPoolable _poolable;

    internal void Initalize(IPool pool)
    {
        this._pool = pool;

        if (!TryGetComponent(out _poolable))
            _poolable = null;
        _poolable?.OnSpawn();
    }
    internal void OnGet()
    {
        gameObject.SetActive(true);
        _poolable?.OnGet();
    }
    internal void OnRelease()
    {
        _poolable?.OnRelease();
        gameObject.SetActive(false);
    }
    public void Release()
    {
        if (_pool == null)
        {
            Destroy(gameObject);
            return;
        }

        _pool.Release(this);
    }
}
