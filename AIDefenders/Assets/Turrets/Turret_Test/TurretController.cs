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

    Transform[] activeMuzzles = new Transform[0];
    Transform[] spinBarrels = new Transform[0];
    Vector3[] barrelSpinAxes = new Vector3[0];
    int muzzleIndex;
    float barrelSpinSpeed = 720f;
    float barrelSpinVel;

    [Header("Vfx")]
    public GameObject workLoopFx;

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
    AudioSource fireLoop;
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
    Vector3[] visualRestLocal;
    GameObject upgradeOutgoing;
    GameObject upgradeIncoming;
    Vector3 upgradeOutRest;
    Vector3 upgradeOutBuried;
    Vector3 upgradeInRest;
    Vector3 upgradeInBuried;
    [System.NonSerialized] GameObject workFxInstance;
    [System.NonSerialized] bool playedUpgradeStartSfx;
    [System.NonSerialized] bool upgradeSfxOnly;

    enum CompleteSfx
    {
        None,
        Construct,
        Upgrade
    }

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
        CombatCollision.AssignLayer(gameObject, "Turret");

        if (weapon == null)
            weapon = GetComponent<WeaponSystem>();

        desiredLevel = Mathf.Clamp(upgradeLevel, 1, MaxLevel);
        upgradeLevel = desiredLevel;
        builtLocalPosition = transform.localPosition;
        CacheVisualRests();
        ApplyCurrentTier();
    }

    public Vector3 GetInspectAnchor()
    {
        Bounds bounds;
        if (!TryGetVisualBounds(false, out bounds))
            return transform.position + Vector3.up * 1.6f;

        return bounds.center + Vector3.up * (bounds.extents.y + 0.35f);
    }

    bool TryGetVisualBounds(bool includeInactive, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!IsInspectRenderer(renderer))
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    static bool IsInspectRenderer(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled)
            return false;
        if (renderer is LineRenderer || renderer is ParticleSystemRenderer)
            return false;
        if (renderer.transform != null && renderer.transform.name == "TurretBeam")
            return false;
        Transform parent = renderer.transform.parent;
        if (parent != null && parent.name == "TurretBeam")
            return false;
        return true;
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
        upgradeSfxOnly = false;
        ready = false;
        constructElapsed = 0f;
        constructDuration = duration;
        transform.localPosition = buriedLocalPosition;
        PlaySfx(constructStart);
        StartWorkFx();
    }

    public void BeginUpgrade(bool instant)
    {
        if (!CanUpgrade())
            return;

        desiredLevel = Mathf.Min(upgradeLevel + 1, MaxLevel);
        upgradeSfxOnly = true;
        ContinueTowardDesired(instant, CompleteSfx.Upgrade);
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

        if (!ready || weapon == null)
        {
            TickBarrelSpin(false);
            return;
        }

        if (firePoint == null && (activeMuzzles == null || activeMuzzles.Length == 0))
        {
            TickBarrelSpin(false);
            return;
        }

        AcquireTarget();
        if (currentTargetBody == null)
        {
            TickBarrelSpin(weapon != null && weapon.IsBursting);
            return;
        }

        AimAndShoot();
        TickBarrelSpin(weapon != null && weapon.IsBursting);
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
        Transform muzzle = CurrentMuzzle();
        Vector3 origin = muzzle != null
            ? muzzle.position
            : (firePoint != null ? firePoint.position : pivot.position);
        Vector3 dir = (currentAimPoint - origin).normalized;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        Vector3 shotDir = dir;
        if (TurretResearch.HasTopAttack(this))
        {
            Vector3 yaw = dir;
            yaw.y = 0f;
            if (yaw.sqrMagnitude < 0.0001f)
                yaw = pivot.forward;
            shotDir = (yaw.normalized + Vector3.up).normalized;
        }

        Quaternion targetRot = Quaternion.LookRotation(shotDir);
        pivot.rotation = Quaternion.Lerp(
            pivot.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );

        float angle = Vector3.Angle(pivot.forward, shotDir);
        if (angle > fireAngleThreshold)
            return;

        if (HasIdentifiedFriendlyOnLine(origin, dir))
        {
            Debug.DrawLine(origin, currentAimPoint, Color.yellow);
            return;
        }

        Debug.DrawLine(origin, currentAimPoint, Color.green);
        if (weapon.Fire(origin, shotDir, currentTarget))
            AdvanceMuzzle();
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
        AnimateUpgradeSwap(t);
        if (t >= 1f)
            FinishUpgradeWork();
    }

    void FinishConstruction()
    {
        constructing = false;
        transform.localPosition = builtLocalPosition;
        if (assignedCctv != null)
            assignedCctv.RegisterTurret(this);

        ContinueTowardDesired(constructionInstant, CompleteSfx.Construct);
    }

    void ContinueTowardDesired(bool instant, CompleteSfx completeSfx)
    {
        if (upgradeLevel >= desiredLevel)
        {
            SetFullyReady(completeSfx);
            return;
        }

        if (instant)
        {
            while (upgradeLevel < desiredLevel)
                AdvanceLevel();
            SetFullyReady(completeSfx);
            return;
        }

        StartUpgradeWork();
    }

    void StartUpgradeWork()
    {
        if (upgradeLevel >= desiredLevel || upgradeLevel >= MaxLevel)
        {
            SetFullyReady(CompleteSfx.Upgrade);
            return;
        }

        TurretUpgradeTier next = GetTier(upgradeLevel + 1);
        upgradeDuration = next != null ? Mathf.Max(0f, next.upgradeTime) : 4f;
        upgradeElapsed = 0f;
        upgrading = true;
        ready = false;
        playedUpgradeStartSfx = false;
        ClearTarget();
        BeginUpgradeSwap();
        PlayUpgradeStartSfx();

        if (upgradeDuration <= 0.01f)
        {
            FinishUpgradeWork();
            return;
        }

        StartWorkFx();
    }

    void FinishUpgradeWork()
    {
        AdvanceLevel();
        if (upgradeLevel < desiredLevel)
            StartUpgradeWork();
        else
            SetFullyReady(CompleteSfx.Upgrade);
    }

    void AdvanceLevel()
    {
        upgradeLevel = Mathf.Min(upgradeLevel + 1, MaxLevel);
        ApplyCurrentTier();
    }

    void SetFullyReady(CompleteSfx completeSfx)
    {
        constructing = false;
        upgrading = false;
        ready = true;
        RestoreVisualRests();
        ApplyCurrentTier();
        StopWorkFx();
        if (completeSfx == CompleteSfx.Construct)
            PlaySfx(constructComplete);
        else if (completeSfx == CompleteSfx.Upgrade)
            PlaySfx(upgradeComplete);
        upgradeSfxOnly = false;
    }

    public void PlayFireSfx()
    {
        Vector3 pos = firePoint != null ? firePoint.position : transform.position;
        GameSfx.PlayWorld(fire, pos);
    }

    public void StartFireLoop()
    {
        if (fireLoop != null && fireLoop.isPlaying)
            return;
        StopFireLoop();
        fireLoop = GameSfx.PlayAttachedLoop(fire, transform);
    }

    public void StopFireLoop()
    {
        GameSfx.StopAttached(fireLoop);
        fireLoop = null;
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
        if (sfx == null)
            return;
        if (upgradeSfxOnly && (sfx == constructStart || sfx == constructComplete))
            return;
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
                SetVisualActive(entry.visualRoot, on);
            }
        }

        SyncNamedTierVisuals(visible);

        if (tier != null)
        {
            range = (tier.range > 0f ? tier.range : range) + TurretResearch.RangeBonus(this);
            armorPierce = tier.armorPierce + TurretResearch.ArmorPierceBonus(this);
            aimPivot = ResolveBarrel(tier, visible);
            BindHardpoints(tier, visible);

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
            BindHardpoints(null, null);
        }
    }

    GameObject ResolveVisibleRoot(TurretUpgradeTier tier)
    {
        if (HasRenderable(tier != null ? tier.visualRoot : null))
            return tier.visualRoot;

        GameObject named = FindTierVisual(upgradeLevel);
        if (HasRenderable(named))
            return named;

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
        Transform turretNode = FindNamed(search, "Turret");
        if (turretNode != null && turretNode != transform)
            return turretNode;

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

    void SyncNamedTierVisuals(GameObject visible)
    {
        for (int i = 1; i <= 3; i++)
        {
            Transform node = transform.Find("MG_T" + i);
            if (node == null)
                node = transform.Find("Tier_" + i);
            if (node == null)
                continue;
            SetVisualActive(node.gameObject, visible != null && node.gameObject == visible);
        }
    }

    GameObject FindTierVisual(int level)
    {
        string[] names =
        {
            "MG_T" + level,
            "Tier_" + level,
            "Tier " + level
        };
        for (int i = 0; i < names.Length; i++)
        {
            Transform node = transform.Find(names[i]);
            if (node == null)
                node = FindNamed(transform, names[i]);
            if (node != null)
                return node.gameObject;
        }

        return null;
    }

    void BindHardpoints(TurretUpgradeTier tier, GameObject visible)
    {
        Transform search = visible != null ? visible.transform : transform;
        List<Transform> muzzles = new List<Transform>();
        List<Transform> barrels = new List<Transform>();
        bool rotary = IsRotaryMachineGun(visible);

        AppendAssigned(muzzles, tier != null ? tier.muzzles : null);
        AppendAssigned(barrels, tier != null ? tier.barrels : null);

        if (rotary)
        {
            if (muzzles.Count == 0)
                CollectNamed(search, muzzles, new[] { "Muzzle" }, new[] { "Collar" });
            if (barrels.Count == 0)
                CollectNamed(search, barrels, new[] { "Barrel", "GatlingTube" }, new[] { "Collar" });
            if (muzzles.Count == 0)
                CollectNamed(search, muzzles, new[] { "FirePoint" }, null);
        }

        SortByName(muzzles);
        SortByName(barrels);

        if (muzzles.Count == 0)
        {
            for (int i = 0; i < barrels.Count; i++)
            {
                Transform tip = EnsureTipMuzzle(barrels[i]);
                if (tip != null && !muzzles.Contains(tip))
                    muzzles.Add(tip);
            }
        }

        if (tier != null && tier.firePoint != null && muzzles.Count == 0)
            muzzles.Add(tier.firePoint);

        Transform foundFire = FindNamed(search, "FirePoint");
        if (foundFire != null && muzzles.Count == 0)
            muzzles.Add(foundFire);

        activeMuzzles = muzzles.ToArray();
        spinBarrels = barrels.ToArray();
        barrelSpinAxes = new Vector3[spinBarrels.Length];
        for (int i = 0; i < spinBarrels.Length; i++)
            barrelSpinAxes[i] = GuessSpinAxis(spinBarrels[i]);

        barrelSpinSpeed = tier != null && tier.barrelSpinSpeed > 1f ? tier.barrelSpinSpeed : 720f;
        if (muzzleIndex >= activeMuzzles.Length)
            muzzleIndex = 0;

        if (activeMuzzles.Length > 0)
            firePoint = activeMuzzles[0];
        else if (tier != null && tier.firePoint != null)
            firePoint = tier.firePoint;
        else
            firePoint = foundFire;
    }

    Transform CurrentMuzzle()
    {
        if (activeMuzzles == null || activeMuzzles.Length == 0)
            return firePoint;
        if (muzzleIndex < 0 || muzzleIndex >= activeMuzzles.Length)
            muzzleIndex = 0;
        Transform muzzle = activeMuzzles[muzzleIndex];
        return muzzle != null ? muzzle : firePoint;
    }

    void AdvanceMuzzle()
    {
        if (activeMuzzles == null || activeMuzzles.Length <= 1)
            return;
        muzzleIndex = (muzzleIndex + 1) % activeMuzzles.Length;
    }

    void TickBarrelSpin(bool firing)
    {
        if (spinBarrels == null || spinBarrels.Length == 0)
            return;

        float target = firing && !isReloadingLike() ? barrelSpinSpeed : 0f;
        barrelSpinVel = Mathf.MoveTowards(barrelSpinVel, target, barrelSpinSpeed * Time.deltaTime * 3f);
        if (barrelSpinVel <= 0.01f)
            return;

        float step = barrelSpinVel * Time.deltaTime;
        for (int i = 0; i < spinBarrels.Length; i++)
        {
            Transform barrel = spinBarrels[i];
            if (barrel == null || barrel == aimPivot)
                continue;
            Vector3 axis = i < barrelSpinAxes.Length ? barrelSpinAxes[i] : Vector3.forward;
            if (axis.sqrMagnitude < 0.0001f)
                axis = Vector3.forward;
            barrel.Rotate(axis.normalized, step, Space.Self);
        }
    }

    bool isReloadingLike()
    {
        return weapon != null && weapon.IsReloading;
    }

    bool IsRotaryMachineGun(GameObject visible)
    {
        if (!string.IsNullOrEmpty(researchGroup) &&
            string.Equals(researchGroup, TurretResearch.MachineGun, System.StringComparison.OrdinalIgnoreCase))
            return true;
        if (visible != null && visible.name.StartsWith("MG_", System.StringComparison.OrdinalIgnoreCase))
            return true;
        return name.IndexOf("Turret_MG", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void AppendAssigned(List<Transform> dest, Transform[] source)
    {
        if (source == null)
            return;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null && !dest.Contains(source[i]))
                dest.Add(source[i]);
        }
    }

    static void CollectNamed(Transform root, List<Transform> dest, string[] prefixes, string[] excludes)
    {
        if (root == null)
            return;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform node = all[i];
            if (node == null || node == root)
                continue;
            if (!NameMatches(node.name, prefixes, excludes))
                continue;
            if (!dest.Contains(node))
                dest.Add(node);
        }
    }

    static bool NameMatches(string name, string[] prefixes, string[] excludes)
    {
        if (string.IsNullOrEmpty(name) || prefixes == null)
            return false;
        if (excludes != null)
        {
            for (int i = 0; i < excludes.Length; i++)
            {
                if (!string.IsNullOrEmpty(excludes[i]) &&
                    name.IndexOf(excludes[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }
        }

        for (int i = 0; i < prefixes.Length; i++)
        {
            if (!string.IsNullOrEmpty(prefixes[i]) &&
                name.StartsWith(prefixes[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static void SortByName(List<Transform> list)
    {
        list.Sort((a, b) => string.Compare(a != null ? a.name : "", b != null ? b.name : "", System.StringComparison.OrdinalIgnoreCase));
    }

    static Transform EnsureTipMuzzle(Transform barrel)
    {
        if (barrel == null)
            return null;

        Transform existing = barrel.Find("Muzzle");
        if (existing != null)
            return existing;

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(barrel, false);
        muzzle.transform.localRotation = Quaternion.identity;
        muzzle.transform.position = BarrelTip(barrel);
        return muzzle.transform;
    }

    static Vector3 BarrelTip(Transform barrel)
    {
        Renderer renderer = barrel.GetComponent<Renderer>();
        if (renderer == null)
            renderer = barrel.GetComponentInChildren<Renderer>();
        if (renderer == null)
            return barrel.position + barrel.forward * 0.45f;

        Vector3 forward = barrel.forward;
        Bounds bounds = renderer.bounds;
        Vector3[] corners =
        {
            bounds.min,
            bounds.max,
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)
        };

        float best = float.NegativeInfinity;
        Vector3 tip = barrel.position;
        for (int i = 0; i < corners.Length; i++)
        {
            float d = Vector3.Dot(corners[i] - barrel.position, forward);
            if (d > best)
            {
                best = d;
                tip = barrel.position + forward * d;
            }
        }

        return tip;
    }

    static Vector3 GuessSpinAxis(Transform barrel)
    {
        if (barrel == null)
            return Vector3.forward;

        MeshFilter filter = barrel.GetComponent<MeshFilter>();
        if (filter == null)
            filter = barrel.GetComponentInChildren<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            return Vector3.forward;

        Vector3 size = filter.sharedMesh.bounds.size;
        if (size.z >= size.x && size.z >= size.y)
            return Vector3.forward;
        if (size.y >= size.x)
            return Vector3.up;
        return Vector3.right;
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
        Bounds bounds;
        if (!TryGetVisualBounds(true, out bounds))
            return 2f;
        return Mathf.Max(1f, bounds.size.y + 0.2f);
    }

    void OnDestroy()
    {
        StopFireLoop();
        StopWorkFx();
        if (assignedCctv != null)
            assignedCctv.UnregisterTurret(this);
    }

    void CacheVisualRests()
    {
        if (upgradeTiers == null)
        {
            visualRestLocal = null;
            return;
        }

        visualRestLocal = new Vector3[upgradeTiers.Length];
        for (int i = 0; i < upgradeTiers.Length; i++)
        {
            GameObject root = upgradeTiers[i] != null ? upgradeTiers[i].visualRoot : null;
            visualRestLocal[i] = root != null ? root.transform.localPosition : Vector3.zero;
        }
    }

    Vector3 RestLocalOf(GameObject root)
    {
        if (root == null)
            return Vector3.zero;
        if (visualRestLocal != null && upgradeTiers != null)
        {
            for (int i = 0; i < upgradeTiers.Length && i < visualRestLocal.Length; i++)
            {
                if (upgradeTiers[i] != null && upgradeTiers[i].visualRoot == root)
                    return visualRestLocal[i];
            }
        }

        return root.transform.localPosition;
    }

    Vector3 BuriedLocalOf(GameObject root, Vector3 rest)
    {
        if (root == null)
            return rest;
        Transform parent = root.transform.parent;
        float height = GetVisualRiseHeight(root);
        Vector3 down = parent != null
            ? parent.InverseTransformVector(Vector3.down * height)
            : Vector3.down * height;
        return rest + down;
    }

    void RestoreVisualRests()
    {
        if (upgradeTiers == null)
            return;

        for (int i = 0; i < upgradeTiers.Length; i++)
        {
            GameObject root = upgradeTiers[i] != null ? upgradeTiers[i].visualRoot : null;
            if (root == null)
                continue;
            root.transform.localPosition = RestLocalOf(root);
        }
    }

    void BeginUpgradeSwap()
    {
        upgradeOutgoing = ResolveVisibleRoot(GetTier(upgradeLevel));
        upgradeIncoming = ResolveVisibleRoot(GetTier(upgradeLevel + 1));
        upgradeOutRest = RestLocalOf(upgradeOutgoing);
        upgradeInRest = RestLocalOf(upgradeIncoming);
        upgradeOutBuried = BuriedLocalOf(upgradeOutgoing, upgradeOutRest);
        upgradeInBuried = BuriedLocalOf(upgradeIncoming, upgradeInRest);

        if (upgradeOutgoing != null)
        {
            SetVisualActive(upgradeOutgoing, true);
            upgradeOutgoing.transform.localPosition = upgradeOutRest;
        }

        if (upgradeIncoming != null && upgradeIncoming != upgradeOutgoing)
        {
            SetVisualActive(upgradeIncoming, false);
            upgradeIncoming.transform.localPosition = upgradeInBuried;
        }
    }

    void AnimateUpgradeSwap(float t)
    {
        t = Mathf.Clamp01(t);
        if (upgradeIncoming == null && upgradeOutgoing == null)
            return;

        GameObject outgoing = upgradeOutgoing;
        GameObject incoming = upgradeIncoming != null ? upgradeIncoming : upgradeOutgoing;
        bool same = incoming == outgoing || outgoing == null;

        float sinkT = Smooth01(Mathf.Clamp01(t * 2f));
        float riseT = Smooth01(Mathf.Clamp01(t * 2f - 1f));

        if (same)
        {
            if (incoming == null)
                return;
            incoming.transform.localPosition = t < 0.5f
                ? Vector3.Lerp(upgradeOutRest, upgradeOutBuried, sinkT)
                : Vector3.Lerp(upgradeInBuried, upgradeInRest, riseT);
            return;
        }

        if (t < 0.5f)
        {
            SetVisualActive(outgoing, true);
            SetVisualActive(incoming, false);
            outgoing.transform.localPosition = Vector3.Lerp(upgradeOutRest, upgradeOutBuried, sinkT);
        }
        else
        {
            if (outgoing != null)
            {
                outgoing.transform.localPosition = upgradeOutRest;
                SetVisualActive(outgoing, false);
            }

            SetVisualActive(incoming, true);
            incoming.transform.localPosition = Vector3.Lerp(upgradeInBuried, upgradeInRest, riseT);
        }
    }

    void SetVisualActive(GameObject root, bool on)
    {
        if (root == null || root == gameObject)
            return;
        if (root.activeSelf != on)
            root.SetActive(on);
    }

    static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    void PlayUpgradeStartSfx()
    {
        if (playedUpgradeStartSfx)
            return;
        playedUpgradeStartSfx = true;
        PlaySfx(upgradeStart);
    }

    void StartWorkFx()
    {
        if (workFxInstance != null)
            return;

        GameObject prefab = ResolveWorkFxPrefab();
        if (prefab == null)
            return;

        Transform parent = transform.parent != null ? transform.parent : transform;
        Vector3 world = parent.TransformPoint(builtLocalPosition);
        workFxInstance = CombatVfx.PlayLoop(prefab, world, parent);
    }

    void StopWorkFx()
    {
        if (workFxInstance == null)
            return;
        CombatVfx.StopLoop(workFxInstance);
        workFxInstance = null;
    }

    GameObject ResolveWorkFxPrefab()
    {
        if (workLoopFx != null)
            return workLoopFx;

        TurretPlatformPlacer placer = FindFirstObjectByType<TurretPlatformPlacer>();
        return placer != null ? placer.turretWorkLoopFx : null;
    }

    float GetVisualRiseHeight(GameObject root)
    {
        if (root == null)
            return GetRiseHeight();

        float height = 0f;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                Vector3 size = Vector3.Scale(filter.sharedMesh.bounds.size, renderer.transform.lossyScale);
                height = Mathf.Max(height, size.y);
                continue;
            }

            height = Mathf.Max(height, renderer.bounds.size.y);
        }

        return Mathf.Max(1f, height + 0.2f);
    }
}
