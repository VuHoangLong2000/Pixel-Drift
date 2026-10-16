using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IconAddress", menuName = "BaseGame/UI/Icon Address")]
public class IconAddress : ScriptableObject
{
    public List<IconAddressEntry> icons = new();
}

[Serializable]
public class IconAddressEntry
{
    public string id;
    public string address;

    public IconAddressEntry()
    {
    }

    public IconAddressEntry(string id, string address)
    {
        this.id = id;
        this.address = address;
    }
}