using UnityEngine;

public class Bullet : Projectile_Base
{
    AmmoData ammo;
    Transform homingTarget;
    float homingTurnRate;

    public void Init(
        Vector3 initialVelocity,
        float gravityValue,
        float initialDamage,
        float life = 5f,
        Transform target = null,
        float turnRate = 0f,
        AmmoData ammoData = null)
    {
        ammo = ammoData;
        homingTarget = target;
        homingTurnRate = turnRate;
        Launch(initialVelocity, gravityValue, life, initialDamage);
    }

    protected override void TickHoming(float deltaTime)
    {
        if (homingTarget == null || homingTurnRate <= 0f)
            return;

        Vector3 aim = homingTarget.position;
        Target_Base body = homingTarget.GetComponentInParent<Target_Base>();
        if (body != null)
            aim = body.GetAimPoint(transform.position);

        Vector3 toTarget = (aim - transform.position).normalized;
        float speed = velocity.magnitude;
        if (speed <= 0.01f)
            return;

        Vector3 newDir = Vector3.RotateTowards(
            velocity.normalized,
            toTarget,
            homingTurnRate * Mathf.Deg2Rad * deltaTime,
            0f
        );
        velocity = newDir * speed;
    }

    protected override void OnHitTarget(Target_Base target, Vector3 point)
    {
        base.OnHitTarget(target, point);
        if (ammo != null)
        {
            ammo.ApplyExplosion(point, target);
            CombatVfx.PlayImpact(ammo.impactEffect, point, Vector3.up);
        }
    }

    protected override void OnHitWorld(Vector3 point)
    {
        if (ammo == null)
            return;
        ammo.ApplyExplosion(point, null);
        CombatVfx.PlayImpact(ammo.impactEffect, point, Vector3.up);
    }

    protected override void OnExpired()
    {
        if (ammo == null)
            return;
        ammo.ApplyExplosion(transform.position, null);
        if (ammo.HasExplosion)
            CombatVfx.PlayImpact(ammo.impactEffect, transform.position, Vector3.up);
    }
}
