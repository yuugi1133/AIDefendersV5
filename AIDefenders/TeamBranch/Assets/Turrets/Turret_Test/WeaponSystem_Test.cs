using UnityEngine;
using System.Collections;

public class WeaponSystem : MonoBehaviour
{
    public AmmoData ammo;

    public float fireDelay = 0.5f;
    public int maxAmmo = 10;
    public float reloadTime = 2f;
    [Tooltip("발사 방향이 조준 축에서 어긋날 수 있는 최대 각도(도). 0이면 집탄.")]
    public float spreadAngle = 0f;

    int currentAmmo;
    bool isReloading = false;
    float lastFireTime = 0f;
    float damageScale = 1f;
    TurretController turret;
    float baseSpread;
    bool capturedSpread;
    GameObject beamRoot;
    LineRenderer beamGlow;
    LineRenderer beamCore;
    float lastBeamTime;

    public int CurrentAmmo
    {
        get { return currentAmmo; }
    }

    public int MagazineSize
    {
        get { return maxAmmo; }
    }

    public bool IsReloading
    {
        get { return isReloading; }
    }

    public string AmmoLabel
    {
        get
        {
            if (ammo == null)
                return "없음";
            return string.IsNullOrWhiteSpace(ammo.name) ? "탄환" : ammo.name;
        }
    }

    void Awake()
    {
        turret = GetComponent<TurretController>();
        if (turret == null)
            turret = GetComponentInParent<TurretController>();
        CaptureBaseSpread();
    }

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    public void ApplyTierStats(float delay, int magazine, float reload, float scale)
    {
        CaptureBaseSpread();
        fireDelay = Mathf.Max(0.01f, delay);
        maxAmmo = Mathf.Max(1, magazine);
        reloadTime = Mathf.Max(0.01f, reload + TurretResearch.ReloadBonus(turret));
        spreadAngle = Mathf.Max(0f, baseSpread + TurretResearch.SpreadBonus(turret));
        damageScale = Mathf.Max(0.01f, scale);
        if (currentAmmo > maxAmmo)
            currentAmmo = maxAmmo;
        if (!isReloading && currentAmmo <= 0)
            currentAmmo = maxAmmo;
    }

    float GetShotDamage()
    {
        if (ammo == null)
            return 0f;
        return ammo.damage * damageScale;
    }

    float GetArmorPierce()
    {
        return turret != null ? turret.armorPierce : 3f;
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

        return ammo.bulletSpeed + TurretResearch.BulletSpeedBonus(turret);
    }

    void CaptureBaseSpread()
    {
        if (capturedSpread)
            return;
        baseSpread = spreadAngle;
        capturedSpread = true;
    }

    public void RestoreAmmo(int amount)
    {
        if (amount <= 0)
            return;
        currentAmmo = Mathf.Min(maxAmmo, currentAmmo + amount);
    }

    public bool CanFire()
    {
        if (ammo == null) return false;
        if (isReloading) return false;
        if (Time.time - lastFireTime < fireDelay) return false;
        if (ammo.fireMode != WeaponFireMode.Beam && currentAmmo <= 0) return false;

        return true;
    }

    public void Fire(Vector3 origin, Vector3 direction, Transform target = null)
    {
        if (ammo == null)
            return;

        Vector3 dir = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : transform.forward;
        dir = ApplySpread(dir);

        if (ammo.fireMode == WeaponFireMode.Beam)
        {
            FireBeam(origin, dir);
            return;
        }

        if (!CanFire())
            return;

        CombatVfx.PlayMuzzle(ammo.muzzleEffect, origin, dir);

        switch (ammo.fireMode)
        {
            case WeaponFireMode.Projectile:
                FireProjectile(origin, dir, null);
                break;
            case WeaponFireMode.Homing:
                FireProjectile(origin, dir, target);
                break;
            case WeaponFireMode.Hitscan:
                FireHitscan(origin, dir);
                break;
        }

        lastFireTime = Time.time;
        PlayFireSfx();
        ConsumeAmmo();
    }

    void LateUpdate()
    {
        if (beamRoot == null)
            return;

        if (Time.time - lastBeamTime > 0.08f)
            SetBeamVisible(false);
    }

    void OnDestroy()
    {
        if (beamRoot != null)
            Destroy(beamRoot);
    }

