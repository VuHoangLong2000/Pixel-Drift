using System;

[Serializable]
public class LanguageSaveData : ISaveData
{
    public string Key => "Language";

    public int language = 0;
}