using UnityEngine;

public class StatusEffect_Burn : StatusEffect
{
    public const string Id = "laser_burn";

    public float tickDamage = 50f;
    public float tickInterval = 1f;
    public TurretController source;

    float tickTimer;

    public StatusEffect_Burn(float durationSeconds, float damagePerTick, TurretController attacker)
    {
        effectId = Id;
        duration = durationSeconds;
        tickDamage = damagePerTick;
        source = attacker;
        allowMultiple = false;
        refreshDuration = true;
        tickTimer = 0f;
    }

    protected override void OnRefreshed(StatusEffect sourceEffect)
    {
        StatusEffect_Burn burn = sourceEffect as StatusEffect_Burn;
        if (burn != null)
        {
            tickDamage = burn.tickDamage;
            if (burn.source != null)
                source = burn.source;
        }

        tickTimer = 0f;
    }

    protected override void OnTick(Target_Base target, float deltaTime)
    {
        if (target == null || tickInterval <= 0.01f)
            return;

        tickTimer += deltaTime;
        while (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            target.TakeDamage(0f, 3f, Mathf.RoundToInt(tickDamage), source, true);
            if (target.isDead)
                return;
        }
    }
}
