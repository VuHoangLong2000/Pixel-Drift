using System;

[Serializable]
public class NotifyData
{
    public string message;
    public float duration;
    public int priority;

    public NotifyData()
    {

    }

    public NotifyData(string message, float duration = 2f, int priority = 0)
    {
        this.message = message;
        this.duration = duration;
        this.priority = priority;
    }
}
