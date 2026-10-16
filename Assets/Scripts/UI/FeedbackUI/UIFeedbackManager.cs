using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFeedbackManager : MonoBehaviour
{
    public static UIFeedbackManager Instance {  get; private set; }
    public event Action<UIFeedbackRequest> OnFeedback;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void Show(UIFeedbackType type, string message, float duration = 2f, int priority = 0)
    {
        UIFeedbackRequest request = new(type, message, duration, priority);
        Submit(request);
    }

    public void Submit(UIFeedbackRequest request)
    {
        if (request == null)
            return;

        OnFeedback?.Invoke(request);
    }

    public void ShowInfo(string message, float duration = 2f)
    {
        Show(UIFeedbackType.Info, message, duration);
    }

    public void ShowSuccess(string message, float duration = 2f)
    {
        Show(UIFeedbackType.Success, message, duration);
    }

    public void ShowWarning(string message, float duration = 2f)
    {
        Show(UIFeedbackType.Warning, message, duration);
    }

    public void ShowError(string message, float duration = 2f)
    {
        Show(UIFeedbackType.Error, message, duration);
    }
}
