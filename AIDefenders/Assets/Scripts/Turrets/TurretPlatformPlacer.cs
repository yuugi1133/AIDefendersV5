using UnityEngine;
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
    public int platformCostIncrease = 20;

    WaveDirector waveDirector;
    CCTVController cctv;
    GameObject ghost;
    GameObject draggingPrefab;
    TurretPlatform hoverPlatform;
    bool draggingPlatform;
    bool draggingTurret;
    bool canPlace;
    int pendingTurretCost;
    int pendingTurretSellPrice;
    Vector2 turretScroll;
    Rect platformButtonRect;
    Rect turretListRect;
    Rect sellButtonRect;
    Rect combatHeaderRect;
    bool combatBuildOpen;
    bool sellMode;
    Color validColor = new Color(0.35f, 0.85f, 0.4f, 1f);
    Color invalidColor = new Color(0.9f, 0.25f, 0.2f, 1f);

    public bool BlocksPlayerLook
    {
        get { return IsCombat() && combatBuildOpen; }
    }

    bool ShowBuildUi
    {
        get { return IsPrep() || BlocksPlayerLook; }
    }

    bool Dragging
    {
        get { return draggingPlatform || draggingTurret; }
    }

    void Start()
    {
        waveDirector = FindFirstObjectByType<WaveDirector>();
        cctv = FindFirstObjectByType<CCTVController>();
        if (platformPrefab == null)
            Debug.LogWarning("TurretPlatformPlacer: Platform Prefab이 비어 있습니다.");
    }

    void Update()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();
        if (cctv == null)
            cctv = FindFirstObjectByType<CCTVController>();

        HandleCombatBuildToggle();

        if (!IsPrep() && draggingPlatform)
            CancelPlace();

        if (!ShowBuildUi)
        {
            if (Dragging || sellMode)
            {
                sellMode = false;
                CancelPlace();
            }
            return;
        }

        if (WasCancelPressed())
        {
            if (Dragging)
            {
                CancelPlace();
                return;
            }

            if (sellMode)
                sellMode = false;
        }

        if (sellMode && !Dragging && WasLeftPressed() && !IsPointerOverBuildUi())
        {
            TrySellHoveredTurret();
            return;
        }

        if (!Dragging)
            return;

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

    void OnGUI()
    {
        if (!ShowBuildUi)
            return;

        float y = 110f;
        if (BlocksPlayerLook)
        {
            combatHeaderRect = new Rect(20f, y, 240f, 36f);
            GUI.Box(combatHeaderRect, "건설 모드  Tab 닫기");
            y += 40f;
        }
        else
        {
            combatHeaderRect = new Rect(0f, 0f, 0f, 0f);
            DrawPlatformButton(y);
            y += 56f;
        }

        DrawTurretList(y);
        y += 228f;
        DrawSellButton(y);
    }

    void DrawPlatformButton(float y)
    {
        platformButtonRect = new Rect(20f, y, 240f, 48f);
        if (platformPrefab == null)
            return;

        string label = draggingPlatform
            ? "배치 중... 우클릭 취소"
            : $"포탑 플랫폼 설치  ${GetPlatformCost()}";
        GUI.Box(platformButtonRect, label);

        Event current = Event.current;
        if (current == null || Dragging || sellMode)
            return;

        if (current.type == EventType.MouseDown &&
            current.button == 0 &&
            platformButtonRect.Contains(current.mousePosition))
        {
            BeginPlatformPlace();
            current.Use();
        }
    }

    void DrawTurretList(float y)
    {
        turretListRect = new Rect(20f, y, 240f, 220f);
        GUI.Box(turretListRect, "");

        GUILayout.BeginArea(new Rect(
            turretListRect.x + 6f,
            turretListRect.y + 6f,
            turretListRect.width - 12f,
            turretListRect.height - 12f
        ));
        GUILayout.Label("포탑");

        int count = turretOptions != null ? turretOptions.Length : 0;
        turretScroll = GUILayout.BeginScrollView(turretScroll, false, true);

        Event current = Event.current;
        for (int i = 0; i < count; i++)
        {
            TurretBuildOption option = turretOptions[i];
            if (option == null || option.prefab == null)
                continue;

            Rect row = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
            GUI.Box(row, draggingTurret && draggingPrefab == option.prefab
                ? "배치 중..."
                : $"{option.Label}  ${option.cost}");

            if (Dragging || sellMode || current == null)
                continue;

            if (current.type == EventType.MouseDown &&
                current.button == 0 &&
                row.Contains(current.mousePosition))
            {
                BeginTurretPlace(option);
                current.Use();
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawSellButton(float y)
    {
        sellButtonRect = new Rect(20f, y, 240f, 44f);
        string label = sellMode
            ? "판매 중... 포탑을 클릭"
            : "포탑 판매";
        GUI.Box(sellButtonRect, label);

        Event current = Event.current;
        if (current == null || Dragging)
            return;

        if (current.type == EventType.MouseDown &&
            current.button == 0 &&
            sellButtonRect.Contains(current.mousePosition))
        {
            sellMode = !sellMode;
            current.Use();
        }
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
        if (!IsCombat())
        {
            combatBuildOpen = false;
            return;
        }

        if (!WasTabPressed())
            return;

        combatBuildOpen = !combatBuildOpen;
        sellMode = false;
        CancelPlace();
    }

    void BeginPlatformPlace()
    {
        if (!IsPrep())
            return;

        sellMode = false;
        BeginGhost(platformPrefab, true);
    }

    void BeginTurretPlace(TurretBuildOption option)
    {
        if (option == null)
            return;

        sellMode = false;
        pendingTurretCost = option.cost;
        pendingTurretSellPrice = option.sellPrice;
        BeginGhost(option.prefab, false);
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
            && CanAfford(GetPlatformCost());
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
        if (!TrySpend(cost))
        {
            CancelPlace();
            return;
        }

        ClearGhost();

        GameObject placed = Instantiate(platformPrefab, pos, rot);
        if (placed.GetComponent<TurretPlatform>() == null)
            placed.AddComponent<TurretPlatform>();
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
        if (!TrySpend(cost))
        {
            CancelPlace();
            return;
        }

        ClearGhost();

        GameObject placed = Instantiate(prefab, pos, rot, platform.transform);

        TurretController turret = placed.GetComponentInChildren<TurretController>();
        if (turret != null)
            turret.sellPrice = sellPrice;

        platform.Mount(turret);

        if (cctv != null && turret != null)
            cctv.RegisterTurret(turret);
    }

    void CancelPlace()
    {
        ClearGhost();
    }

    void ClearGhost()
    {
        if (ghost != null)
            Destroy(ghost);

        ghost = null;
        draggingPrefab = null;
        hoverPlatform = null;
        draggingPlatform = false;
        draggingTurret = false;
        canPlace = false;
        pendingTurretCost = 0;
        pendingTurretSellPrice = 0;
    }

    void TrySellHoveredTurret()
    {
        TurretController turret = FindHoveredTurret();
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

        if (cctv != null)
            cctv.UnregisterTurret(turret);

        Transform instance = turret.transform;
        while (instance.parent != null && instance.parent != platform.transform)
            instance = instance.parent;

        Destroy(instance.gameObject);
        platform.Mount(null);
        sellMode = false;
    }

    int GetSellPrice(TurretController turret)
    {
        if (turret == null)
            return 0;

        if (turret.sellPrice > 0)
            return turret.sellPrice;

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

    TurretController FindHoveredTurret()
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

            TurretController turret = collider.GetComponentInParent<TurretController>();
            if (turret != null)
                return turret;

            TurretPlatform platform = collider.GetComponentInParent<TurretPlatform>();
            if (platform != null && platform.mountedTurret != null)
                return platform.mountedTurret;
        }

        return null;
    }

    int GetPlatformCost()
    {
        return platformBaseCost + platformCostIncrease * CountPlatforms();
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
        Vector2 guiMouse = GuiMousePosition();
        if (platformPrefab != null && platformButtonRect.Contains(guiMouse))
            return true;

        if (combatHeaderRect.width > 0f && combatHeaderRect.Contains(guiMouse))
            return true;

        if (sellButtonRect.Contains(guiMouse))
            return true;

        return turretListRect.Contains(guiMouse);
    }

    Camera GetPlaceCamera()
    {
        if (waveDirector != null &&
            waveDirector.preparationCamera != null &&
            waveDirector.preparationCamera.enabled)
        {
            return waveDirector.preparationCamera;
        }

        return Camera.main;
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

    Vector2 GuiMousePosition()
    {
        Vector3 screen = GetMouseScreenPosition();
        return new Vector2(screen.x, Screen.height - screen.y);
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
