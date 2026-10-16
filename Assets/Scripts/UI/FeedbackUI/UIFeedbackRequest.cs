using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UIFeedbackType
{
    Info,
    Success,
    Warning,
    Error,
    Confirm,
    Notification
}

[Serializable]
public class UIFeedbackRequest
{
    public UIFeedbackType type;
    public string message;
    public float duration;
    public int priority;

    public UIFeedbackRequest()
    {

    }

    public UIFeedbackRequest(UIFeedbackType type, string message, float duration = 2f, int priority = 0)
    {
        this.type = type;
        this.message = message;
        this.duration = duration;
        this.priority = priority;
    }
}
