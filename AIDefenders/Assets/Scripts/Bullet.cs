using UnityEngine;

public class Bullet : Projectile_Base
{
    AmmoData ammo;
    Transform homingTarget;
    float homingTurnRate;
    AudioSource flightLoop;
    bool loftAttack;
    float loftAge;
    int loftPhase;

    public void Init(
        Vector3 initialVelocity,
        float gravityValue,
        float initialDamage,
        float life = 5f,
        Transform target = null,
        float turnRate = 0f,
        AmmoData ammoData = null,
        float armorPierceValue = 3f,
        TurretController shooter = null,
        bool loft = false)
    {
        ammo = ammoData;
        homingTarget = target;
        homingTurnRate = turnRate;
        loftAttack = loft;
        loftAge = 0f;
        loftPhase = 0;
        sourceTurret = shooter;
        Launch(initialVelocity, gravityValue, life, initialDamage, null, armorPierceValue);
        StartFlightLoop();
    }

    protected override void TickHoming(float deltaTime)
    {
        if (loftAttack)
        {
            TickLoft(deltaTime);
            return;
        }

        SteerToward(SeekPoint(), deltaTime);
    }

    void TickLoft(float deltaTime)
    {
        loftAge += deltaTime;
        Vector3 targetPos = SeekPoint();
        Vector3 aim;

        if (loftPhase <= 0)
        {
            Vector3 climb = Vector3.up * 10f;
            Vector3 toward = targetPos - transform.position;
            toward.y = 0f;
            aim = transform.position + climb + toward.normalized * 2f;
            if (loftAge >= 0.42f)
                loftPhase = 1;
        }
        else if (loftPhase == 1)
        {
            aim = targetPos + Vector3.up * 8f;
            Vector3 flat = targetPos - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 16f && transform.position.y > targetPos.y + 3.5f)
                loftPhase = 2;
        }
        else
        {
            aim = targetPos;
        }

        SteerToward(aim, deltaTime);
    }

    Vector3 SeekPoint()
    {
        if (homingTarget == null)
            return transform.position + velocity;

        Target_Base body = homingTarget.GetComponentInParent<Target_Base>();
        if (body != null)
            return body.GetAimPoint(transform.position);
        return homingTarget.position;
    }

    void SteerToward(Vector3 aim, float deltaTime)
    {
        if (homingTurnRate <= 0f)
            return;

        Vector3 toTarget = aim - transform.position;
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        float speed = velocity.magnitude;
        if (speed <= 0.01f)
            return;

        Vector3 newDir = Vector3.RotateTowards(
            velocity.normalized,
            toTarget.normalized,
            homingTurnRate * Mathf.Deg2Rad * deltaTime,
            0f
        );
        velocity = newDir * speed;
    }

    protected override void OnHitTarget(Target_Base target, Vector3 point)
    {
        StopFlightLoop();
        base.OnHitTarget(target, point);
        if (ammo != null)
        {
            Target_Base exclude = ammo.splashHitsPrimary ? null : target;
            ammo.ApplyExplosion(point, exclude, armorPierce, sourceTurret);
            PlayBurst(point);
        }
    }

    protected override void OnHitWorld(Vector3 point)
    {
        StopFlightLoop();
        if (ammo == null)
            return;
        ammo.ApplyExplosion(point, null, armorPierce, sourceTurret);
        PlayBurst(point);
    }

    protected override void OnExpired()
    {
        StopFlightLoop();
        if (ammo == null)
            return;
        ammo.ApplyExplosion(transform.position, null, armorPierce, sourceTurret);
        if (ammo.HasExplosion)
            PlayBurst(transform.position);
    }

    void OnDestroy()
    {
        StopFlightLoop();
    }

    void StartFlightLoop()
    {
        StopFlightLoop();
        if (ammo == null)
            return;
        flightLoop = GameSfx.PlayAttachedLoop(ammo.flightLoopSfx, transform);
    }

    void StopFlightLoop()
    {
        GameSfx.StopAttached(flightLoop);
        flightLoop = null;
    }

    void PlayBurst(Vector3 point)
    {
        if (ammo == null || ammo.impactEffect == null)
            return;

        float life = ammo.HasExplosion ? 2.8f : 0.7f;
        GameObject fx = ammo.HasExplosion
            ? CombatVfx.Play(ammo.impactEffect, point, Quaternion.identity, null, life)
            : CombatVfx.Play(ammo.impactEffect, point, Vector3.up, life);
        if (fx != null && ammo.HasExplosion)
        {
            float radius = ammo.ExplosionRadius(sourceTurret);
            fx.transform.localScale = Vector3.one * Mathf.Max(1f, radius / 6f);
        }
    }
}
