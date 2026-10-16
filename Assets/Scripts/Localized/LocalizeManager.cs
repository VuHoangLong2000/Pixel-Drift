using System;
using System.Collections.Generic;
using System.Globalization;
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLanguage();
    }

    public void Initialize(Language language, Dictionary<string, string> table)
    {
        if (table == null)
            return;

        _tables[language] = table;

        if (CurrentLanguage == language)
            _table = table;
    }

    // Lấy bản dịch chưa có tham số.
    public string GetLocalize(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (_table.TryGetValue(key, out string value))
            return value;

        if (_tables.TryGetValue(DefaultLanguage, out var fallbackTable) &&
            fallbackTable.TryGetValue(key, out value))
        {
            return value;
        }

        Debug.LogWarning($"[Localize] Missing key: {key}");
        return key;
    }

    // Lấy bản dịch có tham số, ví dụ: "Thu thập {0} vật phẩm".
    public string GetLocalize(string key, params object[] args)
    {
        string localizedText = GetLocalize(key);

        if (args == null || args.Length == 0)
            return localizedText;

        try
        {
            return string.Format(CultureInfo.InvariantCulture, localizedText, args);
        }
        catch (FormatException e)
        {
            Debug.LogError($"[Localize] Invalid format for key '{key}': {e.Message}");

            return localizedText;
        }
    }

    public void ChangeLanguage(Language language)
    {
        if (CurrentLanguage == language)
            return;

        if (!_tables.TryGetValue(language, out var table))
        {
            Debug.LogWarning($"[Localize] Language table not initialized: {language}");
            return;
        }

        CurrentLanguage = language;
        _table = table;

        PlayerPrefs.SetInt("Language", (int)language);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke();
    }

    public bool HasKey(string key)
    {
        return _table.ContainsKey(key);
    }

    private void LoadLanguage()
    {
        int savedLanguage = PlayerPrefs.GetInt("Language", (int)Language.Vietnamese);

        if (Enum.IsDefined(typeof(Language), savedLanguage))
            CurrentLanguage = (Language)savedLanguage;
        else
            CurrentLanguage = DefaultLanguage;
    }
}
