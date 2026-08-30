using UnityEngine;

public class Bullet : MonoBehaviour
{
    AmmoData ammo;
    Vector3 velocity;
    float gravity;
    float damage;
    float lifetime = 5f;
    Transform homingTarget;
    float homingTurnRate;
    float age;
    bool exploded;

    void Start()
    {
        Physics.IgnoreLayerCollision(
            LayerMask.NameToLayer("Bullet"),
            LayerMask.NameToLayer("Turret")
        );
    }

    public void Init(
        Vector3 initialVelocity,
        float gravityValue,
        float initialDamage,
        float life = 5f,
        Transform target = null,
        float turnRate = 0f,
        AmmoData ammoData = null)
    {
        velocity = initialVelocity;
        gravity = gravityValue;
        damage = initialDamage;
        lifetime = life > 0f ? life : 5f;
        homingTarget = target;
        homingTurnRate = turnRate;
        ammo = ammoData;
        age = 0f;
        exploded = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (Collider col in GetComponents<Collider>())
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Bullet>() != null)
            return;

        Target_Base direct = other.GetComponentInParent<Target_Base>();
        Vector3 point = other.ClosestPoint(transform.position);
        Explode(point, direct);
    }

    void Explode(Vector3 point, Target_Base directHit)
    {
        if (exploded)
            return;

        exploded = true;

        if (directHit != null && !directHit.isDead)
            directHit.TakeDamage(damage);

        if (ammo != null)
            ammo.ApplyExplosion(point, directHit);

        Destroy(gameObject);
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Explode(transform.position, null);
            return;
        }

        if (homingTarget != null && homingTurnRate > 0f)
        {
            Vector3 toTarget = (homingTarget.position - transform.position).normalized;
            float speed = velocity.magnitude;
            if (speed > 0.01f)
            {
                Vector3 newDir = Vector3.RotateTowards(
                    velocity.normalized,
                    toTarget,
                    homingTurnRate * Mathf.Deg2Rad * Time.deltaTime,
                    0f
                );
                velocity = newDir * speed;
            }
        }

        velocity += Vector3.down * gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity);
    }
}
