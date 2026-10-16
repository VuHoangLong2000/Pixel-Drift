using System.IO;
using UnityEngine;

public class JsonSaveProvider : ISaveProvider
{
    private readonly string _path;
    public JsonSaveProvider(string fileName = "save.json")
    {
        _path = Path.Combine(Application.persistentDataPath, fileName);
    }

    public SaveContainer Load()
    {
        if (!File.Exists(_path))
            return new SaveContainer();

        string json = File.ReadAllText(_path);

        if (string.IsNullOrEmpty(json))
            return new SaveContainer();

        return JsonUtility.FromJson<SaveContainer>(json);
    }

    public void Save(SaveContainer container)
    {
        string json = JsonUtility.ToJson(container, true);

        File.WriteAllText(_path, json);
    }

    public bool Exists()
    {
        return File.Exists(_path);
    }


    public void Delete()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }
}