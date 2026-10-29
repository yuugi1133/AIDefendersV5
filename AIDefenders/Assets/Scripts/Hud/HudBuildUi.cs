using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HudBuildUi
{
    readonly Color panelColor = new Color(0.08f, 0.1f, 0.14f, 0.88f);
    readonly Color rowColor = new Color(0.22f, 0.24f, 0.28f, 1f);
    readonly Color activeColor = new Color(0.32f, 0.52f, 0.78f, 1f);
    readonly Color textColor = Color.white;
    readonly Color mutedColor = new Color(0.78f, 0.8f, 0.84f, 1f);

    readonly TurretPlatformPlacer placer;
    readonly WaveDirector waveDirector;
    readonly RectTransform canvasRt;

    GameObject toggleRoot;
    Text toggleText;
    GameObject buildRoot;
    GameObject platformRow;
    Text platformText;
    Transform turretListContent;
    readonly List<Text> turretLabels = new List<Text>();
    readonly List<Image> turretImages = new List<Image>();
    readonly List<TurretBuildOption> turretEntries = new List<TurretBuildOption>();
    GameObject skipRoot;
    GameObject storageRoot;
    Transform storageContent;
    Text storageEmpty;
    int lastStoredCount = -1;
    bool lastOpen;
    GameObject inspectRoot;
    Text inspectTitle;
    Text inspectAmmo;
    Text inspectUpgrade;
    Text inspectState;
    Button upgradeButton;
    GameObject storeButton;
    Text upgradeText;
    Text sellText;

    public HudBuildUi(RectTransform canvasRoot, TurretPlatformPlacer placer, WaveDirector waveDirector)
    {
        this.placer = placer;
        this.waveDirector = waveDirector;
        canvasRt = canvasRoot;
        Build();
    }

    public void Tick()
    {
        if (placer == null)
        {
            SetActive(toggleRoot, false);
            SetActive(buildRoot, false);
            SetActive(skipRoot, false);
            SetActive(storageRoot, false);
            SetActive(inspectRoot, false);
            return;
        }

        bool hudPhase = placer.IsHudPhase;
        SetActive(toggleRoot, hudPhase);
        if (!hudPhase)
        {
            SetActive(buildRoot, false);
            SetActive(skipRoot, false);
            SetActive(storageRoot, false);
            SetActive(inspectRoot, false);
            return;
        }

        bool open = placer.ShowBuildUi;
        if (open != lastOpen)
        {
            lastStoredCount = -1;
            lastOpen = open;
        }
        toggleText.text = open ? "건설 모드  Tab 닫기" : "Tab  건설 메뉴 열기";
        SetActive(buildRoot, open);
        SetActive(storageRoot, open);
        SetActive(skipRoot, open && placer.IsPrepPhase);

        if (open)
        {
            RefreshPlatform();
            RefreshTurretRows();
            RefreshStorage();
        }

        RefreshInspect();
    }

    void Build()
    {
        toggleRoot = CreatePanel("BuildToggle", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -268f), new Vector2(420f, 48f));
        toggleText = AddCenteredLabel(toggleRoot.transform, "Label", "Tab  건설 메뉴 열기", 20, UiType.BodyMedium);
        AddPointerDown(toggleRoot, eventData =>
        {
            if (IsLeft(eventData))
            {
                GameSfx.PlayUiClick();
                placer.ToggleBuildMenu();
            }
        });

        buildRoot = new GameObject("BuildPanel");
        RectTransform buildRt = buildRoot.AddComponent<RectTransform>();
        buildRt.SetParent(canvasRt, false);
        buildRt.anchorMin = new Vector2(0f, 0f);
        buildRt.anchorMax = new Vector2(0f, 1f);
        buildRt.pivot = new Vector2(0f, 1f);
        buildRt.offsetMin = new Vector2(24f, 148f);
        buildRt.offsetMax = new Vector2(444f, -328f);

        VerticalLayoutGroup column = buildRoot.AddComponent<VerticalLayoutGroup>();
        column.spacing = 10f;
        column.childAlignment = TextAnchor.UpperLeft;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        platformRow = CreateRow(buildRt, "Platform", 52f);
        platformText = platformRow.GetComponentInChildren<Text>();
        AddPointerDown(platformRow, eventData =>
        {
            if (IsLeft(eventData))
            {
                GameSfx.PlayUiClick();
                placer.RequestPlatformPlace();
            }
        });

        Image listFrame = UiFont.AddImage(buildRt, "TurretList", new Color(0.06f, 0.07f, 0.09f, 0.92f));
        LayoutElement listLayout = listFrame.gameObject.AddComponent<LayoutElement>();
        listLayout.minHeight = 180f;
        listLayout.flexibleHeight = 1f;
        listFrame.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = listFrame.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        Text listTitle = UiFont.AddText(listFrame.rectTransform, "Title", "포탑", 20, TextAnchor.MiddleLeft, textColor, UiType.Title);
        listTitle.raycastTarget = false;
        RectTransform titleRt = listTitle.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -8f);
        titleRt.sizeDelta = new Vector2(-24f, 28f);

        GameObject content = new GameObject("Content");
        RectTransform contentRt = content.AddComponent<RectTransform>();
        contentRt.SetParent(listFrame.rectTransform, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = new Vector2(0f, -40f);
        contentRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup list = content.AddComponent<VerticalLayoutGroup>();
        list.padding = new RectOffset(10, 10, 4, 10);
        list.spacing = 8f;
        list.childControlWidth = true;
        list.childControlHeight = true;
        list.childForceExpandWidth = true;
        list.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRt;
        scroll.viewport = listFrame.rectTransform;
        turretListContent = contentRt;
        BuildTurretRows();

        skipRoot = CreatePanel("SkipPrep", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 142f), new Vector2(280f, 52f));
        AddCenteredLabel(skipRoot.transform, "Label", "준비 시간 스킵", 20, UiType.BodyMedium);
        AddPointerDown(skipRoot, eventData =>
        {
            if (IsLeft(eventData) && waveDirector != null)
            {
                GameSfx.PlayUiClick();
                waveDirector.SkipPrep();
            }
        });

        storageRoot = CreatePanel("StorageBar", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(-48f, 110f));
        Text storageTitle = UiFont.AddText(storageRoot.transform, "Title", "보관 포탑", 20, TextAnchor.MiddleLeft, textColor, UiType.Title);
        storageTitle.raycastTarget = false;
        RectTransform storageTitleRt = storageTitle.rectTransform;
        storageTitleRt.anchorMin = new Vector2(0f, 1f);
        storageTitleRt.anchorMax = new Vector2(1f, 1f);
        storageTitleRt.pivot = new Vector2(0.5f, 1f);
        storageTitleRt.anchoredPosition = Vector2.zero;
        storageTitleRt.sizeDelta = new Vector2(-28f, 32f);

        Image storageViewport = UiFont.AddImage(storageRoot.transform, "Viewport", new Color(0f, 0f, 0f, 0f));
        storageViewport.raycastTarget = true;
        RectTransform viewRt = storageViewport.rectTransform;
        viewRt.anchorMin = new Vector2(0f, 0f);
        viewRt.anchorMax = new Vector2(1f, 1f);
        viewRt.offsetMin = new Vector2(12f, 10f);
        viewRt.offsetMax = new Vector2(-12f, -36f);
        storageViewport.gameObject.AddComponent<RectMask2D>();

        ScrollRect storageScroll = storageViewport.gameObject.AddComponent<ScrollRect>();
        storageScroll.horizontal = true;
        storageScroll.vertical = false;
        storageScroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject storageGo = new GameObject("Content");
        RectTransform storageRt = storageGo.AddComponent<RectTransform>();
        storageRt.SetParent(viewRt, false);
        storageRt.anchorMin = new Vector2(0f, 0f);
        storageRt.anchorMax = new Vector2(0f, 1f);
        storageRt.pivot = new Vector2(0f, 0.5f);
        storageRt.anchoredPosition = Vector2.zero;
        storageRt.sizeDelta = new Vector2(0f, 0f);

        HorizontalLayoutGroup storageLayout = storageGo.AddComponent<HorizontalLayoutGroup>();
        storageLayout.spacing = 8f;
        storageLayout.childAlignment = TextAnchor.MiddleLeft;
        storageLayout.childControlWidth = true;
        storageLayout.childControlHeight = true;
        storageLayout.childForceExpandWidth = false;
        storageLayout.childForceExpandHeight = true;
        ContentSizeFitter storageFit = storageGo.AddComponent<ContentSizeFitter>();
        storageFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        storageScroll.content = storageRt;
        storageScroll.viewport = viewRt;
        storageContent = storageRt;

        storageEmpty = UiFont.AddText(viewRt, "Empty", "비어 있음", 18, TextAnchor.MiddleLeft, mutedColor, UiType.Body);
        storageEmpty.raycastTarget = false;
        UiFont.Stretch(storageEmpty.rectTransform);

        inspectRoot = CreatePanel("InspectPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(340f, 220f));
        inspectTitle = AddInspectLine(inspectRoot.transform, "Title", "포탑", 24, UiType.Title, 1f, -12f, 36f);
        inspectAmmo = AddInspectLine(inspectRoot.transform, "Ammo", "탄환", 18, UiType.Body, 1f, -48f, 26f);
        inspectUpgrade = AddInspectLine(inspectRoot.transform, "Upgrade", "업그레이드", 18, UiType.Body, 1f, -74f, 26f);
        inspectState = AddInspectLine(inspectRoot.transform, "State", "상태", 18, UiType.Body, 1f, -100f, 26f);

        sellText = CreateActionButton(inspectRoot.transform, "Sell", "판매", new Vector2(0.02f, 0.06f), new Vector2(0.33f, 0.3f), () => placer.RequestSellSelected());
        upgradeText = null;
        upgradeButton = CreateActionButtonGo(inspectRoot.transform, "Upgrade", "업그레이드", new Vector2(0.35f, 0.06f), new Vector2(0.66f, 0.3f), () => placer.RequestUpgradeSelected(), out upgradeText);
        storeButton = CreateActionButtonGo(inspectRoot.transform, "Store", "보관", new Vector2(0.68f, 0.06f), new Vector2(0.98f, 0.3f), () => placer.RequestStoreSelected(), out _).gameObject;
    }

    void BuildTurretRows()
    {
        turretLabels.Clear();
        turretImages.Clear();
        turretEntries.Clear();
        TurretBuildOption[] options = placer != null ? placer.turretOptions : null;
        if (options == null)
            return;

        for (int i = 0; i < options.Length; i++)
        {
            TurretBuildOption option = options[i];
            if (option == null || option.prefab == null)
                continue;

            GameObject row = CreateRow(turretListContent, "Turret_" + i, 48f);
            Text label = row.GetComponentInChildren<Text>();
            turretLabels.Add(label);
            turretImages.Add(row.GetComponent<Image>());
            turretEntries.Add(option);
            TurretBuildOption captured = option;
            AddPointerDown(row, eventData =>
            {
                if (IsLeft(eventData))
                {
                    GameSfx.PlayUiClick();
                    placer.RequestTurretPlace(captured);
                }
            });
        }
    }

    void RefreshPlatform()
    {
        if (platformRow == null)
            return;

        platformRow.SetActive(placer.HasPlatformPrefab);
        if (!platformRow.activeSelf)
            return;

        int count = placer.PlatformCount;
        int max = placer.PlatformMaxCount;
        if (placer.IsDraggingPlatform)
            platformText.text = "배치 중...  우클릭 취소  " + count + "/" + max;
        else
            platformText.text = "포탑 플랫폼 설치  $" + placer.PlatformCost + "  " + count + "/" + max;

        bool full = count >= max;
        platformRow.GetComponent<Image>().color = placer.IsDraggingPlatform
            ? activeColor
            : (full ? new Color(0.18f, 0.19f, 0.22f, 1f) : rowColor);
    }

    void RefreshTurretRows()
    {
        for (int i = 0; i < turretEntries.Count; i++)
        {
            TurretBuildOption option = turretEntries[i];
            bool dragging = placer.IsDragging && placer.DraggingPrefab == option.prefab;
            string label;
            if (dragging)
                label = "배치 중...";
            else if (placer.IsPrepPhase)
                label = option.Label + "  $" + option.cost;
            else
                label = option.Label + "  $" + option.cost + "  건설 " + option.buildTime.ToString("0.#") + "초";

            turretLabels[i].text = label;
            turretImages[i].color = dragging ? activeColor : rowColor;
        }
    }

    void RefreshStorage()
    {
        IReadOnlyList<StoredTurret> stored = placer.Stored;
        int count = stored != null ? stored.Count : 0;
        if (count == lastStoredCount)
            return;

        lastStoredCount = count;
        for (int i = storageContent.childCount - 1; i >= 0; i--)
            Object.Destroy(storageContent.GetChild(i).gameObject);

        storageEmpty.gameObject.SetActive(count == 0);
        if (count == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            StoredTurret item = stored[i];
            if (item == null || item.prefab == null)
                continue;

            GameObject row = CreateRow(storageContent, "Stored_" + i, 48f);
            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minWidth = 168f;
            layout.preferredWidth = 168f;
            layout.flexibleWidth = 0f;
            layout.minHeight = 48f;
            layout.preferredHeight = 48f;

            string extra = item.upgradeLevel > 1 ? "  +" + item.upgradeLevel : "";
            row.GetComponentInChildren<Text>().text = item.Label + extra;
            int index = i;
            AddPointerDown(row, eventData =>
            {
                if (IsLeft(eventData))
                {
                    GameSfx.PlayUiClick();
                    placer.RequestStoredPlace(index);
                }
            });
        }
    }

    void RefreshInspect()
    {
        if (!placer.ShowBuildUi)
        {
            SetActive(inspectRoot, false);
            return;
        }

        TurretController turret = placer.SelectedTurret;
        TurretPlatform platform = placer.SelectedPlatform;
        if (turret == null && platform == null)
        {
            SetActive(inspectRoot, false);
            return;
        }

        Camera camera = placer.GetHudCamera();
        if (camera == null)
        {
            SetActive(inspectRoot, false);
            return;
        }

        Vector3 world = placer.GetInspectAnchor();
        Vector3 screen = camera.WorldToScreenPoint(world);
        if (screen.z <= 0.1f)
        {
            SetActive(inspectRoot, false);
            return;
        }

        SetActive(inspectRoot, true);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out local);
        RectTransform inspectRt = inspectRoot.GetComponent<RectTransform>();
        Vector2 pos = local + new Vector2(0f, 16f);
        Rect canvasRect = canvasRt.rect;
        float halfW = inspectRt.sizeDelta.x * 0.5f;
        float height = inspectRt.sizeDelta.y;
        pos.x = Mathf.Clamp(pos.x, canvasRect.xMin + halfW + 12f, canvasRect.xMax - halfW - 12f);
        pos.y = Mathf.Clamp(pos.y, canvasRect.yMin + 12f, canvasRect.yMax - height - 12f);
        inspectRt.anchoredPosition = pos;

        bool hasTurret = turret != null;
        SetActive(inspectAmmo.gameObject, hasTurret);
        SetActive(inspectUpgrade.gameObject, hasTurret);
        SetActive(inspectState.gameObject, hasTurret);
        SetActive(upgradeButton.gameObject, hasTurret);
        SetActive(storeButton, hasTurret);

        sellText.text = "판매  $" + placer.GetSelectedSellPrice();
        if (!hasTurret)
        {
            inspectTitle.text = "플랫폼";
            return;
        }

        inspectTitle.text = turret.TypeLabel;
        inspectAmmo.text = "탄환  " + TurretPlatformPlacer.AmmoStatus(turret);
        inspectUpgrade.text = "업그레이드  " + turret.upgradeLevel + "단계";
        inspectState.text = "상태  " + TurretPlatformPlacer.StateLabel(turret);

        bool canUpgrade = turret.CanUpgrade();
        int cost = turret.NextUpgradeCost();
        upgradeButton.interactable = canUpgrade && placer.CanAffordCost(cost);
        upgradeText.text = canUpgrade ? "업그레이드  $" + cost : turret.NextUpgradeLabel();
    }

    GameObject CreatePanel(string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        Image image = UiFont.AddImage(canvasRt, name, panelColor);
        RectTransform rt = image.rectTransform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return image.gameObject;
    }

    GameObject CreateRow(Transform parent, string name, float height)
    {
        Image row = UiFont.AddImage(parent, name, rowColor);
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        Text text = UiFont.AddText(row.rectTransform, "Label", "", 18, TextAnchor.MiddleLeft, textColor, UiType.BodyMedium);
        text.raycastTarget = false;
        UiFont.Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(14f, 0f);
        text.rectTransform.offsetMax = new Vector2(-14f, 0f);
        return row.gameObject;
    }

    Text AddCenteredLabel(Transform parent, string name, string content, int size, UiType type)
    {
        Text text = UiFont.AddText(parent, name, content, size, TextAnchor.MiddleCenter, textColor, type);
        text.raycastTarget = false;
        UiFont.Stretch(text.rectTransform);
        return text;
    }

    static Text AddInspectLine(Transform parent, string name, string content, int size, UiType type, float anchorMaxY, float top, float height)
    {
        Text text = UiFont.AddText(parent, name, content, size, TextAnchor.MiddleLeft, Color.white, type);
        text.raycastTarget = false;
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, top);
        rt.sizeDelta = new Vector2(-28f, height);
        return text;
    }

    Text CreateActionButton(Transform parent, string name, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction click)
    {
        Text text;
        CreateActionButtonGo(parent, name, label, min, max, click, out text);
        return text;
    }

    Button CreateActionButtonGo(Transform parent, string name, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction click, out Text text)
    {
        Image image = UiFont.AddImage(parent, name, Color.white);
        RectTransform rt = image.rectTransform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            GameSfx.PlayUiClick();
            click();
        });
        text = UiFont.AddText(rt, "Label", label, 16, TextAnchor.MiddleCenter, new Color(0.12f, 0.13f, 0.15f, 1f), UiType.BodyMedium);
        text.raycastTarget = false;
        UiFont.Stretch(text.rectTransform);
        return button;
    }

    static void AddPointerDown(GameObject go, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = go.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    static bool IsLeft(BaseEventData eventData)
    {
        PointerEventData pointer = eventData as PointerEventData;
        return pointer == null || pointer.button == PointerEventData.InputButton.Left;
    }

    static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
