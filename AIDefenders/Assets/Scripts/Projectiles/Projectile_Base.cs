using UnityEngine;

public abstract class Projectile_Base : MonoBehaviour
{
    public float damage;

    protected Vector3 velocity;
    protected float gravity;
    protected float lifetime = 5f;
    protected Transform ignoreRoot;
    protected bool spent;
    protected bool armed;

    float age;

    protected virtual void Awake()
    {
        ConfigureColliders(false);
    }

    protected virtual void Start()
    {
        int bullet = LayerMask.NameToLayer("Bullet");
        int turret = LayerMask.NameToLayer("Turret");
        if (bullet >= 0 && turret >= 0)
            Physics.IgnoreLayerCollision(bullet, turret);
    }

    public void Launch(
        Vector3 initialVelocity,
        float gravityValue,
        float life,
        float damageValue,
        Transform ignore = null)
    {
        velocity = initialVelocity;
        gravity = gravityValue;
        lifetime = life > 0f ? life : 5f;
        damage = damageValue;
        ignoreRoot = ignore;
        age = 0f;
        spent = false;
        armed = true;

        PrepareBody();
        IgnoreCasterColliders();
        ConfigureColliders(true);
        OnLaunched();
    }

    protected virtual void OnLaunched()
    {
    }

    protected virtual void Update()
    {
        if (!armed || spent)
            return;

        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Expire();
            return;
        }

        TickHoming(Time.deltaTime);
        TickMove(Time.deltaTime);
    }

    protected virtual void TickHoming(float deltaTime)
    {
    }

    protected virtual void TickMove(float deltaTime)
    {
        velocity += Vector3.down * gravity * deltaTime;
        transform.position += velocity * deltaTime;

        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity);
    }

    void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
            TryHit(collision.collider);
    }

    void TryHit(Collider other)
    {
        if (!armed || spent || other == null)
            return;

        if (ShouldIgnore(other))
            return;

        Target_Base target = other.GetComponentInParent<Target_Base>();
        if (target != null && !target.isDead && !target.isSample)
        {
            Vector3 point = other.ClosestPoint(transform.position);
            OnHitTarget(target, point);
            Consume();
            return;
        }

        if (other.isTrigger)
            return;

        OnHitWorld(other.ClosestPoint(transform.position));
        Consume();
    }

    protected virtual bool ShouldIgnore(Collider other)
    {
        Transform root = other.transform;
        if (ignoreRoot != null && (root == ignoreRoot || root.IsChildOf(ignoreRoot)))
            return true;

        if (other.GetComponentInParent<Projectile_Base>() != null)
            return true;
        if (other.GetComponentInParent<CCTVDroneController>() != null)
            return true;
        if (other.GetComponentInParent<CCTVController>() != null)
            return true;
        if (other.GetComponentInParent<CCTVMoveLimit>() != null)
            return true;
        if (other.GetComponentInParent<TargetGoal>() != null)
            return true;
        if (other.GetComponentInParent<TurretController>() != null)
            return true;
        if (other.GetComponentInParent<TurretPlatform>() != null)
            return true;

        return false;
    }

    protected virtual void OnHitTarget(Target_Base target, Vector3 point)
    {
        if (damage > 0.01f && target != null)
            target.TakeDamage(damage);
    }

    protected virtual void OnHitWorld(Vector3 point)
    {
    }

    protected virtual void OnExpired()
    {
    }

    void Expire()
    {
        if (spent)
            return;

        OnExpired();
        Consume();
    }

    protected void Consume()
    {
        if (spent)
            return;

        spent = true;
        armed = false;
        Destroy(gameObject);
    }

    void PrepareBody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void IgnoreCasterColliders()
    {
        if (ignoreRoot == null)
            return;

        Collider[] mine = GetComponents<Collider>();
        Collider[] theirs = ignoreRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < mine.Length; i++)
        {
            if (mine[i] == null)
                continue;
            for (int j = 0; j < theirs.Length; j++)
            {
                if (theirs[j] != null)
                    Physics.IgnoreCollision(mine[i], theirs[j], true);
            }
        }
    }

    protected virtual void ConfigureColliders(bool enable)
    {
        Collider[] colliders = GetComponents<Collider>();
        int keep = -1;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
                continue;

            colliders[i].isTrigger = true;
            if (keep < 0)
            {
                keep = i;
                colliders[i].enabled = enable;
            }
            else
            {
                colliders[i].enabled = false;
            }
        }
    }
}
