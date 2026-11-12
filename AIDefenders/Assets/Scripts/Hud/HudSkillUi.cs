using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HudSkillUi
{
    readonly CCTVSkillSystem skills;
    readonly GameObject root;
    readonly Image crosshairH;
    readonly Image crosshairV;
    readonly Text hint;
    readonly SlotView[] slots = new SlotView[CCTVSkillSystem.SlotCount];
    readonly Color readyBorder = new Color(0.55f, 0.75f, 0.7f, 0.85f);
    readonly Color primedBorder = new Color(1f, 0.85f, 0.25f, 0.95f);
    readonly Color primedCrosshair = new Color(1f, 0.85f, 0.25f, 0.95f);
    static Sprite solidSprite;

    struct SlotView
    {
        public Image background;
        public Image icon;
        public Image cooldown;
        public Outline outline;
        public Text key;
        public Text name;
        public Text status;
    }

    public HudSkillUi(RectTransform canvasRoot, CCTVSkillSystem skills)
    {
        this.skills = skills;

        root = new GameObject("SkillHud");
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.SetParent(canvasRoot, false);
        UiFont.Stretch(rootRt);

        Color crossColor = skills != null ? skills.crosshairColor : new Color(0.95f, 0.95f, 0.9f, 0.9f);
        float arm = skills != null ? skills.crosshairSize : 14f;
        float thickness = skills != null ? Mathf.Max(1f, skills.crosshairThickness) : 2f;

        crosshairH = CreateBar(rootRt, "CrosshairH", crossColor, new Vector2(arm * 2f, thickness));
        crosshairV = CreateBar(rootRt, "CrosshairV", crossColor, new Vector2(thickness, arm * 2f));

        float slotSize = skills != null ? skills.slotSize : 72f;
        float gap = skills != null ? skills.slotGap : 8f;
        float bottom = skills != null ? skills.slotBottomMargin : 28f;
        float totalWidth = CCTVSkillSystem.SlotCount * slotSize + (CCTVSkillSystem.SlotCount - 1) * gap;

        GameObject bar = new GameObject("SkillBar");
        RectTransform barRt = bar.AddComponent<RectTransform>();
        barRt.SetParent(rootRt, false);
        barRt.anchorMin = new Vector2(0.5f, 0f);
        barRt.anchorMax = new Vector2(0.5f, 0f);
        barRt.pivot = new Vector2(0.5f, 0f);
        barRt.anchoredPosition = new Vector2(0f, bottom);
        barRt.sizeDelta = new Vector2(totalWidth, slotSize + 24f);

        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = gap;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < CCTVSkillSystem.SlotCount; i++)
            slots[i] = CreateSlot(barRt, i, slotSize);

        hint = UiFont.AddText(rootRt, "PrimeHint", "좌클릭 적 식별 / 우클릭 아군 식별 / 키 다시 눌러 취소", 18, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.4f, 1f), UiType.BodyMedium);
        hint.raycastTarget = false;
        RectTransform hintRt = hint.rectTransform;
        hintRt.anchorMin = new Vector2(0.5f, 0f);
        hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, bottom + slotSize + 32f);
        hintRt.sizeDelta = new Vector2(640f, 28f);
    }

    public void Tick()
    {
        bool visible = skills != null && skills.ShouldDrawHud;
        if (root.activeSelf != visible)
            root.SetActive(visible);
        if (!visible)
            return;

        bool primed = skills.HasPrimedSkill();
        Color cross = primed ? primedCrosshair : skills.crosshairColor;
        crosshairH.color = cross;
        crosshairV.color = cross;
        hint.gameObject.SetActive(primed);

        for (int i = 0; i < slots.Length; i++)
            RefreshSlot(i);
    }

    void RefreshSlot(int index)
    {
        SlotView slot = slots[index];
        CCTVActiveSkill skill = skills.GetEquipped(index);
        bool primed = skill != null && skill.IsPrimed;
        slot.outline.effectColor = primed ? primedBorder : readyBorder;
        slot.key.text = CCTVSkillSystem.SlotKeyLabels[index];

        if (skill == null)
        {
            slot.icon.sprite = null;
            slot.icon.enabled = false;
            slot.name.text = "빈 칸";
            slot.name.color = new Color(1f, 1f, 1f, 0.35f);
            slot.status.text = "";
            slot.cooldown.fillAmount = 0f;
            return;
        }

        Sprite icon = skill.HudIcon;
        if (slot.icon.sprite != icon)
            slot.icon.sprite = icon;
        slot.icon.enabled = icon != null;
        slot.icon.color = skill.IsReady ? Color.white : new Color(0.6f, 0.65f, 0.7f, 1f);
        slot.name.color = Color.white;
        slot.name.text = string.IsNullOrWhiteSpace(skill.displayName) ? skill.name : skill.displayName;
        slot.cooldown.fillAmount = skill.IsReady ? 0f : skill.CooldownNormalized;
        if (skill.IsPrimed)
            slot.status.text = "장착";
        else if (!skill.IsReady)
            slot.status.text = skill.CooldownRemaining.ToString("0.0");
        else
            slot.status.text = "";
    }

    SlotView CreateSlot(RectTransform parent, int index, float size)
    {
        Image background = CreateImage(parent, "Slot_" + index, UiSkin.Blue);
        UiSkin.Surface(background);
        LayoutElement layout = background.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = size;
        layout.minHeight = size + 24f;
        layout.preferredWidth = size;
        layout.preferredHeight = size + 24f;

        Outline outline = background.GetComponent<Outline>();
        outline.effectColor = readyBorder;
        outline.effectDistance = new Vector2(2f, -2f);

        Image icon = CreateImage(background.rectTransform, "Icon", Color.white);
        icon.raycastTarget = false;
        icon.sprite = null;
        icon.enabled = false;
        icon.preserveAspect = true;
        icon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        icon.rectTransform.pivot = new Vector2(0.5f, 1f);
        icon.rectTransform.anchoredPosition = new Vector2(0f, -6f);
        icon.rectTransform.sizeDelta = new Vector2(size - 12f, size - 12f);

        Image cooldown = CreateImage(background.rectTransform, "Cooldown", new Color(0f, 0f, 0f, 0.6f));
        cooldown.raycastTarget = false;
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Vertical;
        cooldown.fillOrigin = (int)Image.OriginVertical.Bottom;
        cooldown.fillAmount = 0f;
        UiFont.Stretch(cooldown.rectTransform);
        cooldown.rectTransform.offsetMin = new Vector2(0f, 24f);

        Image namePlate = CreateImage(background.rectTransform, "NamePlate", new Color(0f, 0f, 0f, 0.42f));
        namePlate.raycastTarget = false;
        namePlate.rectTransform.anchorMin = Vector2.zero;
        namePlate.rectTransform.anchorMax = new Vector2(1f, 0f);
        namePlate.rectTransform.pivot = new Vector2(0.5f, 0f);
        namePlate.rectTransform.sizeDelta = new Vector2(0f, 24f);

        Image keyBadge = CreateImage(background.rectTransform, "KeyBadge", new Color(0.02f, 0.03f, 0.04f, 0.9f));
        keyBadge.raycastTarget = false;
        keyBadge.rectTransform.anchorMin = new Vector2(0f, 1f);
        keyBadge.rectTransform.anchorMax = new Vector2(0f, 1f);
        keyBadge.rectTransform.pivot = new Vector2(0f, 1f);
        keyBadge.rectTransform.anchoredPosition = new Vector2(4f, -4f);
        keyBadge.rectTransform.sizeDelta = new Vector2(20f, 20f);

        Text key = UiFont.AddText(keyBadge.rectTransform, "Key", "Q", 14, TextAnchor.MiddleCenter, Color.white, UiType.BodyBold);
        key.raycastTarget = false;
        UiFont.Stretch(key.rectTransform);

        Text name = UiFont.AddText(namePlate.rectTransform, "Name", "", 13, TextAnchor.MiddleCenter, Color.white, UiType.BodyMedium);
        name.raycastTarget = false;
        name.horizontalOverflow = HorizontalWrapMode.Wrap;
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = 10;
        name.resizeTextMaxSize = 13;
        RectTransform nameRt = name.rectTransform;
        UiFont.Stretch(nameRt);
        nameRt.offsetMin = new Vector2(3f, 0f);
        nameRt.offsetMax = new Vector2(-3f, 0f);

        Text status = UiFont.AddText(background.rectTransform, "Status", "", 20, TextAnchor.MiddleCenter, mutedHint(), UiType.BodyBold);
        status.raycastTarget = false;
        RectTransform statusRt = status.rectTransform;
        UiFont.Stretch(statusRt);
        statusRt.offsetMin = new Vector2(0f, 24f);
        Outline statusOutline = status.gameObject.AddComponent<Outline>();
        statusOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        statusOutline.effectDistance = new Vector2(1f, -1f);

        int captured = index;
        EventTrigger trigger = background.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener(_ =>
        {
            if (skills != null)
            {
                GameSfx.PlayUiClick();
                skills.TryUseSlot(captured);
            }
        });
        trigger.triggers.Add(entry);

        return new SlotView
        {
            background = background,
            icon = icon,
            cooldown = cooldown,
            outline = outline,
            key = key,
            name = name,
            status = status
        };
    }

    static Color mutedHint()
    {
        return new Color(0.9f, 0.92f, 0.86f, 1f);
    }

    static Image CreateBar(RectTransform parent, string name, Color color, Vector2 size)
    {
        Image image = CreateImage(parent, name, color);
        image.raycastTarget = false;
        RectTransform rt = image.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return image;
    }

    static Image CreateImage(RectTransform parent, string name, Color color)
    {
        if (solidSprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            solidSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            solidSprite.name = "SkillHudSolid";
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Image image = go.GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.sprite = solidSprite;
        image.color = color;
        return image;
    }
}
