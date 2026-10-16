using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance {  get; private set;  }
    public string CurrentState { get; private set; } = GameState.None;
    public string PreviousState { get; private set; } = GameState.None;

    public event Action<string, string> OnStateChanged;

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

    public bool SetState(string newState)
    {
        if (string.IsNullOrWhiteSpace(newState))
            return false;
        if (CurrentState == newState)
            return false;

        PreviousState = CurrentState;
        CurrentState = newState;

        OnStateChanged?.Invoke(PreviousState, CurrentState);

        return true;
    }

    public bool Is(string state)
    {
        return CurrentState == state;
    }

    public bool IsAny(params string[] states)
    {
        if (states == null)
            return false;

        for (int i = 0; i < states.Length; i++)
        {
            if (CurrentState == states[i])
                return true;
        }

        return false;
    }
}
