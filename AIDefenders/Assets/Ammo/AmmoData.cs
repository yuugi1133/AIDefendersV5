using UnityEngine;
using System.Collections.Generic;

public enum WeaponFireMode
{
    Projectile, // 기관총, 플라즈마 볼 등 탄체
    Homing,     // 미사일
    Hitscan,    // 순간 레이저/레일
    Beam        // 지속 빔 (틱 단위 히트스캔)
}

[CreateAssetMenu(fileName = "AmmoData", menuName = "Game/Ammo")]
public class AmmoData : ScriptableObject
{
    [Header("Fire")]
    public WeaponFireMode fireMode = WeaponFireMode.Projectile;
    public float damage = 10f;

    [Header("Projectile / Homing")]
    public float bulletSpeed = 20f;
    public float gravity = 9.81f;
    public float lifetime = 5f;
    public GameObject bulletPrefab;
    public float homingTurnRate = 180f;

    [Header("Explosion")]
    public float explosionRadius = 0f;
    public float explosionDamage = 0f;

    [Header("Hitscan / Beam")]
    public float hitscanRange = 50f;
    public LayerMask hitMask = ~0;

    [Header("VFX")]
    public GameObject muzzleEffect;
    public GameObject trailEffect;
    public GameObject impactEffect;
    public float beamWidth = 0.12f;
    public Color beamColor = new Color(1f, 0.35f, 0.12f, 0.9f);
    public Color beamCoreColor = new Color(1f, 0.92f, 0.75f, 1f);

    public bool HasExplosion
    {
        get { return explosionRadius > 0f && explosionDamage > 0f; }
    }

    public void ApplyExplosion(Vector3 point, Target_Base exclude = null, float armorPierce = 3f)
    {
        if (!HasExplosion)
            return;

        Collider[] hits = Physics.OverlapSphere(point, explosionRadius, hitMask);
        HashSet<Target_Base> damaged = new HashSet<Target_Base>();

        foreach (Collider col in hits)
        {
            Target_Base target = col.GetComponentInParent<Target_Base>();
            if (target == null || target.isDead)
                continue;

            if (exclude != null && target == exclude)
                continue;

            if (!damaged.Add(target))
                continue;

            Vector3 closest = col.ClosestPoint(point);
            float distance = Vector3.Distance(point, closest);
            float multiplier = Mathf.Clamp01(1f - distance / explosionRadius);
            float splash = explosionDamage * multiplier;

            if (splash > 0f)
                target.TakeDamage(splash, armorPierce);
        }

        Debug.DrawLine(point, point + Vector3.up * explosionRadius, Color.orange, 0.35f);
    }
}
