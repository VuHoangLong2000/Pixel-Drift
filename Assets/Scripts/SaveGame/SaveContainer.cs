using System;
using System.Collections.Generic;

[Serializable]
public class SaveContainer
{
    public List<SaveEntry> entries = new();
}

[Serializable]
public class SaveEntry
{
    public string key;
    public string json;
}