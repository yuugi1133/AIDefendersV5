using UnityEngine;
using UnityEngine.Serialization;

public class Target_Enemy : Target_Base
{
    [Header("Enemy Reward")]
    [FormerlySerializedAs("killReward")]
    public int killFunds = 10;

    [Header("Base")]
    public float baseDamage = 1f;

    protected override void OnKilled(WaveDirector director)
    {
        if (director == null)
            return;

        int score = ResolveIdentifyScore(true);
        director.AddFunds(killFunds);
        director.AddScore(score);
        director.RegisterTargetOutcome(this, true);
        Vector3 popup = GetPopupWorld();
        HudFx.ShowFunds(popup, killFunds);
        HudFx.ShowScore(popup, score);
    }

    protected override void OnReachedGoal(WaveDirector director)
    {
        if (director == null)
            return;

        director.RegisterTargetOutcome(this, false);
        director.DamageBase(baseDamage);
    }
}
