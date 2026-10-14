using System.Collections.Generic;
using UnityEngine;

public class CCTVSkill_TrapMissile : CCTVActiveSkill
{
    public GameObject rocketPrefab;
    public int rocketCount = 8;
    public float rocketSpeed = 32f;
    public float rocketLifetime = 6f;
    public float rocketDamage = 8f;
    public float homingTurnRate = 220f;
    public float spreadAngle = 32f;
    public float verticalSpread = 6f;
    public float muzzleOffset = 1.8f;
    public float slowDuration = 5f;
    public float slowSpeedScale = 0.25f;

    protected override void OnUse(CCTVSkillContext context)
    {
        if (rocketPrefab == null)
        {
            Debug.LogWarning("TrapRocket 프리팹이 비어 있습니다.");
            return;
        }

        Vector3 origin = transform.position + transform.forward * muzzleOffset;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = Vector3.up;

        if (context != null && context.ViewCamera != null)
        {
            Ray aim = context.ViewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            origin = aim.origin + aim.direction * muzzleOffset;
            forward = aim.direction.normalized;
            right = context.ViewCamera.transform.right;
            up = context.ViewCamera.transform.up;
        }
        else if (context != null)
        {
            origin = context.AimRay.origin + context.AimRay.direction * muzzleOffset;
            forward = context.AimRay.direction.normalized;
        }

        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;
        forward.Normalize();

        List<Target_Base> locks = CollectUniqueEnemyLocks(context, origin, forward, rocketCount);
        List<TrapRocket> volley = new List<TrapRocket>(rocketCount);

        int count = Mathf.Max(1, rocketCount);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float yaw = Mathf.Lerp(-spreadAngle, spreadAngle, t);
            float pitch = (i % 2 == 0 ? 1f : -1f) * verticalSpread * 0.35f
                + Random.Range(-verticalSpread, verticalSpread) * 0.25f;

            Vector3 direction = Quaternion.AngleAxis(yaw, up) *
                Quaternion.AngleAxis(pitch, right) *
                forward;
            if (direction.sqrMagnitude < 0.0001f)
                direction = forward;
            direction.Normalize();

            GameObject spawned = Instantiate(
                rocketPrefab,
                origin + right * ((t - 0.5f) * 0.35f),
                Quaternion.FromToRotation(Vector3.up, direction)
            );

            TrapRocket rocket = spawned.GetComponent<TrapRocket>();
            if (rocket == null)
                rocket = spawned.AddComponent<TrapRocket>();

            Transform lockTarget = i < locks.Count && locks[i] != null
                ? locks[i].transform
                : null;

            rocket.Init(
                direction * rocketSpeed,
                rocketLifetime,
                rocketDamage,
                lockTarget,
                transform,
                homingTurnRate,
                slowDuration,
                slowSpeedScale
            );
            volley.Add(rocket);
        }

        IgnoreVolleyCollisions(volley);
    }

    List<Target_Base> CollectUniqueEnemyLocks(
        CCTVSkillContext context,
        Vector3 origin,
        Vector3 forward,
        int count)
    {
        var result = new List<Target_Base>();
        CCTVController cctv = context != null ? context.Cctv : GetComponent<CCTVController>();
        if (cctv == null || cctv.enemies == null)
            return result;

        var ranked = new List<Target_Base>();
        for (int i = 0; i < cctv.enemies.Count; i++)
        {
            YoloSighting sighting = cctv.enemies[i];
            if (sighting == null || sighting.target == null)
                continue;
            if (sighting.target.isDead || sighting.target.isSample)
                continue;
            if (ranked.Contains(sighting.target))
                continue;
            ranked.Add(sighting.target);
        }

        ranked.Sort((a, b) =>
        {
            float scoreA = LockScore(a, origin, forward);
            float scoreB = LockScore(b, origin, forward);
            return scoreB.CompareTo(scoreA);
        });

        int take = Mathf.Min(count, ranked.Count);
        for (int i = 0; i < take; i++)
            result.Add(ranked[i]);

        return result;
    }

    static float LockScore(Target_Base target, Vector3 origin, Vector3 forward)
    {
        Vector3 toTarget = target.transform.position - origin;
        float distance = Mathf.Max(1f, toTarget.magnitude);
        float facing = Vector3.Dot(forward, toTarget.normalized);
        if (facing < -0.15f)
            facing *= 0.25f;
        return facing * 12f - distance * 0.04f;
    }

    static void IgnoreVolleyCollisions(List<TrapRocket> volley)
    {
        for (int i = 0; i < volley.Count; i++)
        {
            if (volley[i] == null)
                continue;
            Collider[] a = volley[i].GetComponentsInChildren<Collider>(true);
            for (int j = i + 1; j < volley.Count; j++)
            {
                if (volley[j] == null)
                    continue;
                Collider[] b = volley[j].GetComponentsInChildren<Collider>(true);
                for (int ia = 0; ia < a.Length; ia++)
                {
                    if (a[ia] == null)
                        continue;
                    for (int ib = 0; ib < b.Length; ib++)
                    {
                        if (b[ib] != null)
                            Physics.IgnoreCollision(a[ia], b[ib], true);
                    }
                }
            }
        }
    }
}
