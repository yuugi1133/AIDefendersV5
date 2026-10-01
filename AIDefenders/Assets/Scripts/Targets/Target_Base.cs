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

    [Header("Path")]
    public Transform destination;
    public float waypointRadius = 1.2f;
    public float arriveDistance = 1.5f;
    public float turnSpeed = 8f;

    [Header("Ground")]
    public LayerMask groundMask;
    public float groundRayHeight = 5f;
    public float groundHeightOffset = 0.05f;
    public float maxGroundDistance = 20f;

    Vector3[] route;
    int routeIndex;
    bool reachedDestination;

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

    public void SetMoveRoute(Vector3[] corners, Transform goal)
    {
        destination = goal;
        route = corners;
        routeIndex = 0;
        reachedDestination = false;

        while (route != null &&
               routeIndex < route.Length &&
               HorizontalDistance(route[routeIndex]) <= waypointRadius)
        {
            routeIndex++;
        }
    }

    void Move()
    {
        AdvanceRoute();

        if (reachedDestination)
        {
            rigidbody.linearVelocity = Vector3.zero;
            if (animator)
                animator.SetBool("Move", false);
            return;
        }

        Vector3 steerPoint = GetSteerPoint();
        Vector3 forward = Flatten(steerPoint - rigidbody.position);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Flatten(transform.forward);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 look = Flatten(transform.forward);
        if (look.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                Quaternion.LookRotation(look.normalized, Vector3.up),
                targetRot,
                1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime)
            );
        }

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

    void AdvanceRoute()
    {
        if (route != null && routeIndex < route.Length)
        {
            if (HorizontalDistance(route[routeIndex]) <= waypointRadius)
                routeIndex++;
        }

        if (route != null && routeIndex < route.Length)
            return;

        if (destination == null)
            return;

        if (HorizontalDistance(destination.position) <= arriveDistance)
            reachedDestination = true;
    }

    Vector3 GetSteerPoint()
    {
        if (route != null && routeIndex < route.Length)
            return route[routeIndex];

        if (destination != null)
            return destination.position;

        return rigidbody.position + Flatten(transform.forward);
    }

    Vector3 BodyPosition()
    {
        if (rigidbody != null)
            return rigidbody.position;

        return transform.position;
    }

    float HorizontalDistance(Vector3 worldPoint)
    {
        return Flatten(worldPoint - BodyPosition()).magnitude;
    }

    static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
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
