using System;

[Serializable]
public class LocalizationSaveData : ISaveData
{
    public string Key => "Localization";

    public Language language = Language.Vietnamese;
}