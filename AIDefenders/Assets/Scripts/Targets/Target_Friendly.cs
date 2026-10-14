using UnityEngine;
using UnityEngine.Serialization;

public class Target_Friendly : Target_Base
{
    [Header("Pass Reward")]
    public int passFunds = 15;
    public int passScore = 10;

    [Header("Friendly Fire")]
    [FormerlySerializedAs("killReward")]
    public int friendlyFireFunds = 10;
    public int friendlyFireScore = 0;

    protected override void OnKilled(WaveDirector director)
    {
        if (director == null)
            return;

        director.AddFunds(-friendlyFireFunds);
        director.AddScore(-friendlyFireScore);
    }

    protected override void OnReachedGoal(WaveDirector director)
    {
        if (director == null)
            return;

        director.AddFunds(passFunds);
        director.AddScore(passScore);
    }
}
