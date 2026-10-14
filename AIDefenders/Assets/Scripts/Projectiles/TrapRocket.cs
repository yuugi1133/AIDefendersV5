using UnityEngine;

public class TrapRocket : Projectile_Base
{
    public float homingTurnRate = 220f;
    public float fallGravity = 18f;
    public float fallDrag = 1.6f;
    public float slowDuration = 5f;
    public float slowSpeedScale = 0.25f;

    Transform homingTarget;
    bool hasThrust = true;
    TrailRenderer trail;

    protected override void Awake()
    {
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        base.Awake();
    }

    public void Init(
        Vector3 initialVelocity,
        float life,
        float damageValue,
        Transform target,
        Transform ignore,
        float turnRate,
        float duration,
        float speedScale)
    {
        homingTarget = target;
        homingTurnRate = turnRate;
        slowDuration = duration;
        slowSpeedScale = speedScale;
        hasThrust = target != null;
        Launch(initialVelocity, hasThrust ? 0f : fallGravity, life, damageValue, ignore);
    }

    protected override void OnLaunched()
    {
        EnsureTrail();
        SetTrailEmitting(hasThrust);
        if (!hasThrust)
            DropThrust();
    }

    protected override void TickHoming(float deltaTime)
    {
        if (!hasThrust)
            return;

        if (!IsTargetValid())
        {
            DropThrust();
            return;
        }

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

    protected override void TickMove(float deltaTime)
    {
        if (!hasThrust)
        {
            velocity += Vector3.down * fallGravity * deltaTime;
            float damp = Mathf.Clamp01(fallDrag * deltaTime);
            velocity.x *= 1f - damp;
            velocity.z *= 1f - damp;
        }
        else
        {
            velocity += Vector3.down * gravity * deltaTime;
        }

        transform.position += velocity * deltaTime;
        AlignToVelocity();
    }

    protected override bool ShouldIgnore(Collider other)
    {
        if (base.ShouldIgnore(other))
            return true;

        Target_Base target = other.GetComponentInParent<Target_Base>();
        if (target == null)
            return false;

        if (!hasThrust || !IsTargetValid())
            return true;

        return target.transform != homingTarget && !target.transform.IsChildOf(homingTarget);
    }

    protected override void OnHitTarget(Target_Base target, Vector3 point)
    {
        base.OnHitTarget(target, point);
        if (target != null)
            target.ApplyStatus(new StatusEffect_TrapSlow(slowDuration, slowSpeedScale));
    }

    void DropThrust()
    {
        hasThrust = false;
        homingTarget = null;
        gravity = fallGravity;
        SetTrailEmitting(false);
    }

    bool IsTargetValid()
    {
        if (homingTarget == null)
            return false;

        Target_Base target = homingTarget.GetComponent<Target_Base>();
        if (target == null)
            target = homingTarget.GetComponentInParent<Target_Base>();

        return target != null && !target.isDead;
    }

    void AlignToVelocity()
    {
        if (velocity.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.FromToRotation(Vector3.up, velocity.normalized);
    }

    void EnsureTrail()
    {
        Transform engine = transform.Find("Engine");
        Transform attach = engine != null ? engine : transform;
        trail = attach.GetComponent<TrailRenderer>();
        if (trail != null)
            return;

        trail = attach.gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.28f;
        trail.startWidth = 0.12f;
        trail.endWidth = 0.02f;
        trail.numCapVertices = 4;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0f),
                new GradientColorKey(new Color(0.9f, 0.15f, 0.1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;
    }

    void SetTrailEmitting(bool emitting)
    {
        if (trail != null)
            trail.emitting = emitting;
    }
}