    Vector3 ApplySpread(Vector3 direction)
    {
        if (spreadAngle <= 0.01f)
            return direction;

        Vector3 right = Vector3.Cross(direction, Vector3.up);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.Cross(direction, Vector3.right);
        right.Normalize();
        Vector3 up = Vector3.Cross(right, direction);

        Vector2 offset = Random.insideUnitCircle * spreadAngle;
        return (Quaternion.AngleAxis(offset.x, up) * Quaternion.AngleAxis(offset.y, right) * direction).normalized;
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

        Vector3 velocity = direction * GetMuzzleSpeed();
        bool homing = ammo.fireMode == WeaponFireMode.Homing;

        bullet.Init(
            velocity,
            ammo.gravity,
            GetShotDamage(),
            ammo.lifetime,
            homing ? homingTarget : null,
            homing ? ammo.homingTurnRate : 0f,
            ammo,
            GetArmorPierce(),
            turret
        );

        CombatVfx.AttachTrail(ammo.trailEffect, bulletObj.transform);
    }

    void FireHitscan(Vector3 origin, Vector3 direction)
    {
        float range = ammo.hitscanRange > 0f ? ammo.hitscanRange : 50f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, ammo.hitMask))
        {
            Target_Base target = hit.collider.GetComponentInParent<Target_Base>();
            if (target != null)
                target.TakeDamage(GetShotDamage(), GetArmorPierce(), TurretResearch.WeakspotBonus(turret, target), turret);

            ammo.ApplyExplosion(hit.point, null, GetArmorPierce());
            CombatVfx.PlayImpact(ammo.impactEffect, hit.point, hit.normal);
            Debug.DrawLine(origin, hit.point, Color.cyan, 0.1f);
        }
        else
        {
            Debug.DrawRay(origin, direction * range, Color.cyan, 0.1f);
        }
    }

    void FireBeam(Vector3 origin, Vector3 direction)
    {
        float range = ammo.hitscanRange > 0f ? ammo.hitscanRange : 50f;
        Vector3 end = origin + direction * range;
        Vector3 impactNormal = -direction;
        bool hitSomething = false;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, ammo.hitMask))
        {
            end = hit.point;
            impactNormal = hit.normal;
            hitSomething = true;

            if (CanFire())
            {
                Target_Base target = hit.collider.GetComponentInParent<Target_Base>();
                if (target != null)
                    target.TakeDamage(GetShotDamage(), GetArmorPierce(), TurretResearch.WeakspotBonus(turret, target), turret);

                ammo.ApplyExplosion(hit.point, null, GetArmorPierce());
                CombatVfx.PlayBeamImpact(ammo.impactEffect, hit.point, impactNormal);
                lastFireTime = Time.time;
                PlayFireSfx();
            }
        }

        ShowBeam(origin, end);
        lastBeamTime = Time.time;
        if (!hitSomething)
            Debug.DrawRay(origin, direction * range, Color.red, 0.05f);
    }

    void ShowBeam(Vector3 start, Vector3 end)
    {
        EnsureBeam();
        SetBeamVisible(true);
        beamGlow.SetPosition(0, start);
        beamGlow.SetPosition(1, end);
        beamCore.SetPosition(0, start);
        beamCore.SetPosition(1, end);

        float width = ammo.beamWidth > 0.01f ? ammo.beamWidth : 0.12f;
        beamGlow.startWidth = width;
        beamGlow.endWidth = width * 0.65f;
        beamCore.startWidth = width * 0.35f;
        beamCore.endWidth = width * 0.18f;
        ApplyBeamColors();
    }

    void EnsureBeam()
    {
        if (beamRoot != null)
            return;

        beamRoot = new GameObject("TurretBeam");
        beamRoot.transform.SetParent(transform, false);
        beamGlow = CreateBeamLine(beamRoot.transform, "Glow", 8);
        beamCore = CreateBeamLine(beamRoot.transform, "Core", 12);
    }

    LineRenderer CreateBeamLine(Transform parent, string name, int caps)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = caps;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.material = CreateBeamMaterial();
        return line;
    }

    static Material CreateBeamMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Hidden/InternalErrorShader");
        return new Material(shader);
    }

    void ApplyBeamColors()
    {
        Color glow = ammo != null ? ammo.beamColor : new Color(1f, 0.35f, 0.12f, 0.9f);
        Color core = ammo != null ? ammo.beamCoreColor : Color.white;
        beamGlow.startColor = glow;
        beamGlow.endColor = glow;
        beamCore.startColor = core;
        beamCore.endColor = new Color(core.r, core.g, core.b, core.a * 0.65f);
    }

    void SetBeamVisible(bool visible)
    {
        if (beamRoot != null)
            beamRoot.SetActive(visible);
    }

    void PlayFireSfx()
    {
        if (turret != null)
            turret.PlayFireSfx();
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
        if (turret != null)
            turret.PlayReloadSfx();
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }
}
