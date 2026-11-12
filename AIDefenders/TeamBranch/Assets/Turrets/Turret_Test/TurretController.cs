using System.Collections.Generic;
using UnityEngine;

public class TurretController : MonoBehaviour
{
    public Transform firePoint;
    public WeaponSystem weapon;

    public float rotationSpeed = 5f;
    public float fireAngleThreshold = 3f;
    public float range = 15f;
    public float armorPierce = 1f;
    public float commandTimeout = 1.5f;
    public float friendlyBlockRadius = 0.35f;
    public int sellPrice;
    public string displayName;
    public string researchGroup;
    public int upgradeLevel = 1;
    public GameObject sourcePrefab;
    public TurretUpgradeTier[] upgradeTiers;

    [Header("Sfx")]
    public SfxEvent constructStart;
    public SfxEvent constructComplete;
    public SfxEvent upgradeStart;
    public SfxEvent upgradeComplete;
    public SfxEvent fire;
    public SfxEvent reload;
    public SfxEvent dismantle;

    Transform aimPivot;
    Transform currentTarget;
    Target_Base currentTargetBody;
    Vector3 currentAimPoint;
    float lastCommandTime = -999f;
    CCTVController assignedCctv;
    bool constructing;
    bool upgrading;
    bool ready = true;
    bool constructionInstant;
    int desiredLevel = 1;
    float constructElapsed;
    float constructDuration;
    float upgradeElapsed;
    float upgradeDuration;
    Vector3 builtLocalPosition;
    Vector3 buriedLocalPosition;

    public CCTVController AssignedCctv
    {
        get { return assignedCctv; }
    }

    public bool IsReady
    {
        get { return ready && !constructing && !upgrading; }
    }

    public bool IsConstructing
    {
        get { return constructing; }
    }

    public bool IsUpgrading
    {
        get { return upgrading; }
    }

    public int MaxLevel
    {
        get { return upgradeTiers != null && upgradeTiers.Length > 0 ? upgradeTiers.Length : 1; }
    }

