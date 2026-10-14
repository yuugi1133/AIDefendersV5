using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TurretLabResearch
{
    public string displayName;
    public string description;
    public int cost;
}

[System.Serializable]
public class TurretLabEntry
{
    public string displayName;
    public GameObject prefab;
    public TurretLabResearch[] researches;

    public string Label
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
            return prefab != null ? prefab.name : "포탑";
        }
    }
}

public class MenuManager_TurretLab : MonoBehaviour
{
    public TurretPlatform previewPlatform;
    public TurretLabEntry[] turretCatalog;
    public int researchPoints;

    readonly Color panelColor = new Color(0.12f, 0.14f, 0.18f, 0.94f);
    readonly Color rowColor = new Color(0.22f, 0.24f, 0.28f, 1f);
    readonly Color selectedColor = new Color(0.32f, 0.52f, 0.78f, 1f);
    readonly Color textColor = new Color(0.94f, 0.95f, 0.97f, 1f);
    readonly Color mutedColor = new Color(0.72f, 0.74f, 0.78f, 1f);

    Transform turretListContent;
    Transform researchListContent;
    Text pointsText;
    Image[] turretRowImages = new Image[0];
    int selectedIndex = -1;
    GameObject previewInstance;

    void Start()
    {
        EnsureDefaultCatalog();
        if (previewPlatform == null)
            previewPlatform = FindFirstObjectByType<TurretPlatform>();

        ConfigureCanvas();
        ApplyExistingFonts();
        BuildLabPanel();

        if (turretCatalog != null && turretCatalog.Length > 0)
            SelectTurret(0);
        else
            RefreshResearchList();
    }

    public void LoadScene(string sceneName)
    {
        SceneLoader.Load(sceneName);
    }

    void EnsureDefaultCatalog()
    {
        if (turretCatalog != null && turretCatalog.Length > 0)
            return;

        turretCatalog = new TurretLabEntry[0];
    }

