using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    public static ConfigManager Instance { get; private set; }

    [SerializeField] private ConfigDatabaseSO database;

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
            return;

        database.BuildLookup();
    }

    public T Get<T>(string id) where T : ConfigEntry
    {
        if (database == null)
            return null;

        T config = database.Get<T>(id);

        if (config == null)
        {
            Debug.LogError( $"Config [{id}] not found " + $"or type mismatch.");
        }

        return config;
    }

    public bool Contains(string id)
    {
        return database != null && database.Get(id) != null; 
    }
}
