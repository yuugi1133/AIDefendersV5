using UnityEngine;
using System.Collections;

public class WeaponSystem : MonoBehaviour
{
    public AmmoData ammo;

    public float fireDelay = 0.5f;  //?????
    public int maxAmmo = 10;        //??? ?????
    public float reloadTime = 2f;   //?????? ????

    int currentAmmo;
    bool isReloading = false;
    float lastFireTime = 0f;

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    public bool UsesProjectileLead
    {
        get
        {
            if (ammo == null)
                return false;

            return ammo.fireMode == WeaponFireMode.Projectile
                || ammo.fireMode == WeaponFireMode.Homing;
        }
    }

    public float GetMuzzleSpeed()
    {
        if (ammo == null)
            return 0f;

        return ammo.bulletSpeed;
    }

    public bool CanFire()
    {
        if (ammo == null) return false;
        if (isReloading) return false;
        if (Time.time - lastFireTime < fireDelay) return false;
        if (currentAmmo <= 0) return false;

        return true;
    }

    public void Fire(Vector3 origin, Vector3 direction, Transform target = null)
    {
        if (!CanFire())
            return;

        Vector3 dir = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : transform.forward;

        switch (ammo.fireMode)
        {
            case WeaponFireMode.Projectile:
                FireProjectile(origin, dir, null);
                break;
            case WeaponFireMode.Homing:
                FireProjectile(origin, dir, target);
                break;
            case WeaponFireMode.Hitscan:
            case WeaponFireMode.Beam:
                FireHitscan(origin, dir);
                break;
        }

        lastFireTime = Time.time;
        ConsumeAmmo();
    }

    void FireProjectile(Vector3 origin, Vector3 direction, Transform homingTarget)
    {
        if (ammo.bulletPrefab == null)
        {
            Debug.LogWarning(name + ": bulletPrefab?? ???????.");
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject bulletObj = Instantiate(ammo.bulletPrefab, origin, rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet == null)
        {
            Debug.LogWarning(name + ": ? ??????? Bullet?? ???????.");
            Destroy(bulletObj);
            return;
        }

        Vector3 velocity = direction * ammo.bulletSpeed;
        bool homing = ammo.fireMode == WeaponFireMode.Homing;

        bullet.Init(
            velocity,
            ammo.gravity,
            ammo.damage,
            ammo.lifetime,
            homing ? homingTarget : null,
            homing ? ammo.homingTurnRate : 0f,
            ammo
        );
    }

    void FireHitscan(Vector3 origin, Vector3 direction)
    {
        float range = ammo.hitscanRange > 0f ? ammo.hitscanRange : 50f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, ammo.hitMask))
        {
            Target_Base target = hit.collider.GetComponentInParent<Target_Base>();
            if (target != null)
                target.TakeDamage(ammo.damage);

            ammo.ApplyExplosion(hit.point, null);
            Debug.DrawLine(origin, hit.point, Color.cyan, 0.1f);
        }
        else
        {
            Debug.DrawRay(origin, direction * range, Color.cyan, 0.1f);
        }
    }

    void ConsumeAmmo()
    {
        if (ammo.fireMode == WeaponFireMode.Beam)
            return;

        currentAmmo--;
        if (currentAmmo <= 0)
            StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }
}
