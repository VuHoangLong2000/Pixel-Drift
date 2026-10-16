using System;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private ISaveProvider _provider;

    private SaveContainer _container;

    private readonly Dictionary<string, object> _cache = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        _provider = new JsonSaveProvider();

        Load();
    }



    public void Load()
    {
        _container = _provider.Load();

        foreach (var entry in _container.entries)
        {
            _cache[entry.key] = entry.json;
        }
    }
    public T Get<T>() where T : class, ISaveData, new()
    {
        T data = new T();

        if (_cache.TryGetValue(data.Key,  out object json))
        {
            return JsonUtility.FromJson<T>(
                json.ToString()
            );
        }

        return data;
    }

    public void Set<T>(T data) where T : class, ISaveData
    {
        string json = JsonUtility.ToJson(data);

        _cache[data.Key] = json;
    }

    public void Save()
    {
        _container.entries.Clear();

        foreach (var item in _cache)
        {
            _container.entries.Add(
                new SaveEntry()
                {
                    key = item.Key,
                    json = item.Value.ToString()
                }
            );
        }

        _provider.Save(_container);
    }
    public void Delete()
    {
        _provider.Delete();

        _cache.Clear();

        _container = new SaveContainer();
    }
}