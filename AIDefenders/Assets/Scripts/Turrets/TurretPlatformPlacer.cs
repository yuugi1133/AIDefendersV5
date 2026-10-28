using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TurretPlatformPlacer : MonoBehaviour
{
    public GameObject platformPrefab;
    public TurretBuildOption[] turretOptions;
    public LayerMask groundMask;
    public float placeHeightOffset = 0.5f;
    public float overlapPadding = 0.9f;
    public int platformBaseCost = 40;
    public int platformMaxCount = 8;
    public int platformSellPrice = 5;

    WaveDirector waveDirector;
    CCTVController cctv;
    CCTVDroneController drone;
    GameObject ghost;
    GameObject draggingPrefab;
    TurretPlatform hoverPlatform;
    bool draggingPlatform;
    bool draggingTurret;
    bool canPlace;
    int pendingTurretCost;
    int pendingTurretSellPrice;
    float pendingTurretBuildTime;
    string pendingTurretDisplayName;
    int pendingTurretUpgradeLevel;
    StoredTurret pendingStored;
    readonly List<StoredTurret> storedTurrets = new List<StoredTurret>();
    readonly List<RaycastResult> uiRaycasts = new List<RaycastResult>();
    bool combatBuildOpen;
    TurretController selectedTurret;
    TurretPlatform selectedPlatform;
    Renderer[] highlightRenderers;
    Color[][] highlightOriginalColors;
    Color validColor = new Color(0.35f, 0.85f, 0.4f, 1f);
    Color invalidColor = new Color(0.9f, 0.25f, 0.2f, 1f);
    Color highlightColor = new Color(1f, 0.82f, 0.22f, 1f);
    readonly TurretRangeRing rangeRing = new TurretRangeRing();

    public bool BlocksPlayerLook
    {
        get { return ShowBuildUi; }
    }

    public bool ShowBuildUi
    {
        get { return IsHudPhase && combatBuildOpen; }
    }

    public bool IsHudPhase
    {
        get { return IsPrep() || IsCombat(); }
    }

    public bool IsPrepPhase
    {
        get { return IsPrep(); }
    }

    public bool IsDragging
    {
        get { return Dragging; }
    }

    public bool IsDraggingPlatform
    {
        get { return draggingPlatform; }
    }

    public GameObject DraggingPrefab
    {
        get { return draggingPrefab; }
    }

    public TurretController SelectedTurret
    {
        get { return selectedTurret; }
    }

    public TurretPlatform SelectedPlatform
    {
        get { return selectedPlatform; }
    }

    public int PlatformCount
    {
        get { return CountPlatforms(); }
    }

    public int PlatformMaxCount
    {
        get { return Mathf.Max(0, platformMaxCount); }
    }

    public IReadOnlyList<StoredTurret> Stored
    {
        get { return storedTurrets; }
    }

    public int PlatformCost
    {
        get { return GetPlatformCost(); }
    }

    public bool HasPlatformPrefab
    {
        get { return platformPrefab != null; }
    }

    bool Dragging
    {
        get { return draggingPlatform || draggingTurret; }
    }

    void Start()
    {
        waveDirector = FindFirstObjectByType<WaveDirector>();
        cctv = FindFirstObjectByType<CCTVController>();
        drone = FindFirstObjectByType<CCTVDroneController>();
        if (platformPrefab == null)
            Debug.LogWarning("TurretPlatformPlacer: Platform Prefab이 비어 있습니다.");

        if (waveDirector != null)
        {
            waveDirector.OnPrepStarted += HandlePrepStarted;
            waveDirector.OnWaveStarted += HandleWaveStarted;
        }
    }

    void OnDestroy()
    {
        ClearSelection();
        rangeRing.Dispose();

        if (waveDirector == null)
            return;

        waveDirector.OnPrepStarted -= HandlePrepStarted;
        waveDirector.OnWaveStarted -= HandleWaveStarted;
    }

    void HandlePrepStarted(int waveIndex, float duration)
    {
        CancelPlace();
    }

    void HandleWaveStarted(int waveIndex)
    {
        CancelPlace();
    }

    void Update()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();
        if (cctv == null)
            cctv = FindFirstObjectByType<CCTVController>();
        if (drone == null)
            drone = FindFirstObjectByType<CCTVDroneController>();

        HandleCombatBuildToggle();

        if (!IsPrep() && draggingPlatform)
            CancelPlace();

        if (!ShowBuildUi)
        {
            if (Dragging || HasSelection)
            {
                ClearSelection();
                CancelPlace();
            }
            rangeRing.Hide();
            return;
        }

        if (WasCancelPressed())
        {
            if (Dragging)
            {
                CancelPlace();
                return;
            }

            if (HasSelection)
            {
                ClearSelection();
                return;
            }
        }

        if (!Dragging && WasLeftPressed() && !IsPointerOverBuildUi())
            TrySelectClickedTurret();

        if (Dragging)
        {
            if (draggingPlatform)
                UpdatePlatformGhost();
            else
                UpdateTurretGhost();

            if (WasLeftReleased())
            {
                if (canPlace)
                    ConfirmPlace();
                else
                    CancelPlace();
            }
        }

        TickRangeRing();
    }

    public void ToggleBuildMenu()
    {
        if (!IsHudPhase)
            return;

        combatBuildOpen = !combatBuildOpen;
        ClearSelection();
        CancelPlace();
    }

    public void RequestPlatformPlace()
    {
        if (!Dragging)
            BeginPlatformPlace();
    }

    public void RequestTurretPlace(TurretBuildOption option)
    {
        if (!Dragging)
            BeginTurretPlace(option);
    }

    public void RequestStoredPlace(int index)
    {
        if (!Dragging)
            BeginStoredPlace(index);
    }

    public void RequestSellSelected()
    {
        if (selectedTurret != null)
            TrySellSelectedTurret();
        else
            TrySellSelectedPlatform();
    }

    public void RequestUpgradeSelected()
    {
        TryUpgradeSelectedTurret();
    }

    public void RequestStoreSelected()
    {
        TryStoreSelectedTurret();
    }

    public int GetSelectedSellPrice()
    {
        if (selectedTurret != null)
            return GetSellPrice(selectedTurret);
        if (selectedPlatform != null)
            return Mathf.Max(0, platformSellPrice);
        return 0;
    }

    public Vector3 GetInspectAnchor()
    {
        if (selectedTurret != null)
            return selectedTurret.GetInspectAnchor();
        if (selectedPlatform != null)
            return GetPlatformInspectAnchor(selectedPlatform);
        return Vector3.zero;
    }

    public Camera GetHudCamera()
    {
        return GetPlaceCamera();
    }

    public static string AmmoStatus(TurretController turret)
    {
        return GetAmmoStatus(turret);
    }

    public static string StateLabel(TurretController turret)
    {
        return GetTurretStateLabel(turret);
    }

    bool IsPrep()
    {
        return waveDirector != null && waveDirector.Phase == WavePhase.Prep;
    }

    bool IsCombat()
    {
        return waveDirector != null && waveDirector.Phase == WavePhase.Combat;
    }

    void HandleCombatBuildToggle()
    {
        if (!IsHudPhase)
        {
            combatBuildOpen = false;
            return;
        }

        if (WasTabPressed())
            ToggleBuildMenu();
    }

    void BeginPlatformPlace()
    {
        if (!IsPrep())
            return;

        ClearSelection();
        BeginGhost(platformPrefab, true);
    }

    void BeginTurretPlace(TurretBuildOption option)
    {
        if (option == null)
            return;

        ClearSelection();
        pendingTurretCost = option.cost;
        pendingTurretSellPrice = option.sellPrice;
        pendingTurretBuildTime = option.buildTime;
        pendingTurretDisplayName = option.Label;
        pendingTurretUpgradeLevel = 1;
        pendingStored = null;
        BeginGhost(option.prefab, false);
    }

    void BeginStoredPlace(int index)
    {
        if (index < 0 || index >= storedTurrets.Count)
            return;

        StoredTurret stored = storedTurrets[index];
        if (stored == null || stored.prefab == null)
            return;

        ClearSelection();
        storedTurrets.RemoveAt(index);
        pendingStored = stored;
        pendingTurretCost = 0;
        pendingTurretSellPrice = stored.sellPrice;
        pendingTurretBuildTime = stored.buildTime;
        pendingTurretDisplayName = stored.displayName;
        pendingTurretUpgradeLevel = stored.upgradeLevel;
        BeginGhost(stored.prefab, false);

        if (ghost == null)
        {
            storedTurrets.Add(stored);
            pendingStored = null;
        }
    }

    void BeginGhost(GameObject prefab, bool platform)
    {
        if (prefab == null || Dragging)
            return;

        ghost = Instantiate(prefab);
        ghost.name = prefab.name + " (미리보기)";
        draggingPrefab = prefab;
        draggingPlatform = platform;
        draggingTurret = !platform;
        hoverPlatform = null;
        canPlace = false;

        Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        TurretController[] ghostTurrets = ghost.GetComponentsInChildren<TurretController>(true);
        for (int i = 0; i < ghostTurrets.Length; i++)
            ghostTurrets[i].enabled = false;

        WeaponSystem[] ghostWeapons = ghost.GetComponentsInChildren<WeaponSystem>(true);
        for (int i = 0; i < ghostWeapons.Length; i++)
            ghostWeapons[i].enabled = false;

        if (platform)
            UpdatePlatformGhost();
        else
            UpdateTurretGhost();
    }

    void UpdatePlatformGhost()
    {
        if (ghost == null)
            return;

        if (IsPointerOverBuildUi())
        {
            SetGhostInvalidHidden();
            return;
        }

        if (!TryRaycastGround(out RaycastHit hit, skipPlatforms: true))
        {
            SetGhostInvalidHidden();
            return;
        }

        ghost.SetActive(true);
        Vector3 pos = hit.point + Vector3.up * placeHeightOffset;
        ghost.transform.SetParent(null);
        ghost.transform.position = pos;
        ghost.transform.rotation = Quaternion.identity;
        canPlace = !IsOccupied(pos, ghost.transform.rotation)
            && CanAfford(GetPlatformCost())
            && CountPlatforms() < PlatformMaxCount;
        TintGhost(canPlace ? validColor : invalidColor);
    }

    void UpdateTurretGhost()
    {
        if (ghost == null)
            return;

        if (IsPointerOverBuildUi())
        {
            SetGhostInvalidHidden();
            return;
        }

        hoverPlatform = FindHoveredPlatform();
        if (hoverPlatform != null)
        {
            ghost.SetActive(true);
            ghost.transform.SetParent(null);
            ghost.transform.position = hoverPlatform.GetMountPosition();
            ghost.transform.rotation = hoverPlatform.transform.rotation;
            canPlace = hoverPlatform.IsEmpty && CanAfford(pendingTurretCost);
            TintGhost(canPlace ? validColor : invalidColor);
            return;
        }

        if (TryRaycastGround(out RaycastHit hit, skipPlatforms: false))
        {
            ghost.SetActive(true);
            ghost.transform.SetParent(null);
            ghost.transform.position = hit.point + Vector3.up * placeHeightOffset;
            ghost.transform.rotation = Quaternion.identity;
        }
        else
        {
            ghost.SetActive(false);
        }

        canPlace = false;
        TintGhost(invalidColor);
    }

    void SetGhostInvalidHidden()
    {
        canPlace = false;
        hoverPlatform = null;
        if (ghost != null)
            ghost.SetActive(false);
    }

    void ConfirmPlace()
    {
        if (ghost == null || draggingPrefab == null)
        {
            CancelPlace();
            return;
        }

        if (draggingTurret)
        {
            ConfirmTurret();
            return;
        }

        Vector3 pos = ghost.transform.position;
        Quaternion rot = ghost.transform.rotation;
        int cost = GetPlatformCost();
        if (CountPlatforms() >= PlatformMaxCount || !TrySpend(cost))
        {
            CancelPlace();
            return;
        }

        ClearGhost();

        GameObject placed = Instantiate(platformPrefab, pos, rot);
        if (placed.GetComponent<TurretPlatform>() == null)
            placed.AddComponent<TurretPlatform>();
        GameSfx.PlayPlatformPlaced();
    }

    void ConfirmTurret()
    {
        TurretPlatform platform = hoverPlatform;
        GameObject prefab = draggingPrefab;
        if (platform == null || !platform.IsEmpty || prefab == null)
        {
            CancelPlace();
            return;
        }

        Vector3 pos = platform.GetMountPosition();
        Quaternion rot = platform.transform.rotation;
        int cost = pendingTurretCost;
        int sellPrice = pendingTurretSellPrice;
        float buildTime = pendingTurretBuildTime;
        string displayName = pendingTurretDisplayName;
        int upgradeLevel = pendingTurretUpgradeLevel;
        GameObject sourcePrefab = prefab;
        if (!TrySpend(cost))
        {
            CancelPlace();
            return;
        }

        pendingStored = null;
        ClearGhost();

        GameObject placed = Instantiate(prefab, pos, rot, platform.transform);

        TurretController turret = placed.GetComponentInChildren<TurretController>();
        if (turret != null)
        {
            turret.sellPrice = sellPrice;
            turret.sourcePrefab = sourcePrefab;
            turret.upgradeLevel = Mathf.Max(1, upgradeLevel);
            if (!string.IsNullOrWhiteSpace(displayName))
                turret.displayName = displayName;
        }

        platform.Mount(turret);

        if (turret != null)
            turret.BeginConstruction(buildTime, IsPrep(), cctv);
    }

    void TickRangeRing()
    {
        if (draggingTurret)
        {
            float radius = TurretController.PreviewRange(draggingPrefab, pendingTurretUpgradeLevel);
            if (radius < 0.2f && ghost != null)
                radius = TurretController.PreviewRange(ghost, pendingTurretUpgradeLevel);

            Vector3 center;
            if (!TryGetRangeCenter(out center))
            {
                rangeRing.Hide();
                return;
            }

            Color color = canPlace ? validColor : invalidColor;
            rangeRing.Show(center, radius, color);
            return;
        }

        if (selectedTurret != null)
        {
            rangeRing.Show(selectedTurret.transform.position, selectedTurret.range, highlightColor);
            return;
        }

        rangeRing.Hide();
    }

    bool TryGetRangeCenter(out Vector3 center)
    {
        center = Vector3.zero;
        if (ghost != null && ghost.activeSelf)
        {
            center = ghost.transform.position;
            return true;
        }

        if (IsPointerOverBuildUi())
            return false;

        if (!TryRaycastGround(out RaycastHit hit, skipPlatforms: false))
            return false;

        center = hit.point;
        return true;
    }

    void CancelPlace()
    {
        ClearGhost();
    }

    void ClearGhost()
    {
        if (ghost != null)
            Destroy(ghost);

        if (pendingStored != null)
        {
            storedTurrets.Add(pendingStored);
            pendingStored = null;
        }

        ghost = null;
        draggingPrefab = null;
        hoverPlatform = null;
        draggingPlatform = false;
        draggingTurret = false;
        canPlace = false;
        pendingTurretCost = 0;
        pendingTurretSellPrice = 0;
        pendingTurretBuildTime = 0f;
        pendingTurretDisplayName = "";
        pendingTurretUpgradeLevel = 1;
    }

    bool HasSelection
    {
        get { return selectedTurret != null || selectedPlatform != null; }
    }

    void TrySelectClickedTurret()
    {
        FindHoveredBuildTarget(out TurretController turret, out TurretPlatform platform);
        if (turret == null && platform == null)
        {
            ClearSelection();
            return;
        }

        if (turret != null)
            SelectTurret(turret);
        else
            SelectPlatform(platform);
    }

    void SelectTurret(TurretController turret)
    {
        if (turret == null)
        {
            ClearSelection();
            return;
        }

        TurretPlatform platform = turret.GetComponentInParent<TurretPlatform>();
        if (platform != null && platform.mountedTurret != null)
            turret = platform.mountedTurret;

        SetSelection(turret, platform);
    }

    void SelectPlatform(TurretPlatform platform)
    {
        if (platform == null)
        {
            ClearSelection();
            return;
        }

        if (platform.mountedTurret != null)
        {
            SelectTurret(platform.mountedTurret);
            return;
        }

        SetSelection(null, platform);
    }

    void SetSelection(TurretController turret, TurretPlatform platform)
    {
        if (selectedTurret == turret && selectedPlatform == platform)
            return;

        ClearHighlight();
        selectedTurret = turret;
        selectedPlatform = platform;
        if (turret != null && platform != null)
            ApplyHighlight(turret.transform, platform.transform);
        else if (turret != null)
            ApplyHighlight(turret.transform);
        else if (platform != null)
            ApplyHighlight(platform.transform);
    }

    void ClearSelection()
    {
        ClearHighlight();
        selectedTurret = null;
        selectedPlatform = null;
    }

    void ApplyHighlight(params Transform[] roots)
    {
        List<Renderer> renderers = new List<Renderer>();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null)
                continue;

            Renderer[] found = roots[i].GetComponentsInChildren<Renderer>();
            for (int r = 0; r < found.Length; r++)
            {
                if (found[r] != null && !renderers.Contains(found[r]))
                    renderers.Add(found[r]);
            }
        }

        if (renderers.Count == 0)
            return;

        highlightRenderers = renderers.ToArray();
        highlightOriginalColors = new Color[highlightRenderers.Length][];
        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            Renderer renderer = highlightRenderers[i];
            if (renderer == null)
                continue;

            Material[] materials = renderer.materials;
            highlightOriginalColors[i] = new Color[materials.Length];
            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] == null)
                    continue;

                highlightOriginalColors[i][m] = ReadMaterialColor(materials[m]);
                WriteMaterialColor(materials[m], highlightColor);
            }
        }
    }

    void ClearHighlight()
    {
        if (highlightRenderers == null || highlightOriginalColors == null)
        {
            highlightRenderers = null;
            highlightOriginalColors = null;
            return;
        }

        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            Renderer renderer = highlightRenderers[i];
            if (renderer == null || highlightOriginalColors[i] == null)
                continue;

            Material[] materials = renderer.materials;
            for (int m = 0; m < materials.Length && m < highlightOriginalColors[i].Length; m++)
            {
                if (materials[m] != null)
                    WriteMaterialColor(materials[m], highlightOriginalColors[i][m]);
            }
        }

        highlightRenderers = null;
        highlightOriginalColors = null;
    }

    static Color ReadMaterialColor(Material material)
    {
        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color"))
            return material.GetColor("_Color");
        return Color.white;
    }

    static void WriteMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    void TrySellSelectedTurret()
    {
        TurretController turret = selectedTurret;
        if (turret == null)
            return;

        TurretPlatform platform = turret.GetComponentInParent<TurretPlatform>();
        if (platform == null)
            return;

        if (platform.mountedTurret != null)
            turret = platform.mountedTurret;

        int refund = GetSellPrice(turret);
        if (waveDirector != null && refund > 0)
            waveDirector.AddFunds(refund);

        RemoveMountedTurret(turret, platform);
    }

    void TrySellSelectedPlatform()
    {
        TurretPlatform platform = selectedPlatform;
        if (platform == null || platform.mountedTurret != null)
            return;

        int refund = Mathf.Max(0, platformSellPrice);
        if (waveDirector != null && refund > 0)
            waveDirector.AddFunds(refund);

        ClearSelection();
        Destroy(platform.gameObject);
    }

    void TryStoreSelectedTurret()
    {
        TurretController turret = selectedTurret;
        if (turret == null)
            return;

        TurretPlatform platform = turret.GetComponentInParent<TurretPlatform>();
        if (platform == null)
            return;

        if (platform.mountedTurret != null)
            turret = platform.mountedTurret;

        GameObject prefab = GetSourcePrefab(turret);
        if (prefab == null)
        {
            Debug.LogWarning("보관할 포탑 프리팹을 찾지 못했습니다.");
            return;
        }

        var stored = new StoredTurret();
        stored.prefab = prefab;
        stored.displayName = turret.TypeLabel;
        stored.sellPrice = Mathf.Max(0, turret.sellPrice);
        stored.buildTime = GetBuildTime(turret);
        stored.upgradeLevel = Mathf.Max(1, turret.upgradeLevel);
        storedTurrets.Add(stored);

        RemoveMountedTurret(turret, platform);
    }

    void RemoveMountedTurret(TurretController turret, TurretPlatform platform)
    {
        if (turret == null || platform == null)
            return;

        if (cctv != null)
            cctv.UnregisterTurret(turret);

        Transform instance = turret.transform;
        while (instance.parent != null && instance.parent != platform.transform)
            instance = instance.parent;

        turret.PlayDismantleSfx();
        ClearSelection();
        Destroy(instance.gameObject);
        platform.Mount(null);
    }

    static string GetAmmoStatus(TurretController turret)
    {
        if (turret == null)
            return "-";

        WeaponSystem weapon = turret.weapon;
        if (weapon == null)
            weapon = turret.GetComponentInChildren<WeaponSystem>();
        if (weapon == null)
            return "없음";

        if (weapon.IsReloading)
            return weapon.AmmoLabel + "  재장전 중";

        return weapon.AmmoLabel + "  " + weapon.CurrentAmmo + "/" + weapon.MagazineSize;
    }

    static string GetTurretStateLabel(TurretController turret)
    {
        if (turret == null)
            return "-";
        if (turret.IsConstructing)
            return "건설 중";
        if (turret.IsUpgrading)
            return "업그레이드 중";
        if (!turret.IsReady)
            return "대기";
        return "가동";
    }

    void TryUpgradeSelectedTurret()
    {
        TurretController turret = selectedTurret;
        if (turret == null || !turret.CanUpgrade())
            return;

        int cost = turret.NextUpgradeCost();
        if (!TrySpend(cost))
            return;

        turret.BeginUpgrade(IsPrep());
    }

    int GetSellPrice(TurretController turret)
    {
        if (turret == null)
            return 0;

        int value = turret.GetSellValue();
        if (value > 0)
            return value;

        if (turretOptions == null)
            return 0;

        Transform named = turret.transform;
        while (named.parent != null && named.parent.GetComponent<TurretPlatform>() == null)
            named = named.parent;

        string turretName = named.name;

        for (int i = 0; i < turretOptions.Length; i++)
        {
            TurretBuildOption option = turretOptions[i];
            if (option == null || option.prefab == null)
                continue;

            if (turretName.StartsWith(option.prefab.name) ||
                turret.name.StartsWith(option.prefab.name))
            {
                return Mathf.Max(0, option.sellPrice);
            }
        }

        return 0;
    }

    GameObject GetSourcePrefab(TurretController turret)
    {
        if (turret == null)
            return null;

        if (turret.sourcePrefab != null)
            return turret.sourcePrefab;

        TurretBuildOption option = FindBuildOption(turret);
        return option != null ? option.prefab : null;
    }

    float GetBuildTime(TurretController turret)
    {
        TurretBuildOption option = FindBuildOption(turret);
        if (option != null)
            return option.buildTime;
        return 3f;
    }

    TurretBuildOption FindBuildOption(TurretController turret)
    {
        if (turret == null || turretOptions == null)
            return null;

        if (turret.sourcePrefab != null)
        {
            for (int i = 0; i < turretOptions.Length; i++)
            {
                TurretBuildOption option = turretOptions[i];
                if (option != null && option.prefab == turret.sourcePrefab)
                    return option;
            }
        }

        Transform named = turret.transform;
        while (named.parent != null && named.parent.GetComponent<TurretPlatform>() == null)
            named = named.parent;

        string turretName = named.name;
        for (int i = 0; i < turretOptions.Length; i++)
        {
            TurretBuildOption option = turretOptions[i];
            if (option == null || option.prefab == null)
                continue;

            if (turretName.StartsWith(option.prefab.name) ||
                turret.name.StartsWith(option.prefab.name) ||
                turret.TypeLabel == option.Label)
                return option;
        }

        return null;
    }

    void FindHoveredBuildTarget(out TurretController turret, out TurretPlatform platform)
    {
        turret = null;
        platform = null;
        Camera camera = GetPlaceCamera();
        if (camera == null)
            return;

        Ray ray = camera.ScreenPointToRay(GetMouseScreenPosition());
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            2000f,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            if (collider == null)
                continue;

            turret = collider.GetComponentInParent<TurretController>();
            platform = collider.GetComponentInParent<TurretPlatform>();
            if (turret != null)
            {
                if (platform == null)
                    platform = turret.GetComponentInParent<TurretPlatform>();
                return;
            }

            if (platform != null)
            {
                turret = platform.mountedTurret;
                return;
            }
        }
    }

    static Vector3 GetPlatformInspectAnchor(TurretPlatform platform)
    {
        Collider col = platform.GetComponent<Collider>();
        if (col != null)
            return col.bounds.center + Vector3.up * (col.bounds.extents.y + 0.35f);
        return platform.transform.position + Vector3.up * 1.2f;
    }

    int GetPlatformCost()
    {
        return Mathf.Max(0, platformBaseCost);
    }

    int CountPlatforms()
    {
        TurretPlatform[] platforms = FindObjectsByType<TurretPlatform>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int count = 0;
        for (int i = 0; i < platforms.Length; i++)
        {
            TurretPlatform platform = platforms[i];
            if (platform == null)
                continue;

            if (ghost != null && platform.transform.IsChildOf(ghost.transform))
                continue;

            if (platform.gameObject.name.Contains("미리보기"))
                continue;

            count++;
        }

        return count;
    }

    public bool CanAffordCost(int cost)
    {
        return CanAfford(cost);
    }

    bool CanAfford(int cost)
    {
        if (waveDirector == null)
            return true;

        return waveDirector.CanAfford(cost);
    }

    bool TrySpend(int cost)
    {
        if (waveDirector == null)
            return true;

        return waveDirector.TrySpend(cost);
    }

    TurretPlatform FindHoveredPlatform()
    {
        Camera camera = GetPlaceCamera();
        if (camera == null)
            return null;

        Ray ray = camera.ScreenPointToRay(GetMouseScreenPosition());
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            2000f,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            if (collider == null)
                continue;

            if (ghost != null && collider.transform.IsChildOf(ghost.transform))
                continue;

            TurretPlatform platform = collider.GetComponentInParent<TurretPlatform>();
            if (platform != null)
                return platform;
        }

        return null;
    }

    bool TryRaycastGround(out RaycastHit hit, bool skipPlatforms)
    {
        hit = default;
        Camera camera = GetPlaceCamera();
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(GetMouseScreenPosition());
        int mask = GetGroundMask();
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            2000f,
            mask,
            QueryTriggerInteraction.Ignore
        );

        float closest = float.MaxValue;
        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            if (collider == null)
                continue;

            if (ghost != null && collider.transform.IsChildOf(ghost.transform))
                continue;

            if (skipPlatforms && collider.GetComponentInParent<TurretPlatform>() != null)
                continue;

            if (hits[i].distance < closest)
            {
                closest = hits[i].distance;
                hit = hits[i];
                found = true;
            }
        }

        return found;
    }

    bool IsOccupied(Vector3 position, Quaternion rotation)
    {
        Vector3 extents = GetPlaceExtents() * overlapPadding;
        Collider[] hits = Physics.OverlapBox(
            position,
            extents,
            rotation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i];
            if (collider == null)
                continue;

            if (ghost != null && collider.transform.IsChildOf(ghost.transform))
                continue;

            if (collider.GetComponentInParent<TurretPlatform>() != null)
                return true;
        }

        return false;
    }

    Vector3 GetPlaceExtents()
    {
        BoxCollider box = platformPrefab != null
            ? platformPrefab.GetComponent<BoxCollider>()
            : null;

        if (box != null)
            return Vector3.Scale(box.size * 0.5f, platformPrefab.transform.localScale);

        return Vector3.one * 0.5f;
    }

    void TintGhost(Color color)
    {
        if (ghost == null)
            return;

        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] == null)
                    continue;

                if (materials[m].HasProperty("_BaseColor"))
                    materials[m].SetColor("_BaseColor", color);
                if (materials[m].HasProperty("_Color"))
                    materials[m].SetColor("_Color", color);
            }
        }
    }

    bool IsPointerOverBuildUi()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        PointerEventData pointer = new PointerEventData(eventSystem);
        pointer.position = GetMouseScreenPosition();
        uiRaycasts.Clear();
        eventSystem.RaycastAll(pointer, uiRaycasts);
        return uiRaycasts.Count > 0;
    }

    Camera GetPlaceCamera()
    {
        Camera best = null;
        float bestDepth = float.MinValue;

        if (cctv != null)
            PickScreenCamera(cctv.GetComponentsInChildren<Camera>(true), ref best, ref bestDepth);

        if (drone != null)
            PickScreenCamera(drone.GetComponentsInChildren<Camera>(true), ref best, ref bestDepth);

        if (best != null)
            return best;

        Camera main = Camera.main;
        if (main != null && main.targetTexture == null && main.enabled)
            return main;

        return best;
    }

    static void PickScreenCamera(Camera[] cameras, ref Camera best, ref float bestDepth)
    {
        if (cameras == null)
            return;

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
    }

    int GetGroundMask()
    {
        if (groundMask.value != 0)
            return groundMask.value;

        int mask = Physics.DefaultRaycastLayers;
        mask &= ~LayerMask.GetMask("Enemy", "Friendly", "Turret", "Bullet", "Ignore Raycast");
        return mask;
    }

    Vector3 GetMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif
        return Input.mousePosition;
    }

    bool WasLeftReleased()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.leftButton.wasReleasedThisFrame;
#endif
        return Input.GetMouseButtonUp(0);
    }

    bool WasLeftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.leftButton.wasPressedThisFrame;
#endif
        return Input.GetMouseButtonDown(0);
    }

    bool WasTabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
            return keyboard.tabKey.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(KeyCode.Tab);
    }

    bool WasCancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            return true;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            return true;
        if (mouse != null || keyboard != null)
            return false;
#endif
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
    }
}
