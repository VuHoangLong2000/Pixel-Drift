using System;

[Serializable]
public class AudioSaveData : ISaveData
{
    public string Key => "Audio";

    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
}