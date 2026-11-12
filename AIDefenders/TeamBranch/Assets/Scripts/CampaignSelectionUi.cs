using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Chapter cards and stage route use actual game screenshots, never illustrated map substitutes.
public class CampaignSelectionUi
{
    readonly Transform root;
    readonly CampaignStageEntry[] stages;
    readonly bool requireUnlocks;
    readonly Action home;
    readonly Action<string> load;
    RectTransform content;
    Text title;
    Text steps;
    Text footer;
    int chapter = -1;

    public CampaignSelectionUi(Transform root, CampaignStageEntry[] stages, bool requireUnlocks, Action home, Action<string> load)
    {
        this.root = root;
        this.stages = stages;
        this.requireUnlocks = requireUnlocks;
        this.home = home;
        this.load = load;
        var backdrop = UiFont.AddImage(root, "CampaignBackground", UiSkin.Panel);
        UiFont.Stretch(backdrop.rectTransform);
        var line = UiFont.AddImage(root, "HeaderRule", UiSkin.Sky);
        UiSkin.Place(line.rectTransform, new Vector2(.5f, 1), new Vector2(0, -150), new Vector2(1780, 2));
        UiSkin.Button(root, "Back", "‹", new Color32(102, 94, 131, 255), new Vector2(0, 1), new Vector2(110, -90), new Vector2(100, 80)).onClick.AddListener(() => { if (chapter < 0) home(); else ShowChapters(); });
        UiSkin.Button(root, "Home", "홈", UiSkin.Blue, new Vector2(1, 1), new Vector2(-110, -90), new Vector2(110, 80)).onClick.AddListener(() => home());
        title = Label(root, "Title", "챕터 선택", new Vector2(0, 1), new Vector2(550, -90), new Vector2(660, 70), 42);
        title.alignment = TextAnchor.MiddleLeft;
        steps = Label(root, "Steps", "", new Vector2(1, 1), new Vector2(-480, -90), new Vector2(540, 50), 22);
        steps.color = UiSkin.Muted;
        var go = new GameObject("Content", typeof(RectTransform));
        go.transform.SetParent(root, false);
        content = go.GetComponent<RectTransform>();
        UiSkin.Place(content, new Vector2(.5f, .48f), Vector2.zero, new Vector2(1780, 740));
        footer = Label(root, "Footer", "", new Vector2(.5f, 0), new Vector2(0, 62), new Vector2(1500, 50), 23);
        footer.color = UiSkin.Muted;
        ShowChapters();
    }

