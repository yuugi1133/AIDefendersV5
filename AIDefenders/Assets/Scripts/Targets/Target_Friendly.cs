using UnityEngine;
using UnityEngine.Serialization;

public class Target_Friendly : Target_Base
{
    [Header("Pass Reward")]
    public int passFunds = 15;

    [Header("Friendly Fire")]
    [FormerlySerializedAs("killReward")]
    public int friendlyFireFunds = 10;

    protected override void OnKilled(WaveDirector director)
    {
        if (director == null)
            return;

        director.AddFunds(-friendlyFireFunds);
        director.RegisterTargetOutcome(this, true);
        HudFx.ShowFunds(GetPopupWorld(), -friendlyFireFunds);
    }

    protected override void OnReachedGoal(WaveDirector director)
    {
        if (director == null)
            return;

        int score = ResolveIdentifyScore(false);
        director.AddFunds(passFunds);
        director.AddScore(score);
        director.RegisterTargetOutcome(this, false);
        Vector3 popup = GetPopupWorld();
        HudFx.ShowFunds(popup, passFunds);
        HudFx.ShowScore(popup, score);
    }
}