    void ConfigureCanvas()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
            return;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform panel = canvas.transform.Find("Panel");
        if (panel != null)
        {
            Image image = panel.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
        }
    }

    void ApplyExistingFonts()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
            return;

        TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                UiFont.Apply(texts[i], UiType.BodyMedium);
        }
    }

    void BuildLabPanel()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
            return;

        Transform host = canvas.transform.Find("Panel");
        if (host == null)
            host = canvas.transform;

        Transform existing = host.Find("Panel_TurretLab");
        if (existing != null)
            Destroy(existing.gameObject);

        Image sidebar = UiFont.AddImage(host, "Panel_TurretLab", panelColor);
        RectTransform sidebarRt = sidebar.rectTransform;
        sidebarRt.anchorMin = new Vector2(1f, 0f);
        sidebarRt.anchorMax = new Vector2(1f, 1f);
        sidebarRt.pivot = new Vector2(1f, 0.5f);
        sidebarRt.sizeDelta = new Vector2(420f, 0f);
        sidebarRt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 24, 24);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddSectionLabel(sidebarRt, "포탑");
        turretListContent = CreateScrollSection(sidebarRt, "TurretList", 280f);
        AddSectionLabel(sidebarRt, "연구");
        researchListContent = CreateScrollSection(sidebarRt, "ResearchList", 240f);

        Image pointsBar = UiFont.AddImage(sidebarRt, "ResearchPoints", new Color(0.08f, 0.09f, 0.12f, 1f));
        LayoutElement pointsLayout = pointsBar.gameObject.AddComponent<LayoutElement>();
        pointsLayout.minHeight = 52f;
        pointsLayout.preferredHeight = 52f;

        pointsText = UiFont.AddText(
            pointsBar.rectTransform,
            "Value",
            ResearchPointsLabel(),
            22,
            TextAnchor.MiddleLeft,
            textColor,
            UiType.BodyMedium);
        UiFont.Stretch(pointsText.rectTransform);
        pointsText.rectTransform.offsetMin = new Vector2(16f, 0f);
        pointsText.rectTransform.offsetMax = new Vector2(-16f, 0f);

        RebuildTurretList();
    }

    void AddSectionLabel(Transform parent, string label)
    {
        Text text = UiFont.AddText(parent, label + "Header", label, 26, TextAnchor.MiddleLeft, textColor, UiType.Title);
        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 36f;
        layout.preferredHeight = 36f;
    }

    Transform CreateScrollSection(Transform parent, string name, float height)
    {
        Image frame = UiFont.AddImage(parent, name, new Color(0.08f, 0.09f, 0.11f, 0.9f));
        LayoutElement frameLayout = frame.gameObject.AddComponent<LayoutElement>();
        frameLayout.minHeight = 120f;
        frameLayout.preferredHeight = height;
        frameLayout.flexibleHeight = 1f;

        Mask mask = frame.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;
        ScrollRect scroll = frame.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        GameObject content = new GameObject("Content");
        RectTransform contentRt = content.AddComponent<RectTransform>();
        contentRt.SetParent(frame.rectTransform, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup list = content.AddComponent<VerticalLayoutGroup>();
        list.padding = new RectOffset(8, 8, 8, 8);
        list.spacing = 8f;
        list.childAlignment = TextAnchor.UpperCenter;
        list.childControlWidth = true;
        list.childControlHeight = true;
        list.childForceExpandWidth = true;
        list.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRt;
        scroll.viewport = frame.rectTransform;
        return contentRt;
    }

    void RebuildTurretList()
    {
        ClearChildren(turretListContent);
        int count = turretCatalog != null ? turretCatalog.Length : 0;
        turretRowImages = new Image[count];

        for (int i = 0; i < count; i++)
        {
            TurretLabEntry entry = turretCatalog[i];
            if (entry == null)
                continue;

            int index = i;
            Image row = CreateRow(turretListContent, "Turret_" + i, entry.Label, mutedColor);
            turretRowImages[i] = row;
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            button.onClick.AddListener(() => SelectTurret(index));
        }

        if (count == 0)
            CreateRow(turretListContent, "Empty", "등록된 포탑이 없습니다.", mutedColor);
    }

    void RefreshResearchList()
    {
        ClearChildren(researchListContent);

        TurretLabEntry entry = GetSelectedEntry();
        TurretLabResearch[] researches = entry != null ? entry.researches : null;
        if (researches == null || researches.Length == 0)
        {
            CreateRow(researchListContent, "EmptyResearch", "준비 중 — 연구가 아직 없습니다.", mutedColor);
            return;
        }

        for (int i = 0; i < researches.Length; i++)
        {
            TurretLabResearch research = researches[i];
            if (research == null)
                continue;

            string title = string.IsNullOrWhiteSpace(research.displayName) ? "연구" : research.displayName;
            string detail = research.description;
            if (research.cost > 0)
                detail = string.IsNullOrWhiteSpace(detail)
                    ? research.cost + " RP"
                    : detail + "  ·  " + research.cost + " RP";

            Image row = CreateRow(researchListContent, "Research_" + i, title, textColor);
            if (!string.IsNullOrWhiteSpace(detail))
            {
                Text sub = UiFont.AddText(row.rectTransform, "Detail", detail, 16, TextAnchor.MiddleLeft, mutedColor, UiType.Body);
                sub.rectTransform.anchorMin = new Vector2(0f, 0f);
                sub.rectTransform.anchorMax = new Vector2(1f, 0.48f);
                sub.rectTransform.offsetMin = new Vector2(14f, 6f);
                sub.rectTransform.offsetMax = new Vector2(-14f, 0f);

                Text titleText = row.GetComponentInChildren<Text>();
                if (titleText != null)
                {
                    titleText.alignment = TextAnchor.LowerLeft;
                    titleText.rectTransform.anchorMin = new Vector2(0f, 0.42f);
                    titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
                    titleText.rectTransform.offsetMin = new Vector2(14f, 0f);
                    titleText.rectTransform.offsetMax = new Vector2(-14f, -6f);
                }

                LayoutElement layout = row.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.minHeight = 72f;
                    layout.preferredHeight = 72f;
                }
            }
        }
    }

    Image CreateRow(Transform parent, string name, string label, Color labelColor)
    {
        Image row = UiFont.AddImage(parent, name, rowColor);
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 48f;
        layout.preferredHeight = 48f;

        Text text = UiFont.AddText(row.rectTransform, "Label", label, 20, TextAnchor.MiddleLeft, labelColor, UiType.BodyMedium);
        UiFont.Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(14f, 0f);
        text.rectTransform.offsetMax = new Vector2(-14f, 0f);
        return row;
    }

    void SelectTurret(int index)
    {
        if (turretCatalog == null || index < 0 || index >= turretCatalog.Length)
            return;

        selectedIndex = index;
        for (int i = 0; i < turretRowImages.Length; i++)
        {
            if (turretRowImages[i] != null)
                turretRowImages[i].color = i == selectedIndex ? selectedColor : rowColor;
        }

        RefreshResearchList();
        SpawnPreview(turretCatalog[index]);
    }

    void SpawnPreview(TurretLabEntry entry)
    {
        ClearPreview();
        if (entry == null || entry.prefab == null || previewPlatform == null)
            return;

        Vector3 mount = previewPlatform.GetMountPosition();
        Quaternion facing = previewPlatform.transform.rotation;
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 toCam = cam.transform.position - mount;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.01f)
                facing = Quaternion.LookRotation(toCam.normalized);
        }

        previewInstance = Instantiate(entry.prefab, mount, facing);
        previewInstance.name = entry.Label;
        previewInstance.transform.SetParent(previewPlatform.transform, true);

        TurretController turret = previewInstance.GetComponent<TurretController>();
        if (turret != null)
        {
            turret.enabled = false;
            turret.sourcePrefab = entry.prefab;
            turret.displayName = entry.Label;
            previewPlatform.Mount(turret);
        }
        else
        {
            previewPlatform.Mount(null);
            previewPlatform.occupied = true;
        }

        WeaponSystem weapon = previewInstance.GetComponent<WeaponSystem>();
        if (weapon != null)
            weapon.enabled = false;
    }

    void ClearPreview()
    {
        if (previewInstance != null)
            Destroy(previewInstance);
        previewInstance = null;

        if (previewPlatform != null)
        {
            previewPlatform.mountedTurret = null;
            previewPlatform.occupied = false;
        }
    }

    TurretLabEntry GetSelectedEntry()
    {
        if (turretCatalog == null || selectedIndex < 0 || selectedIndex >= turretCatalog.Length)
            return null;
        return turretCatalog[selectedIndex];
    }

    string ResearchPointsLabel()
    {
        return "연구 포인트  " + Mathf.Max(0, researchPoints);
    }

    static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    Canvas FindCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        return canvas;
    }
}
