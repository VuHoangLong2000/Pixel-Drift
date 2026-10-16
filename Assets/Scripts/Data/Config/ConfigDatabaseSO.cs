using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="ConfigDatabase", menuName = "BaseGame/Data/Config Database")]
public class ConfigDatabaseSO : ScriptableObject
{
    [SerializeField] private List<ConfigEntry> _configs = new();

    private Dictionary<string, ConfigEntry> _lookup;
    public IReadOnlyList<ConfigEntry> Configs => _configs;

    public void BuildLookup()
    {
        _lookup = new Dictionary<string, ConfigEntry>();

        foreach (ConfigEntry config in _configs)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.ID))
                continue;
            if (_lookup.ContainsKey(config.ID))
                continue;
            _lookup.Add(config.ID, config);
        }
    }

    public ConfigEntry Get(string id)
    {
        if (_lookup == null)
            BuildLookup();

        _lookup.TryGetValue(id, out ConfigEntry config);
        return config;
    }

    public T Get<T>(string id) where T : ConfigEntry
    {
        ConfigEntry config = Get(id);

        return config as T;
    }
}
