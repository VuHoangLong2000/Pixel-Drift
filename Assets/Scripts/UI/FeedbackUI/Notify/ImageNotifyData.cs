using System;
using UnityEngine;

[Serializable]
public class ImageNotifyData
{
    public Sprite image;
    public float duration;
    public float blinkInterval;
    public int priority;

    public ImageNotifyData()
    {

    }

    public ImageNotifyData(Sprite image, float duration = 2f, float blinkInterval = 0.25f, int priority = 0)
    {
        this.image = image;
        this.duration = duration;
        this.blinkInterval = blinkInterval;
        this.priority = priority;
    }
}
