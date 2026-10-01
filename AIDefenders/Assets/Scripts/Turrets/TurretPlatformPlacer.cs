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

    WaveDirector waveDirector;
    CCTVController cctv;
    GameObject ghost;
    GameObject draggingPrefab;
    TurretPlatform hoverPlatform;
    bool draggingPlatform;
    bool draggingTurret;
    bool canPlace;
    Vector2 turretScroll;
    Rect platformButtonRect;
    Rect turretListRect;
    Color validColor = new Color(0.35f, 0.85f, 0.4f, 1f);
    Color invalidColor = new Color(0.9f, 0.25f, 0.2f, 1f);

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

        if (!IsPrep() && Dragging)
            CancelPlace();

        if (!Dragging)
            return;

        if (WasCancelPressed())
        {
            CancelPlace();
            return;
        }

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
        if (!IsPrep())
            return;

        DrawPlatformButton();
        DrawTurretList();
    }

    void DrawPlatformButton()
    {
        platformButtonRect = new Rect(20f, 110f, 240f, 48f);
        if (platformPrefab == null)
            return;

        string label = draggingPlatform ? "배치 중... 우클릭 취소" : "포탑 플랫폼 설치";
        GUI.Box(platformButtonRect, label);

        Event current = Event.current;
        if (current == null || Dragging)
            return;

        if (current.type == EventType.MouseDown &&
            current.button == 0 &&
            platformButtonRect.Contains(current.mousePosition))
        {
            BeginPlatformPlace();
            current.Use();
        }
    }

    void DrawTurretList()
    {
        turretListRect = new Rect(20f, 166f, 240f, 220f);
        GUI.Box(turretListRect, "");

        GUILayout.BeginArea(new Rect(
            turretListRect.x + 6f,
            turretListRect.y + 6f,
            turretListRect.width - 12f,
            turretListRect.height - 12f
        ));
        GUILayout.Label("포탑");

        int count = turretOptions != null ? turretOptions.Length : 0;
        float contentHeight = Mathf.Max(count * 48f, turretListRect.height - 40f);
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
                : option.Label);

            if (Dragging || current == null)
                continue;

            if (current.type == EventType.MouseDown &&
                current.button == 0 &&
                row.Contains(current.mousePosition))
            {
                BeginTurretPlace(option.prefab);
                current.Use();
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    bool IsPrep()
    {
        return waveDirector != null && waveDirector.Phase == WavePhase.Prep;
    }

    void BeginPlatformPlace()
    {
        BeginGhost(platformPrefab, true);
    }

    void BeginTurretPlace(GameObject prefab)
    {
        BeginGhost(prefab, false);
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
        canPlace = !IsOccupied(pos, ghost.transform.rotation);
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
            canPlace = hoverPlatform.IsEmpty;
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
        ClearGhost();

        GameObject placed = Instantiate(prefab, pos, rot, platform.transform);

        TurretController turret = placed.GetComponentInChildren<TurretController>();
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
