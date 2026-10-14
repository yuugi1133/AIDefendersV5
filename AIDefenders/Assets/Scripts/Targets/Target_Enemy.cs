using UnityEngine;
using UnityEngine.Serialization;

public class Target_Enemy : Target_Base
{
    [Header("Enemy Reward")]
    [FormerlySerializedAs("killReward")]
    public int killFunds = 10;
    public int killScore = 10;

    [Header("Base")]
    public float baseDamage = 1f;

    protected override void OnKilled(WaveDirector director)
    {
        if (director == null)
            return;

        director.AddFunds(killFunds);
        director.AddScore(killScore);
    }

    protected override void OnReachedGoal(WaveDirector director)
    {
        if (director == null)
            return;

        director.DamageBase(baseDamage);
    }
}
