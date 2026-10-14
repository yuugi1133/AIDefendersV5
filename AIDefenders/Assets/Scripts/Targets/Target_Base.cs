using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Target_Base : MonoBehaviour
{
    [Header("Stats")]
    public float maxHP = 100f;
    public float currentHP;
    public float moveSpeed = 3f;

    public bool isSample = false;

    public string HudLabel
    {
        get
        {
            if (this is Target_Sheep)
                return "양";
            if (this is Target_Wolf)
                return "늑대";
            if (this is Target_Bear)
                return "곰";
            if (this is Target_Dear)
                return "사슴";
            if (this is Target_Chicken)
                return "닭";
            if (this is Target_Friendly)
                return "아군";
            if (this is Target_Enemy)
                return "적";
            return gameObject.name
                .Replace("Target_Friendly_", "")
                .Replace("Target_Enemy_", "")
                .Replace("Target_", "");
        }
    }

    public bool IsActiveInPlay
    {
        get { return !isSample && !isDead && !reachedGoal; }
    }

    [Header("Path")]
    public Transform destination;
    public float waypointRadius = 1.8f;
    public float arriveDistance = 2.2f;
    public float turnSpeed = 8f;
    [Tooltip("이 시간 동안 웨이포인트에 가까워지지 못하면 다음으로 넘어갑니다.")]
    public float waypointSkipTime = 2.5f;

    [Header("Ground")]
    public LayerMask groundMask;
    public float groundRayHeight = 5f;
    public float groundHeightOffset = 0.05f;
    public float maxGroundDistance = 20f;

    Vector3[] route;
    int routeIndex;
    float routePointTimer;
    float lastRouteDistance = float.MaxValue;
    bool reachedDestination;
    bool reachedGoal;

    [Header("Death")]
    public bool isDead = false;
    public float sinkSpeed = 1f;
    public float destroyDelay = 3f;

    [Header("Components")]
    public Animator animator;
    public Renderer renderer;
    public Rigidbody rigidbody;

    readonly List<StatusEffect> statusEffects = new List<StatusEffect>();

    public IReadOnlyList<StatusEffect> StatusEffects
    {
        get { return statusEffects; }
    }

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
    {
        if (isSample || isDead)
            return;

        TickStatusEffects(Time.deltaTime);
    }

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

    public Vector3 GetAimPoint(Vector3 from)
    {
        if (TryGetAimCollider(out Collider aimCollider))
        {
            Vector3 center = aimCollider.bounds.center;
            Vector3 closest = aimCollider.ClosestPoint(from);
            closest.y = center.y;
            return closest;
        }

        if (renderer != null)
            return renderer.bounds.center;

        return transform.position + Vector3.up * 0.9f;
    }

    public bool TryGetAimCollider(out Collider aimCollider)
    {
        aimCollider = null;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        float best = -1f;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || collider.isTrigger)
                continue;

            float size = collider.bounds.size.sqrMagnitude;
            if (size > best)
            {
                best = size;
                aimCollider = collider;
            }
        }

        return aimCollider != null;
    }

    public void SetMoveRoute(Vector3[] corners, Transform goal)
    {
        destination = goal;
        route = corners;
        routeIndex = 0;
        routePointTimer = 0f;
        lastRouteDistance = float.MaxValue;
        reachedDestination = false;

        while (route != null &&
               routeIndex < route.Length &&
               HorizontalDistance(route[routeIndex]) <= 0.4f)
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

        float speed = GetStatusMoveSpeed();
        Vector3 velocity = forward * speed;

        if (TryGetGroundHit(rigidbody.position, out RaycastHit hit))
        {
            Vector3 alongSlope = Vector3.ProjectOnPlane(forward, hit.normal);
            if (alongSlope.sqrMagnitude > 0.0001f)
                velocity = alongSlope.normalized * speed;
        }

        rigidbody.linearVelocity = velocity;

        if (animator)
            animator.SetBool("Move", true);
    }

    void AdvanceRoute()
    {
        if (route != null && routeIndex < route.Length)
        {
            float dist = HorizontalDistance(route[routeIndex]);
            bool reached = dist <= waypointRadius || HasPassedCurrentWaypoint();
            if (!reached)
            {
                routePointTimer += Time.fixedDeltaTime;
                bool stuck = dist >= lastRouteDistance - 0.05f;
                reached = waypointSkipTime > 0f &&
                    routePointTimer >= waypointSkipTime &&
                    stuck;
            }

            lastRouteDistance = dist;
            if (reached)
            {
                routeIndex++;
                routePointTimer = 0f;
                lastRouteDistance = float.MaxValue;
            }
        }

        if (route != null && routeIndex < route.Length)
            return;

        if (destination == null)
            return;

        if (HorizontalDistance(destination.position) <= arriveDistance)
            reachedDestination = true;
    }

    bool HasPassedCurrentWaypoint()
    {
        if (route == null || routeIndex + 1 >= route.Length)
            return false;

        Vector3 dir = Flatten(route[routeIndex + 1] - route[routeIndex]);
        if (dir.sqrMagnitude < 0.0001f)
            return false;

        Vector3 fromWaypoint = Flatten(BodyPosition() - route[routeIndex]);
        return Vector3.Dot(fromWaypoint, dir.normalized) > 0.35f;
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
        ShowDamageNumber(damage);

        if (currentHP <= 0)
            Die();
    }

    public bool ApplyStatus(StatusEffect effect)
    {
        if (effect == null || isDead || isSample)
            return false;

        if (!effect.allowMultiple && !string.IsNullOrEmpty(effect.effectId))
        {
            StatusEffect existing = FindStatus(effect.effectId);
            if (existing != null)
            {
                existing.RefreshFrom(effect);
                return true;
            }
        }

        statusEffects.Add(effect);
        effect.Bind(this);
        return true;
    }

    public bool RemoveStatus(StatusEffect effect)
    {
        if (effect == null)
            return false;

        if (!statusEffects.Remove(effect))
            return false;

        effect.Unbind();
        return true;
    }

    public bool RemoveStatus(string effectId)
    {
        if (string.IsNullOrEmpty(effectId))
            return false;

        bool removed = false;
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = statusEffects[i];
            if (effect == null || effect.effectId != effectId)
                continue;

            statusEffects.RemoveAt(i);
            effect.Unbind();
            removed = true;
        }

        return removed;
    }

    public bool HasStatus(string effectId)
    {
        return FindStatus(effectId) != null;
    }

    public void ClearStatusEffects()
    {
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = statusEffects[i];
            statusEffects.RemoveAt(i);
            if (effect != null)
                effect.Unbind();
        }
    }

    StatusEffect FindStatus(string effectId)
    {
        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffect effect = statusEffects[i];
            if (effect != null && effect.effectId == effectId)
                return effect;
        }

        return null;
    }

    void TickStatusEffects(float deltaTime)
    {
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = statusEffects[i];
            if (effect == null || !effect.Tick(deltaTime))
            {
                statusEffects.RemoveAt(i);
                if (effect != null)
                    effect.Unbind();
            }
        }
    }

    float GetStatusMoveSpeed()
    {
        float speed = moveSpeed;
        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffect effect = statusEffects[i];
            if (effect != null)
                speed = effect.ModifyMoveSpeed(speed);
        }

        return Mathf.Max(0f, speed);
    }

    void ShowDamageNumber(float damage)
    {
        if (isSample || damage <= 0.01f)
            return;

        Vector3 world = transform.position + Vector3.up * 1.4f;
        if (renderer != null)
            world = renderer.bounds.center + Vector3.up * (renderer.bounds.extents.y + 0.2f);

        HudFx.ShowDamage(world, damage);
    }

    protected virtual void Die()
    {
        isDead = true;
        ClearStatusEffects();
        DisableColliders();

        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.isKinematic = true;

        OnKilled(FindWaveDirector());

        if (animator)
        {
            animator.SetBool("Move", false);
            animator.ResetTrigger("Die");
            animator.SetTrigger("Die");
            animator.Play("Die", 0, 0f);
        }

        StartCoroutine(DeathRoutine());
    }

    protected virtual void OnKilled(WaveDirector director)
    {
    }

    public void NotifyReachedGoal()
    {
        if (isSample || isDead || reachedGoal)
            return;

        reachedGoal = true;
        ClearStatusEffects();
        OnReachedGoal(FindWaveDirector());
        RemoveAtGoal();
    }

    protected virtual void OnReachedGoal(WaveDirector director)
    {
    }

    void RemoveAtGoal()
    {
        DisableColliders();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }

        Destroy(gameObject);
    }

    protected static WaveDirector FindWaveDirector()
    {
        return FindFirstObjectByType<WaveDirector>();
    }

    void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
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
