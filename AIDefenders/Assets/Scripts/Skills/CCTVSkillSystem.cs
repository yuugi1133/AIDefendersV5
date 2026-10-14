using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CCTVSkillSystem : MonoBehaviour
{
    public const int SlotCount = 4;

    [Tooltip("이 스테이지에서 쓸 액티브 스킬. 최대 4칸, 스테이지 중에는 바뀌지 않는다.")]
    public CCTVActiveSkill[] equippedSkills = new CCTVActiveSkill[SlotCount];

    public float crosshairSize = 14f;
    public float crosshairThickness = 2f;
    public Color crosshairColor = new Color(0.95f, 0.95f, 0.9f, 0.9f);

    public float slotSize = 72f;
    public float slotGap = 8f;
    public float slotBottomMargin = 28f;

    static readonly KeyCode[] SlotKeys =
    {
        KeyCode.Q,
        KeyCode.E,
        KeyCode.R,
        KeyCode.F
    };

    public static readonly string[] SlotKeyLabels = { "Q", "E", "R", "F" };

    WaveDirector waveDirector;
    CCTVController cctv;
    TurretPlatformPlacer buildPlacer;
    Camera viewCamera;

    void Start()
    {
        waveDirector = FindFirstObjectByType<WaveDirector>();
        cctv = GetComponent<CCTVController>();
        if (cctv == null)
            cctv = FindFirstObjectByType<CCTVController>();
        buildPlacer = FindFirstObjectByType<TurretPlatformPlacer>();
    }

    void Update()
    {
        if (!IsHudActive())
            return;

        if (buildPlacer != null && buildPlacer.BlocksPlayerLook)
            return;

        for (int i = 0; i < SlotCount; i++)
        {
            if (WasSlotPressed(i))
                TryUseSlot(i);
        }
    }

    public bool ShouldDrawHud
    {
        get
        {
            if (!IsHudActive())
                return false;
            if (buildPlacer == null)
                buildPlacer = FindFirstObjectByType<TurretPlatformPlacer>();
            return buildPlacer == null || !buildPlacer.BlocksPlayerLook;
        }
    }

    public bool HasPrimedSkill()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            CCTVActiveSkill skill = GetEquipped(i);
            if (skill != null && skill.IsPrimed)
                return true;
        }

        return false;
    }

    public void TryUseSlot(int index)
    {
        if (!IsHudActive())
            return;
        if (buildPlacer != null && buildPlacer.BlocksPlayerLook)
            return;

        CCTVActiveSkill skill = GetEquipped(index);
        if (skill == null)
            return;

        skill.TryUse(BuildContext());
    }

    public bool IsHudActive()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();

        if (waveDirector == null)
            return false;

        return waveDirector.Phase == WavePhase.Prep || waveDirector.Phase == WavePhase.Combat;
    }

    public CCTVActiveSkill GetEquipped(int slot)
    {
        if (equippedSkills == null || slot < 0 || slot >= equippedSkills.Length)
            return null;
        return equippedSkills[slot];
    }

    CCTVSkillContext BuildContext()
    {
        Camera camera = GetViewCamera();
        var context = new CCTVSkillContext();
        context.Cctv = cctv;
        context.ViewCamera = camera;
        context.Origin = transform;
        if (camera != null)
            context.AimRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return context;
    }

    Camera GetViewCamera()
    {
        if (viewCamera != null && viewCamera.enabled && viewCamera.targetTexture == null)
            return viewCamera;

        Camera best = null;
        float bestDepth = float.MinValue;
        Camera[] cameras = GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || !camera.enabled || !camera.gameObject.activeInHierarchy)
                continue;
            if (camera.targetTexture != null)
                continue;
            if (best != null && camera.depth < bestDepth)
                continue;
            best = camera;
            bestDepth = camera.depth;
        }

        viewCamera = best;
        return viewCamera;
    }

    static bool WasSlotPressed(int index)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            switch (index)
            {
                case 0: return keyboard.qKey.wasPressedThisFrame;
                case 1: return keyboard.eKey.wasPressedThisFrame;
                case 2: return keyboard.rKey.wasPressedThisFrame;
                case 3: return keyboard.fKey.wasPressedThisFrame;
            }
        }
#endif
        if (index < 0 || index >= SlotKeys.Length)
            return false;
#if ENABLE_INPUT_SYSTEM
        return false;
#else
        return Input.GetKeyDown(SlotKeys[index]);
#endif
    }
}
