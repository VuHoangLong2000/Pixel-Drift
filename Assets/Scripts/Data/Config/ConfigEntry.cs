using UnityEngine;
using System;

[Serializable]
public abstract class ConfigEntry : IConfig
{
    [SerializeField] private string id;
    public string ID => id;
}
