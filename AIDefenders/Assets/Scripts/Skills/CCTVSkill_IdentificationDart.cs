using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CCTVSkill_IdentificationDart : CCTVActiveSkill
{
    public GameObject dartPrefab;
    public float dartSpeed = 40f;
    public float dartGravity = 0f;
    public float dartLifetime = 5f;
    public float muzzleOffset = 1.6f;

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
            transform
        );

        primed = false;
        armedContext = null;
        StartCooldown();
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