    public string TypeLabel
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            string n = name.Replace("(Clone)", "").Trim();
            return string.IsNullOrEmpty(n) ? "포탑" : n;
        }
    }

    void Awake()
    {
        if (weapon == null)
            weapon = GetComponent<WeaponSystem>();

        desiredLevel = Mathf.Clamp(upgradeLevel, 1, MaxLevel);
        upgradeLevel = desiredLevel;
        ApplyCurrentTier();
    }

    public Vector3 GetInspectAnchor()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return transform.position + Vector3.up * 1.6f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center + Vector3.up * (bounds.extents.y + 0.35f);
    }

    public int GetSellValue()
    {
        int value = Mathf.Max(0, sellPrice);
        if (upgradeTiers == null)
            return value;

        int paidThrough = Mathf.Clamp(upgradeLevel, 1, MaxLevel);
        for (int i = 1; i < paidThrough; i++)
        {
            TurretUpgradeTier tier = upgradeTiers[i];
            if (tier != null)
                value += Mathf.Max(0, tier.upgradeCost);
        }

        return value;
    }

    public bool CanUpgrade()
    {
        return IsReady && upgradeLevel < MaxLevel;
    }

    public float RangeAtLevel(int level)
    {
        TurretUpgradeTier tier = GetTier(level);
        if (tier != null && tier.range > 0f)
            return tier.range;
        return range;
    }

    public static float PreviewRange(GameObject prefab, int level)
    {
        if (prefab == null)
            return 0f;
        TurretController turret = prefab.GetComponentInChildren<TurretController>(true);
        if (turret == null)
            return 0f;
        return turret.RangeAtLevel(level) + TurretResearch.RangeBonus(turret);
    }

    public int NextUpgradeCost()
    {
        TurretUpgradeTier next = GetTier(upgradeLevel + 1);
        return next != null ? Mathf.Max(0, next.upgradeCost) : 0;
    }

    public string NextUpgradeLabel()
    {
        TurretUpgradeTier next = GetTier(upgradeLevel + 1);
        if (next == null)
            return "최대 단계";
        if (!string.IsNullOrWhiteSpace(next.label))
            return next.label;
        return (upgradeLevel + 1) + "단계";
    }

    public void BeginConstruction(float duration, bool instant, CCTVController cctv)
    {
        assignedCctv = cctv;
        ClearTarget();
        builtLocalPosition = transform.localPosition;
        desiredLevel = Mathf.Clamp(upgradeLevel, 1, MaxLevel);
        upgradeLevel = 1;
        constructionInstant = instant || duration <= 0.01f;
        ApplyCurrentTier();

        if (constructionInstant)
        {
            FinishConstruction();
            return;
        }

        float riseHeight = GetRiseHeight();
        Vector3 builtWorld = transform.position;
        Vector3 buriedWorld = builtWorld + Vector3.down * riseHeight;
        Transform parent = transform.parent;
        buriedLocalPosition = parent != null
            ? parent.InverseTransformPoint(buriedWorld)
            : buriedWorld;

        constructing = true;
        upgrading = false;
        ready = false;
        constructElapsed = 0f;
        constructDuration = duration;
        transform.localPosition = buriedLocalPosition;
        PlaySfx(constructStart);
    }

    public void BeginUpgrade(bool instant)
    {
        if (!CanUpgrade())
            return;

        desiredLevel = Mathf.Min(upgradeLevel + 1, MaxLevel);
        ContinueTowardDesired(instant, false);
    }

    public void SetTarget(Transform target, Vector3 aimPoint)
    {
        if (!IsReady)
            return;

        currentTarget = target;
        currentTargetBody = target != null ? target.GetComponentInParent<Target_Base>() : null;
        currentAimPoint = aimPoint;
        lastCommandTime = Time.time;
    }

    public void ClearTarget()
    {
        currentTarget = null;
        currentTargetBody = null;
    }

    void Update()
    {
        if (constructing)
        {
            TickConstruction();
            return;
        }

        if (upgrading)
        {
            TickUpgrade();
            return;
        }

        if (!ready || weapon == null || firePoint == null)
            return;

        AcquireTarget();
        if (currentTargetBody == null)
            return;

        AimAndShoot();
    }

    void AcquireTarget()
    {
        CCTVController cctv = assignedCctv;
        if (cctv == null)
        {
            ClearTarget();
            return;
        }

        Vector3 from = firePoint.position;
        Target_Base closest = null;
        float closestDist = range;

        List<YoloSighting> enemies = cctv.enemies;
        if (enemies != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                YoloSighting sighting = enemies[i];
                if (sighting == null || sighting.target == null || sighting.target.isDead)
                    continue;

                float dist = Vector3.Distance(from, sighting.target.GetAimPoint(from));
                if (dist <= closestDist)
                {
                    closestDist = dist;
                    closest = sighting.target;
                }
            }
        }

        if (closest == null)
        {
            ClearTarget();
            return;
        }

        currentTargetBody = closest;
        currentTarget = closest.transform;
        currentAimPoint = PredictAimPoint(closest, from);
    }

    Vector3 PredictAimPoint(Target_Base target, Vector3 from)
    {
        Vector3 targetPos = target.GetAimPoint(from);
        if (weapon == null || !weapon.UsesProjectileLead)
            return targetPos;

        float speed = weapon.GetMuzzleSpeed();
        if (speed <= 0.01f)
            return targetPos;

        Rigidbody rb = target.GetComponentInParent<Rigidbody>();
        Vector3 targetVel = rb != null ? rb.linearVelocity : Vector3.zero;
        Vector3 toTarget = targetPos - from;
        float a = Vector3.Dot(targetVel, targetVel) - speed * speed;
        if (Mathf.Abs(a) < 0.0001f)
            return targetPos;
        float b = 2f * Vector3.Dot(toTarget, targetVel);
        float c = Vector3.Dot(toTarget, toTarget);
        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f)
            return targetPos;

        float sqrt = Mathf.Sqrt(discriminant);
        float t1 = (-b + sqrt) / (2f * a);
        float t2 = (-b - sqrt) / (2f * a);
        float t = Mathf.Min(t1, t2);
        if (t < 0f)
            t = Mathf.Max(t1, t2);
        if (t < 0f)
            return targetPos;

        return targetPos + targetVel * t;
    }

    void AimAndShoot()
    {
        Transform pivot = aimPivot != null ? aimPivot : transform;
        Vector3 origin = firePoint != null ? firePoint.position : pivot.position;
        Vector3 dir = (currentAimPoint - origin).normalized;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        pivot.rotation = Quaternion.Lerp(
            pivot.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );

        float angle = Vector3.Angle(pivot.forward, dir);
        if (angle > fireAngleThreshold)
            return;

        if (HasIdentifiedFriendlyOnLine(origin, dir))
        {
            Debug.DrawLine(origin, currentAimPoint, Color.yellow);
            return;
        }

        Debug.DrawLine(origin, currentAimPoint, Color.green);
        weapon.Fire(origin, dir, currentTarget);
    }

    bool HasIdentifiedFriendlyOnLine(Vector3 origin, Vector3 dir)
    {
        if (assignedCctv == null || currentTargetBody == null)
            return false;

        float maxDist = Vector3.Distance(origin, currentTargetBody.GetAimPoint(origin));
        if (maxDist < 0.05f)
            return false;

        LayerMask mask = ~0;
        if (weapon != null && weapon.ammo != null)
            mask = weapon.ammo.hitMask;

        RaycastHit[] hits = Physics.SphereCastAll(origin, friendlyBlockRadius, dir, maxDist, mask);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
                continue;

            if (ShouldIgnoreLineHit(col))
                continue;

            Target_Base body = col.GetComponentInParent<Target_Base>();
            if (body == null || body.isDead || body.isSample)
                continue;

            if (body == currentTargetBody)
                return false;

            bool asEnemy;
            if (!assignedCctv.TryGetIdentification(body, out asEnemy))
                continue;

            if (!asEnemy)
                return true;
        }

        return false;
    }

    bool ShouldIgnoreLineHit(Collider col)
    {
        Transform root = col.transform;
        if (root == transform || root.IsChildOf(transform))
            return true;
        if (col.GetComponentInParent<TurretController>() != null)
            return true;
        if (col.GetComponentInParent<TurretPlatform>() != null)
            return true;
        if (col.GetComponentInParent<Projectile_Base>() != null)
            return true;
        if (col.GetComponentInParent<CCTVController>() != null)
            return true;
        if (col.GetComponentInParent<CCTVDroneController>() != null)
            return true;
        if (col.GetComponentInParent<CCTVMoveLimit>() != null)
            return true;
        if (col.GetComponentInParent<TargetGoal>() != null)
            return true;
        return false;
    }

    void TickConstruction()
    {
        constructElapsed += Time.deltaTime;
        float t = constructDuration > 0f
            ? Mathf.Clamp01(constructElapsed / constructDuration)
            : 1f;
        float eased = t * t * (3f - 2f * t);
        transform.localPosition = Vector3.Lerp(buriedLocalPosition, builtLocalPosition, eased);

        if (t >= 1f)
            FinishConstruction();
    }

    void TickUpgrade()
    {
        upgradeElapsed += Time.deltaTime;
        float t = upgradeDuration > 0f
            ? Mathf.Clamp01(upgradeElapsed / upgradeDuration)
            : 1f;
        if (t >= 1f)
            FinishUpgradeWork();
    }

    void FinishConstruction()
    {
        constructing = false;
        transform.localPosition = builtLocalPosition;
        if (assignedCctv != null)
            assignedCctv.RegisterTurret(this);

        ContinueTowardDesired(constructionInstant, true);
    }

    void ContinueTowardDesired(bool instant, bool fromConstruction)
    {
        if (upgradeLevel >= desiredLevel)
        {
            SetFullyReady(fromConstruction);
            return;
        }

        if (instant)
        {
            while (upgradeLevel < desiredLevel)
                AdvanceLevel();
            SetFullyReady(fromConstruction);
            return;
        }

        StartUpgradeWork();
    }

    void StartUpgradeWork()
    {
        if (upgradeLevel >= desiredLevel || upgradeLevel >= MaxLevel)
        {
            SetFullyReady(false);
            return;
        }

        TurretUpgradeTier next = GetTier(upgradeLevel + 1);
        upgradeDuration = next != null ? Mathf.Max(0f, next.upgradeTime) : 4f;
        upgradeElapsed = 0f;
        upgrading = true;
        ready = false;
        ClearTarget();

        if (upgradeDuration <= 0.01f)
        {
            FinishUpgradeWork();
            return;
        }

        PlaySfx(upgradeStart);
    }

    void FinishUpgradeWork()
    {
        AdvanceLevel();
        if (upgradeLevel < desiredLevel)
            StartUpgradeWork();
        else
            SetFullyReady(false);
    }

    void AdvanceLevel()
    {
        upgradeLevel = Mathf.Min(upgradeLevel + 1, MaxLevel);
        ApplyCurrentTier();
    }

    void SetFullyReady(bool fromConstruction)
    {
        constructing = false;
        upgrading = false;
        ready = true;
        ApplyCurrentTier();
        PlaySfx(fromConstruction ? constructComplete : upgradeComplete);
    }

    public void PlayFireSfx()
    {
        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        GameSfx.PlayWorld(fire, pos);
    }

    public void PlayReloadSfx()
    {
        PlaySfx(reload);
    }

    public void PlayDismantleSfx()
    {
        PlaySfx(dismantle);
    }

    void PlaySfx(SfxEvent sfx)
    {
        GameSfx.PlayWorld(sfx, transform.position);
    }

    void ApplyCurrentTier()
    {
        TurretUpgradeTier tier = GetTier(upgradeLevel);
        GameObject visible = ResolveVisibleRoot(tier);

        if (upgradeTiers != null)
        {
            for (int i = 0; i < upgradeTiers.Length; i++)
            {
                TurretUpgradeTier entry = upgradeTiers[i];
                if (entry == null || entry.visualRoot == null)
                    continue;
                bool on = visible != null && entry.visualRoot == visible;
                if (entry.visualRoot != gameObject)
                    entry.visualRoot.SetActive(on);
            }
        }

        if (tier != null)
        {
            range = (tier.range > 0f ? tier.range : range) + TurretResearch.RangeBonus(this);
            armorPierce = tier.armorPierce + TurretResearch.ArmorPierceBonus(this);
            aimPivot = ResolveBarrel(tier, visible);
            if (tier.firePoint != null)
                firePoint = tier.firePoint;
            else
                firePoint = FindNamed(visible != null ? visible.transform : transform, "FirePoint");

            if (weapon != null)
            {
                weapon.ApplyTierStats(
                    tier.fireDelay,
                    tier.maxAmmo,
                    tier.reloadTime,
                    tier.damageScale
                );
            }
        }
        else
        {
            if (aimPivot == null)
                aimPivot = FindNamed(transform, "Turret_barrel");
            if (firePoint == null)
                firePoint = FindNamed(transform, "FirePoint");
        }
    }

    GameObject ResolveVisibleRoot(TurretUpgradeTier tier)
    {
        if (HasRenderable(tier != null ? tier.visualRoot : null))
            return tier.visualRoot;

        if (upgradeTiers == null)
            return null;

        for (int i = 0; i < upgradeTiers.Length; i++)
        {
            TurretUpgradeTier entry = upgradeTiers[i];
            if (HasRenderable(entry != null ? entry.visualRoot : null))
                return entry.visualRoot;
        }

        return null;
    }

    Transform ResolveBarrel(TurretUpgradeTier tier, GameObject visible)
    {
        if (tier != null && tier.barrel != null)
            return tier.barrel;

        Transform search = visible != null ? visible.transform : transform;
        Transform named = FindNamed(search, "Turret_barrel");
        return named != null ? named : search;
    }

    TurretUpgradeTier GetTier(int level)
    {
        if (upgradeTiers == null || upgradeTiers.Length == 0)
            return null;

        int index = Mathf.Clamp(level, 1, upgradeTiers.Length) - 1;
        return upgradeTiers[index];
    }

    static bool HasRenderable(GameObject root)
    {
        if (root == null)
            return false;
        return root.GetComponentInChildren<Renderer>(true) != null;
    }

    static Transform FindNamed(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamed(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    float GetRiseHeight()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return 2f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                bounds.Encapsulate(renderers[i].bounds);
        }

        return Mathf.Max(1f, bounds.size.y + 0.2f);
    }

    void OnDestroy()
    {
        if (assignedCctv != null)
            assignedCctv.UnregisterTurret(this);
    }
}
