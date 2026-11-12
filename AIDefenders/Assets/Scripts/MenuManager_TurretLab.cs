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

    readonly Color panelColor = UiSkin.Panel;
    readonly Color rowColor = UiSkin.Blue;
    readonly Color selectedColor = UiSkin.Sky;
    readonly Color textColor = new Color(0.94f, 0.95f, 0.97f, 1f);
    readonly Color mutedColor = UiSkin.Muted;

    Transform turretListContent;
    Transform researchListContent;
    Text pointsText;
    Image[] turretRowImages = new Image[0];
    int selectedIndex = -1;
    int previewLevel = 1;
    GameObject previewInstance;

    void Start()
    {
        EnsureDefaultCatalog();
        if (previewPlatform == null)
            previewPlatform = FindFirstObjectByType<TurretPlatform>();

        ConfigureCanvas();
        StripLegacyHud();
        CreateNavButtons();
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
        sidebarRt.sizeDelta = new Vector2(520f, 0f);
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
        turretListContent = CreateScrollSection(sidebarRt, "TurretList", 240f);
        AddSectionLabel(sidebarRt, "연구");
        researchListContent = CreateScrollSection(sidebarRt, "ResearchList", 380f);

        Image pointsBar = UiFont.AddImage(sidebarRt, "ResearchPoints", new Color(.36f, .16f, .65f, 1f));
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
        RefreshResearchPoints();
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
        Image frame = UiFont.AddImage(parent, name, UiSkin.Navy);
        LayoutElement frameLayout = frame.gameObject.AddComponent<LayoutElement>();
        frameLayout.minHeight = 120f;
        frameLayout.preferredHeight = height;
        frameLayout.flexibleHeight = name == "TurretList" ? 0f : 1f;

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
            BindUiClick(button);
            button.onClick.AddListener(() => SelectTurret(index));
        }

        if (count == 0)
            CreateRow(turretListContent, "Empty", "등록된 포탑이 없습니다.", mutedColor);
    }

    void RefreshResearchList()
    {
        ClearChildren(researchListContent);

        TurretLabEntry entry = GetSelectedEntry();
        string group = entry != null ? TurretResearch.GroupOfPrefab(entry.prefab) : "";
        TurretResearchDef[] researches = TurretResearch.ForGroup(group);
        CreateResearchOverview(entry, researches);
        if (researches == null || researches.Length == 0)
        {
            CreateRow(researchListContent, "EmptyResearch", "준비 중 — 연구가 아직 없습니다.", mutedColor);
            return;
        }

        for (int i = 0; i < researches.Length; i++)
        {
            TurretResearchDef research = researches[i];
            if (research == null)
                continue;

            bool complete = TurretResearch.IsComplete(research.id);
            bool unlocked = TurretResearch.PrerequisitesMet(research);
            bool affordable = GameProgress.ResearchPoints >= research.cost;
            Color titleColor = complete ? new Color(0.55f, 0.92f, 0.68f, 1f) : unlocked ? textColor : mutedColor;
            Image row = CreateRow(researchListContent, "Research_" + i, research.displayName, titleColor);

            string detail;
            if (complete)
                detail = research.description + "  ·  완료";
            else if (!unlocked)
                detail = "선행 필요  ·  " + TurretResearch.PrerequisiteLabel(research);
            else if (!affordable)
                detail = research.description + "  ·  " + research.cost + " RP  ·  포인트 부족";
            else
                detail = research.description + "  ·  " + research.cost + " RP";

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
                layout.minHeight = 78f;
                layout.preferredHeight = 78f;
            }

            if (complete)
                row.color = new Color(0.16f, 0.28f, 0.22f, 1f);
            else if (!unlocked)
                row.color = new Color(0.16f, 0.17f, 0.19f, 1f);

            if (!complete && unlocked)
            {
                string researchId = research.id;
                Button button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row;
                button.interactable = affordable;
                BindUiClick(button);
                button.onClick.AddListener(() => TryBuyResearch(researchId));
            }
        }
    }

    void TryBuyResearch(string id)
    {
        if (!TurretResearch.TryUnlock(id))
            return;

        RefreshResearchPoints();
        RefreshResearchList();
        SpawnPreview(GetSelectedEntry());
    }

    void CreateResearchOverview(TurretLabEntry entry, TurretResearchDef[] researches)
    {
        if (entry == null || entry.prefab == null)
            return;
        TurretController source = entry.prefab.GetComponentInChildren<TurretController>(true);
        if (source == null)
            return;
        previewLevel = Mathf.Clamp(previewLevel, 1, source.MaxLevel);
        TurretUpgradeTier tier = source.upgradeTiers != null && source.upgradeTiers.Length >= previewLevel
            ? source.upgradeTiers[previewLevel - 1] : null;
        WeaponSystem weapon = source.weapon != null ? source.weapon : source.GetComponent<WeaponSystem>();
        float baseRange = tier != null && tier.range > 0f ? tier.range : source.range;
        float baseAp = tier != null ? tier.armorPierce : source.armorPierce;
        float rangeBonus = TurretResearch.RangeBonus(source);
        float apBonus = TurretResearch.ArmorPierceBonus(source);
        int complete = 0;
        if (researches != null)
            foreach (TurretResearchDef research in researches)
                if (research != null && TurretResearch.IsComplete(research.id))
                    complete++;
        string detail = "연구 완료  " + complete + " / " + (researches != null ? researches.Length : 0)
            + "\n사거리  " + (baseRange + rangeBonus).ToString("0.#") + "  (연구 +" + rangeBonus.ToString("0.#") + ")"
            + "    AP  " + (baseAp + apBonus).ToString("0.#") + "  (+" + apBonus.ToString("0.#") + ")";
        if (weapon != null)
        {
            float speed = weapon.ammo != null ? weapon.ammo.bulletSpeed : 0f;
            float reload = tier != null ? tier.reloadTime : weapon.reloadTime;
            detail += "\n탄속  " + (speed + TurretResearch.BulletSpeedBonus(source)).ToString("0.#")
                + "    탄퍼짐  " + Mathf.Max(0f, weapon.spreadAngle + TurretResearch.SpreadBonus(source)).ToString("0.#") + "°"
                + "\n재장전  " + Mathf.Max(0.01f, reload + TurretResearch.ReloadBonus(source)).ToString("0.##") + "초";
        }
        detail += "\n영구 연구는 아래 목록에서 해금합니다.\n단계 업그레이드는 전투 중 포탑을 선택해 진행합니다.";

        Image row = CreateRow(researchListContent, "ResearchOverview", "연구 적용 성능 · " + previewLevel + "단계 미리보기", textColor);
        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.minHeight = rowLayout.preferredHeight = 246f;
        Text heading = row.GetComponentInChildren<Text>();
        heading.fontSize = 18;
        heading.rectTransform.anchorMin = new Vector2(0f, 1f);
        heading.rectTransform.anchorMax = new Vector2(1f, 1f);
        heading.rectTransform.pivot = new Vector2(0.5f, 1f);
        heading.rectTransform.offsetMin = new Vector2(14f, -38f);
        heading.rectTransform.offsetMax = new Vector2(-14f, -8f);
        Text body = UiFont.AddText(row.rectTransform, "AppliedStats", detail, 16, TextAnchor.UpperLeft, mutedColor, UiType.Body);
        body.raycastTarget = false;
        UiFont.Stretch(body.rectTransform);
        body.rectTransform.offsetMin = new Vector2(14f, 52f);
        body.rectTransform.offsetMax = new Vector2(-14f, -44f);

        var buttons = new GameObject("PreviewLevels", typeof(RectTransform));
        RectTransform buttonsRt = buttons.GetComponent<RectTransform>();
        buttonsRt.SetParent(row.transform, false);
        buttonsRt.anchorMin = new Vector2(0f, 0f);
        buttonsRt.anchorMax = new Vector2(1f, 0f);
        buttonsRt.pivot = new Vector2(0.5f, 0f);
        buttonsRt.offsetMin = new Vector2(14f, 10f);
        buttonsRt.offsetMax = new Vector2(-14f, 44f);
        HorizontalLayoutGroup layout = buttons.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        for (int level = 1; level <= source.MaxLevel; level++)
        {
            int captured = level;
            Image image = UiFont.AddImage(buttonsRt, "PreviewLevel_" + level, level == previewLevel ? selectedColor : rowColor);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            BindUiClick(button);
            Text label = UiFont.AddText(image.rectTransform, "Label", level + "단계", 16, TextAnchor.MiddleCenter, textColor, UiType.BodyMedium);
            label.raycastTarget = false;
            UiFont.Stretch(label.rectTransform);
            button.onClick.AddListener(() =>
            {
                previewLevel = captured;
                SpawnPreview(entry);
                RefreshResearchList();
            });
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
        previewLevel = 1;
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
            turret.upgradeLevel = Mathf.Clamp(previewLevel, 1, turret.MaxLevel);
            turret.BeginConstruction(0f, true, null);
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

    void RefreshResearchPoints()
    {
        if (pointsText != null)
            pointsText.text = ResearchPointsLabel();
    }

    static string ResearchPointsLabel()
    {
        return "연구 포인트  " + GameProgress.ResearchPoints;
    }

    static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    void StripLegacyHud()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
            return;

        Transform panel = canvas.transform.Find("Panel");
        if (panel != null)
            Destroy(panel.gameObject);

        Transform dataset = canvas.transform.Find("Button_Dataset");
        if (dataset != null)
            Destroy(dataset.gameObject);

        Transform main = canvas.transform.Find("Button_MainMenu");
        if (main != null)
            Destroy(main.gameObject);
    }

    void CreateNavButtons()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
            return;

        Button dataset = UiSkin.Button(
            canvas.transform,
            "DatasetFactory",
            "데이터셋 팩토리",
            UiSkin.Blue,
            Vector2.zero,
            new Vector2(180f, 72f),
            new Vector2(300f, 76f));
        dataset.onClick.AddListener(() => LoadScene("DatasetGeneratorScene"));

        Button main = UiSkin.Button(
            canvas.transform,
            "MainMenu",
            "메인 메뉴",
            UiSkin.Blue,
            Vector2.zero,
            new Vector2(520f, 72f),
            new Vector2(300f, 76f));
        main.onClick.AddListener(() => LoadScene("MainMenu"));
    }

    static void BindUiClick(Button button)
    {
        if (button != null)
            button.onClick.AddListener(GameSfx.PlayUiClick);
    }

    Canvas FindCanvas()
    {
        Canvas canvas = UiFont.FindSceneCanvas(this);
        if (canvas == null)
            canvas = SceneLoader.FindSceneCanvas();
        return canvas;
    }
}
