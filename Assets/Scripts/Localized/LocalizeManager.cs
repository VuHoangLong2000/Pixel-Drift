using System;
using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    Vietnamese = 0,
    English = 1,
}

public class LocalizeManager : MonoBehaviour
{
    public static LocalizeManager Instance { get; private set; }

    private Dictionary<string, string> _table = new();
    private Dictionary<Language, Dictionary<string, string>> _tables = new();

    public Language CurrentLanguage { get; private set; }
    private readonly Language DefaultLanguage = Language.Vietnamese;

    public event Action OnLanguageChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLanguage();
    }

    /// <summary>
    /// Google Sheet gọi hàm này sau khi pull data xong
    /// </summary>
    public void Initialize(Language language, Dictionary<string, string> table)
    {
        if (table == null)
            return;

        _tables[language] = table;

        if(CurrentLanguage == language)
            this._table = table;
    }


    /// <summary>
    /// Lấy text theo key
    /// </summary>
    /// //Nơi khác gọi LocalizeManager.Instance.GetLocalize("đưa key vào"); là hoạt động
    public string GetLocalize(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (_table.TryGetValue(key, out var value)) 
            return value;

        if (_tables.TryGetValue(DefaultLanguage, out var fallbackTable))
        {
            if (fallbackTable.TryGetValue(key, out value))
            {
                return value;
            }
        }

        Debug.LogWarning($"[Localize] Missing key: {key}");
        return key;
    }

    /// <summary>
    /// Đổi ngôn ngữ
    /// </summary>
    public void ChangeLanguage(Language language)
    {
        if (CurrentLanguage == language)
            return;

        if (!_tables.ContainsKey(language))
            return;

        CurrentLanguage = language;

        _table = _tables[language];

        PlayerPrefs.SetInt("Language", (int)language);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke();
    }
    /// <summary>
    /// Check key tồn tại
    /// </summary>
    public bool HasKey(string key)
    {
        return _table.ContainsKey(key);
    }
    /// <summary>
    /// Load language đã lưu
    /// </summary>
    private void LoadLanguage()
    {
        CurrentLanguage = (Language)PlayerPrefs.GetInt("Language", (int)Language.Vietnamese);
    }
}