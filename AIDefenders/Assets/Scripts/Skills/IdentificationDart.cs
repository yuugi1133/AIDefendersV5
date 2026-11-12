using UnityEngine;

public class IdentificationDart : Projectile_Base
{
    CCTVController cctv;
    bool asEnemy;
    Target_Base homingTarget;
    float homingTurnRate;

    public void Init(
        Vector3 initialVelocity,
        float gravityValue,
        float life,
        CCTVController controller,
        bool markAsEnemy,
        Transform ignore,
        Target_Base guideTarget = null,
        float turnRate = 0f)
    {
        cctv = controller;
        asEnemy = markAsEnemy;
        homingTarget = guideTarget;
        homingTurnRate = turnRate;
        Launch(initialVelocity, gravityValue, life, 0f, ignore);
    }

    protected override void TickHoming(float deltaTime)
    {
        if (homingTarget == null || homingTarget.isDead || homingTurnRate <= 0.01f)
        {
            homingTarget = null;
            return;
        }

        Vector3 aim = homingTarget.GetAimPoint(transform.position);
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

    protected override void OnLaunched()
    {
        ApplyTint();
    }

    protected override void OnHitTarget(Target_Base target, Vector3 point)
    {
        if (target != null && cctv != null)
            cctv.ForceIdentify(target, asEnemy);
    }

    void ApplyTint()
    {
        Renderer mesh = GetComponent<Renderer>();
        if (mesh == null)
            return;

        Color color = asEnemy
            ? new Color(0.95f, 0.2f, 0.15f, 1f)
            : new Color(0.2f, 0.55f, 1f, 1f);
        mesh.material.color = color;
    }
}
