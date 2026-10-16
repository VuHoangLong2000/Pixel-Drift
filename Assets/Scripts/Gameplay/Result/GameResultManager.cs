using System;
using UnityEngine;

public enum GameResult
{
    None,
    Win,
    Lose,
    Draw
}

public class GameResultManager : MonoBehaviour
{
    public static GameResultManager Instance { get; private set; }

    public GameResult Result { get; private set; } = GameResult.None;
    public bool IsFinished => Result != GameResult.None;

    public event Action<GameResult> OnResult;

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
    public void Win()
    {
        SetResult(GameResult.Win);
    }
    public void Lose()
    {
        SetResult(GameResult.Lose);
    }
    public void Draw()
    {
        SetResult(GameResult.Draw);
    }
    public void SetResult(GameResult result)
    {
        if (IsFinished || result == GameResult.None)
            return;

        Result = result;
        OnResult?.Invoke(Result);
    }
    public void Reset()
    {
        Result = GameResult.None;
    }
}