    void Clear()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i).gameObject;
            child.SetActive(false);
            UnityEngine.Object.Destroy(child);
        }
    }

    public void ShowChapters()
    {
        chapter = -1;
        Clear();
        title.text = "챕터 선택";
        steps.text = "01  챕터 선택   /   02  스테이지 선택";
        footer.text = "챕터를 선택하면 해당 지역의 스테이지가 열립니다";
        var ids = new List<int>();
        foreach (var stage in stages) if (stage != null && !ids.Contains(stage.chapterId)) ids.Add(stage.chapterId);
        ids.Sort();
        Label(content, "Prompt", "어떤 이야기 속으로 떠날까요?", new Vector2(.5f, 1), new Vector2(0, -18), new Vector2(1500, 60), 29);
        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            var entries = Entries(id);
            float width = Mathf.Min(540, 1740f / ids.Count - 30);
            var card = UiFont.AddImage(content, "Chapter_" + id, UiSkin.Navy);
            UiSkin.Place(card.rectTransform, new Vector2((i + .5f) / ids.Count, .46f), Vector2.zero, new Vector2(width, 620));
            var outline = card.GetComponent<Outline>();
            if (outline != null) { outline.effectColor = UiSkin.Sky; outline.effectDistance = new Vector2(3, -3); }
            var preview = UiSkin.Preview(card.transform, entries[0].sceneName, new Vector2(.5f, 1), new Vector2(width - 24, (width - 24) * .5625f));
            if (preview != null) { preview.rectTransform.pivot = new Vector2(.5f, 1); preview.rectTransform.anchoredPosition = new Vector2(0, -12); }
            var badge = UiFont.AddImage(card.transform, "ChapterBadge", UiSkin.Gold);
            UiSkin.Place(badge.rectTransform, new Vector2(0, 1), new Vector2(110, -46), new Vector2(188, 50));
            Label(badge.transform, "Number", "CHAPTER " + id.ToString("00"), Vector2.one * .5f, Vector2.zero, new Vector2(184, 50), 23);
            Label(card.transform, "Name", entries[0].chapterName, new Vector2(.5f, .43f), Vector2.zero, new Vector2(width - 40, 70), 44);
            var description = Label(card.transform, "Description", Description(id), new Vector2(.5f, .30f), Vector2.zero, new Vector2(width - 40, 65), 23);
            description.color = UiSkin.Muted;
            Label(card.transform, "Available", entries.Count + "개 스테이지", new Vector2(.32f, .12f), Vector2.zero, new Vector2(250, 60), 22);
            UiSkin.Button(card.transform, "SelectChapter", "선택  ›", UiSkin.Green, new Vector2(.77f, .12f), Vector2.zero, new Vector2(175, 75)).onClick.AddListener(() => ShowStages(id, 0));
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            var motion = card.gameObject.AddComponent<ChapterCardMotion>();
            button.onClick.AddListener(motion.Select);
        }
    }

    public void ShowStages(int id, int selected = 0)
    {
        var entries = Entries(id);
        if (entries.Count == 0) return;
        chapter = id;
        selected = Mathf.Clamp(selected, 0, entries.Count - 1);
        var stage = entries[selected];
        Clear();
        title.text = stage.chapterName;
        steps.text = "01  챕터 선택   /   02  스테이지 선택";
        footer.text = stage.chapterName + "   /   스테이지 선택";
        var map = UiFont.AddImage(content, "StageMap", UiSkin.Navy);
        UiSkin.Place(map.rectTransform, new Vector2(0, .5f), new Vector2(535, 0), new Vector2(1070, 740));
        // 16:9 screenshot stays undistorted; the route sits on its own navy panel below.
        var preview = UiSkin.Preview(map.transform, stage.sceneName, new Vector2(.5f, 1), new Vector2(1046, 588));
        if (preview != null) { preview.rectTransform.pivot = new Vector2(.5f, 1); preview.rectTransform.anchoredPosition = new Vector2(0, -12); }
        var shade = UiFont.AddImage(map.transform, "RoutePanel", new Color(.025f, .09f, .17f, .88f));
        UiSkin.Place(shade.rectTransform, new Vector2(.5f, 0), new Vector2(0, 163), new Vector2(1046, 302));
        Label(map.transform, "ChapterName", "CHAPTER " + id.ToString("00") + "  /  " + stage.chapterName, new Vector2(.5f, 1), new Vector2(0, -55), new Vector2(1000, 60), 30);
        int slots = Mathf.Max(3, entries.Count);
        var route = UiFont.AddImage(map.transform, "RouteLine", UiSkin.Sky);
        UiSkin.Place(route.rectTransform, new Vector2(.5f, 0), new Vector2(0, 215), new Vector2(720, 5));
        for (int i = 0; i < slots; i++)
        {
            int index = i;
            bool available = i < entries.Count;
            bool unlocked = available && (!requireUnlocks || GameProgress.IsStageUnlocked(entries[i].sceneName));
            bool active = i == selected;
            var node = new GameObject("StageNode_" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(CampaignHexagon), typeof(Button));
            node.transform.SetParent(map.transform, false);
            UiSkin.Place(node.GetComponent<RectTransform>(), new Vector2((i + 1f) / (slots + 1), 0), new Vector2(0, i % 2 == 0 ? 232 : 192), new Vector2(166, 184));
            var graphic = node.GetComponent<CampaignHexagon>();
            graphic.color = active ? UiSkin.Sky : unlocked ? UiSkin.Panel : new Color32(72, 100, 121, 255);
            var number = Label(node.transform, "Number", (i + 1).ToString(), new Vector2(.5f, .60f), Vector2.zero, new Vector2(140, 75), 58);
            number.color = unlocked ? Color.white : UiSkin.Muted;
            var state = Label(node.transform, "State", !available ? "준비 중" : !unlocked ? "잠김" : active ? "선택됨" : "선택", new Vector2(.5f, .29f), Vector2.zero, new Vector2(150, 40), 22);
            state.color = active ? UiSkin.Gold : UiSkin.Muted;
            var button = node.GetComponent<Button>();
            button.targetGraphic = graphic;
            button.interactable = unlocked;
            if (unlocked) button.onClick.AddListener(() => ShowStages(id, index));
            if (available) Label(map.transform, "StageName_" + i, TimeOfDay(entries[i]), new Vector2((i + 1f) / (slots + 1), 0), new Vector2(0, 63), new Vector2(200, 45), 25);
        }
        var detail = UiFont.AddImage(content, "StageDetails", UiSkin.Navy);
        UiSkin.Place(detail.rectTransform, new Vector2(1, .5f), new Vector2(-330, 0), new Vector2(660, 740));
        Label(detail.transform, "StageTitle", "STAGE " + (selected + 1).ToString("00") + "  /  " + TimeOfDay(stage), new Vector2(.5f, .88f), Vector2.zero, new Vector2(610, 70), 32);
        var rule = UiFont.AddImage(detail.transform, "Rule", UiSkin.Sky);
        UiSkin.Place(rule.rectTransform, new Vector2(.5f, .78f), Vector2.zero, new Vector2(550, 2));
        Label(detail.transform, "MissionLabel", "방어 목표", new Vector2(.5f, .68f), Vector2.zero, new Vector2(550, 55), 26);
        Label(detail.transform, "Mission", id == 1 ? "목장을 지켜라" : id == 2 ? "전초기지를 사수하라" : "대피소를 지켜라", new Vector2(.5f, .57f), Vector2.zero, new Vector2(580, 75), 38);
        var desc = Label(detail.transform, "Description", stage.description + "\n\n" + Description(id), new Vector2(.5f, .39f), Vector2.zero, new Vector2(570, 140), 25);
        desc.color = UiSkin.Muted;
        desc.horizontalOverflow = HorizontalWrapMode.Wrap;
        bool canPlay = !requireUnlocks || GameProgress.IsStageUnlocked(stage.sceneName);
        var fight = UiSkin.Button(detail.transform, "StartBattle", canPlay ? "전투 시작  ›" : "잠김", UiSkin.Gold, new Vector2(.5f, .14f), Vector2.zero, new Vector2(550, 100));
        fight.interactable = canPlay;
        fight.onClick.AddListener(() => load(stage.sceneName));
    }

    List<CampaignStageEntry> Entries(int id)
    {
        var result = new List<CampaignStageEntry>();
        foreach (var stage in stages) if (stage != null && stage.chapterId == id) result.Add(stage);
        return result;
    }

    static string Description(int id) { return id == 1 ? "넓은 초원과 숲, 고요한 호숫가" : id == 2 ? "모래 언덕 너머, 사막의 전초기지" : "안개 낀 거리, 도시의 마지막 대피소"; }
    static string TimeOfDay(CampaignStageEntry entry) { return entry.sceneName == "Grassfield_2" ? "저녁" : entry.sceneName == "Grassfield_3" ? "밤" : "낮"; }
    static Text Label(Transform parent, string name, string text, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
    {
        var label = UiFont.AddText(parent, name, text, fontSize, TextAnchor.MiddleCenter, Color.white, UiType.BodyBold);
        UiSkin.Place(label.rectTransform, anchor, position, size);
        return label;
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public class CampaignHexagon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Ring(mesh, 1f, new Color32(139, 221, 249, 255));
        Ring(mesh, .91f, color);
    }

    void Ring(VertexHelper mesh, float scale, Color tint)
    {
        var rect = rectTransform.rect;
        int start = mesh.currentVertCount;
        mesh.AddVert(rect.center, tint, Vector2.zero);
        for (int i = 0; i <= 6; i++)
        {
            float angle = (90 + 60 * i) * Mathf.Deg2Rad;
            mesh.AddVert(rect.center + new Vector2(Mathf.Cos(angle) * rect.width * .55f, Mathf.Sin(angle) * rect.height * .5f) * scale, tint, Vector2.zero);
            if (i > 0) mesh.AddTriangle(start, start + i, start + i + 1);
        }
    }
}
