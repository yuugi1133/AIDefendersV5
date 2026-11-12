using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CCTVSkill_IdentificationDart : CCTVActiveSkill
{
    protected override string DefaultIconResource
    {
        get { return "UI/SkillIcons/IdentificationDart"; }
    }

    public GameObject dartPrefab;
    public float dartSpeed = 40f;
    public float dartGravity = 0f;
    public float dartLifetime = 5f;
    public float muzzleOffset = 1.6f;
    public float homingCone = 16f;
    public float homingTurnRate = 55f;
    public float homingAimBias = 0.2f;
    public float homingRange = 90f;
    public SfxEvent fireSfx;

    bool primed;
    CCTVSkillContext armedContext;
    TurretPlatformPlacer buildPlacer;

    public override bool IsPrimed
    {
        get { return primed; }
    }

    void Start()
    {
        buildPlacer = FindFirstObjectByType<TurretPlatformPlacer>();
    }

    protected override void OnUse(CCTVSkillContext context)
    {
        if (primed)
        {
            CancelPrime();
            return;
        }

        primed = true;
        armedContext = context;
    }

    protected override void Update()
    {
        base.Update();

        if (!primed)
            return;

        if (buildPlacer != null && buildPlacer.BlocksPlayerLook)
        {
            CancelPrime();
            return;
        }

        RefreshAim();

        if (WasCancelPressed())
        {
            CancelPrime();
            return;
        }

        if (WasFirePressed(0))
        {
            Fire(true);
            return;
        }

        if (WasFirePressed(1))
            Fire(false);
    }

    void RefreshAim()
    {
        if (armedContext == null || armedContext.ViewCamera == null)
            return;

        armedContext.AimRay = armedContext.ViewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        armedContext.Origin = transform;
    }

    void Fire(bool asEnemy)
    {
        if (dartPrefab == null)
        {
            Debug.LogWarning("식별 다트 프리팹이 비어 있습니다.");
            CancelPrime();
            return;
        }

        RefreshAim();

        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        if (armedContext != null && armedContext.ViewCamera != null)
        {
            origin = armedContext.AimRay.origin + armedContext.AimRay.direction * muzzleOffset;
            direction = armedContext.AimRay.direction;
        }
        else if (armedContext != null)
        {
            origin = armedContext.AimRay.origin + armedContext.AimRay.direction * muzzleOffset;
            direction = armedContext.AimRay.direction;
        }

        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;
        direction.Normalize();

        Target_Base guide = FindTargetNearCrosshair(origin, direction);
        if (guide != null && homingAimBias > 0f)
        {
            Vector3 toTarget = guide.GetAimPoint(origin) - origin;
            if (toTarget.sqrMagnitude > 0.0001f)
                direction = Vector3.Slerp(direction, toTarget.normalized, Mathf.Clamp01(homingAimBias)).normalized;
        }

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject spawned = Instantiate(dartPrefab, origin, rotation);
        IdentificationDart dart = spawned.GetComponent<IdentificationDart>();
        if (dart == null)
            dart = spawned.AddComponent<IdentificationDart>();

        CCTVController cctv = armedContext != null ? armedContext.Cctv : GetComponent<CCTVController>();
        dart.Init(
            direction * dartSpeed,
            dartGravity,
            dartLifetime,
            cctv,
            asEnemy,
            transform,
            guide,
            homingTurnRate
        );
        GameSfx.PlayWorld(fireSfx, origin);

        primed = false;
        armedContext = null;
        StartCooldown();
    }

    Target_Base FindTargetNearCrosshair(Vector3 origin, Vector3 aimDir)
    {
        Target_Base[] targets = Object.FindObjectsByType<Target_Base>(FindObjectsSortMode.None);
        Target_Base best = null;
        float cone = Mathf.Max(1f, homingCone);
        float maxRange = homingRange > 0.01f ? homingRange : 90f;
        float bestAngle = cone;
        float bestDist = maxRange;

        for (int i = 0; i < targets.Length; i++)
        {
            Target_Base target = targets[i];
            if (target == null || target.isDead || target.isSample)
                continue;

            Vector3 toTarget = target.GetAimPoint(origin) - origin;
            float dist = toTarget.magnitude;
            if (dist < 0.4f || dist > maxRange)
                continue;

            float angle = Vector3.Angle(aimDir, toTarget);
            if (angle > cone)
                continue;
            if (best != null && angle > bestAngle + 0.01f)
                continue;
            if (best != null && angle >= bestAngle - 0.01f && dist >= bestDist)
                continue;

            best = target;
            bestAngle = angle;
            bestDist = dist;
        }

        return best;
    }

    void CancelPrime()
    {
        primed = false;
        armedContext = null;
    }

    static bool WasFirePressed(int button)
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        if (button == 0)
            return mouse.leftButton.wasPressedThisFrame;
        if (button == 1)
            return mouse.rightButton.wasPressedThisFrame;
        return false;
#else
        return Input.GetMouseButtonDown(button);
#endif
    }

    static bool WasCancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
