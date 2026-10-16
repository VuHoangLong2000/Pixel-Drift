using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardResult
{
    public GameResult Result { get; }

    public IReadOnlyList<RewardData> Rewards => _rewards;
    private readonly List<RewardData> _rewards;

    public bool HasReward  => _rewards.Count > 0;

    public RewardResult(GameResult result)
    {
        Result = result;
    }
    public void Add(RewardData reward)
    {
        if (reward == null)
            return;

        _rewards.Add(reward);
    }
}
