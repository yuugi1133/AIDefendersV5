using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Target_Base : MonoBehaviour
{
    [Header("Stats")]
    public float maxHP = 100f;
    public float currentHP;
    public float moveSpeed = 3f;

    public bool isSample = false;

    [Header("Ground")]
    public LayerMask groundMask;
    public float groundRayHeight = 5f;
    public float groundHeightOffset = 0.05f;
    public float maxGroundDistance = 20f;

    [Header("Death")]
    public bool isDead = false;
    public float sinkSpeed = 1f;
    public float destroyDelay = 3f;

    [Header("Components")]
    public Animator animator;
    public Renderer renderer;
    public Rigidbody rigidbody;

    void Start()
    {
        currentHP = maxHP;

        rigidbody = GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        if (animator && !isSample)
            animator.SetBool("Move", true);

        SnapToGround();
    }

    void Update()
    {}

    protected virtual void FixedUpdate()
    {
        if (isSample) return;
        if (isDead) return;

        Move();
        FollowGround();
    }

    public Renderer getRenderer()
    {
        return renderer;
    }

    void Move()
    {
        Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 velocity = forward * moveSpeed;

        if (TryGetGroundHit(rigidbody.position, out RaycastHit hit))
        {
            Vector3 alongSlope = Vector3.ProjectOnPlane(forward, hit.normal);
            if (alongSlope.sqrMagnitude > 0.0001f)
                velocity = alongSlope.normalized * moveSpeed;
        }

        rigidbody.linearVelocity = velocity;

        if (animator)
            animator.SetBool("Move", true);
    }

    void FollowGround()
    {
        if (!TryGetGroundHit(rigidbody.position, out RaycastHit hit))
            return;

        Vector3 pos = rigidbody.position;
        pos.y = hit.point.y + groundHeightOffset;
        rigidbody.MovePosition(pos);
    }

    void SnapToGround()
    {
        if (!TryGetGroundHit(transform.position, out RaycastHit hit))
            return;

        Vector3 pos = transform.position;
        pos.y = hit.point.y + groundHeightOffset;
        transform.position = pos;
    }

    bool TryGetGroundHit(Vector3 from, out RaycastHit hit)
    {
        Vector3 origin = from + Vector3.up * groundRayHeight;
        float distance = groundRayHeight + maxGroundDistance;
        int mask = GetGroundMask();

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            distance,
            mask,
            QueryTriggerInteraction.Ignore
        );

        hit = default;
        float closest = float.MaxValue;
        bool found = false;

        foreach (RaycastHit candidate in hits)
        {
            if (candidate.collider != null &&
                candidate.collider.transform.IsChildOf(transform))
                continue;

            if (candidate.distance < closest)
            {
                closest = candidate.distance;
                hit = candidate;
                found = true;
            }
        }

        return found;
    }

    int GetGroundMask()
    {
        if (groundMask.value != 0)
            return groundMask.value;

        int mask = Physics.DefaultRaycastLayers;
        mask &= ~LayerMask.GetMask("Enemy", "Friendly", "Turret", "Bullet", "Ignore Raycast");
        return mask;
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHP -= damage;

        if (currentHP <= 0)
            Die();
    }

    protected virtual void Die()
    {
        isDead = true;

        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.isKinematic = true;

        if (animator)
        {
            animator.SetBool("Move", false);
            animator.ResetTrigger("Die");
            animator.SetTrigger("Die");
            animator.Play("Die", 0, 0f);
        }

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(destroyDelay);

        while (transform.position.y > -2f)
        {
            transform.position += Vector3.down * sinkSpeed * Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
