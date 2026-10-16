using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class PlayerDataContainer
{
    private readonly Dictionary<string, IPlayerData> _data = new();

    public void Set<T>(T data) where T : IPlayerData
    {
        if (data == null)
            return;
        _data[data.Key] = data;
    }

    public T Get<T>(string key) where T : class, IPlayerData
    {
        if (!_data.TryGetValue(key, out IPlayerData data))
        {
            return null;
        }
        return data as T;
    }

    public bool Contains(string key)
    {
        return _data.ContainsKey(key);
    }
    public bool Remove(string key)
    {
        return _data.ContainsKey(key);
    }
    public void Clear()
    {
        _data.Clear();
    }

}
