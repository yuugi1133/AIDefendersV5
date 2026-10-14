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

    struct SlotView
    {
        public Image background;
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
        barRt.sizeDelta = new Vector2(totalWidth, slotSize);

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
        hintRt.anchoredPosition = new Vector2(0f, bottom + slotSize + 8f);
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
            slot.name.text = "빈 칸";
            slot.name.color = new Color(1f, 1f, 1f, 0.35f);
            slot.status.text = "";
            slot.cooldown.fillAmount = 0f;
            return;
        }

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
        Image background = UiFont.AddImage(parent, "Slot_" + index, new Color(0.06f, 0.08f, 0.1f, 0.78f));
        LayoutElement layout = background.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = size;
        layout.minHeight = size;
        layout.preferredWidth = size;
        layout.preferredHeight = size;

        Outline outline = background.gameObject.AddComponent<Outline>();
        outline.effectColor = readyBorder;
        outline.effectDistance = new Vector2(2f, -2f);

        Image cooldown = UiFont.AddImage(background.rectTransform, "Cooldown", new Color(0f, 0f, 0f, 0.55f));
        cooldown.raycastTarget = false;
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Vertical;
        cooldown.fillOrigin = (int)Image.OriginVertical.Bottom;
        cooldown.fillAmount = 0f;
        UiFont.Stretch(cooldown.rectTransform);

        Text key = UiFont.AddText(background.rectTransform, "Key", "Q", 16, TextAnchor.UpperLeft, Color.white, UiType.BodyBold);
        key.raycastTarget = false;
        RectTransform keyRt = key.rectTransform;
        keyRt.anchorMin = new Vector2(0f, 0.7f);
        keyRt.anchorMax = new Vector2(1f, 1f);
        keyRt.offsetMin = new Vector2(8f, 0f);
        keyRt.offsetMax = new Vector2(-8f, -6f);

        Text name = UiFont.AddText(background.rectTransform, "Name", "", 16, TextAnchor.MiddleLeft, Color.white, UiType.BodyMedium);
        name.raycastTarget = false;
        name.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform nameRt = name.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 0.28f);
        nameRt.anchorMax = new Vector2(1f, 0.72f);
        nameRt.offsetMin = new Vector2(8f, 0f);
        nameRt.offsetMax = new Vector2(-8f, 0f);

        Text status = UiFont.AddText(background.rectTransform, "Status", "", 14, TextAnchor.LowerLeft, mutedHint(), UiType.Body);
        status.raycastTarget = false;
        RectTransform statusRt = status.rectTransform;
        statusRt.anchorMin = new Vector2(0f, 0f);
        statusRt.anchorMax = new Vector2(1f, 0.3f);
        statusRt.offsetMin = new Vector2(8f, 6f);
        statusRt.offsetMax = new Vector2(-8f, 0f);

        int captured = index;
        EventTrigger trigger = background.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener(_ =>
        {
            if (skills != null)
                skills.TryUseSlot(captured);
        });
        trigger.triggers.Add(entry);

        return new SlotView
        {
            background = background,
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
        Image image = UiFont.AddImage(parent, name, color);
        image.raycastTarget = false;
        RectTransform rt = image.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return image;
    }
}
