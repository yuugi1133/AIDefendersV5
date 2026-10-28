using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudModelSelectUi
{
    readonly Color panelColor = new Color(0.12f, 0.14f, 0.18f, 0.97f);
    readonly Color rowColor = new Color(0.22f, 0.24f, 0.28f, 1f);
    readonly Color selectedColor = new Color(0.32f, 0.52f, 0.78f, 1f);
    readonly Color textColor = Color.white;
    readonly Color mutedColor = new Color(0.78f, 0.8f, 0.84f, 1f);
    readonly Color errorColor = new Color(1f, 0.45f, 0.4f, 1f);

    readonly GameManager gameManager;
    readonly GameObject root;
    readonly GameObject selectView;
    readonly GameObject waitView;
    readonly Text titleText;
    readonly Text waitStatus;
    readonly Text errorText;
    readonly Transform listContent;
    readonly Button applyButton;
    readonly List<Image> rowImages = new List<Image>();
    int lastCount = -1;
    int lastSelected = -999;

    public HudModelSelectUi(RectTransform canvasRoot, GameManager gameManager)
    {
        this.gameManager = gameManager;

        Image dim = UiFont.AddImage(canvasRoot, "ModelSelect", new Color(0.04f, 0.05f, 0.07f, 0.78f));
        UiFont.Stretch(dim.rectTransform);
        root = dim.gameObject;

        Image card = UiFont.AddImage(dim.rectTransform, "Card", panelColor);
        RectTransform cardRt = card.rectTransform;
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(680f, 460f);

        titleText = UiFont.AddText(cardRt, "Title", "YOLO 모델 선택", 30, TextAnchor.MiddleLeft, textColor, UiType.Title);
        titleText.raycastTarget = false;
        RectTransform titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -18f);
        titleRt.sizeDelta = new Vector2(-48f, 44f);

        selectView = new GameObject("Select");
        RectTransform selectRt = selectView.AddComponent<RectTransform>();
        selectRt.SetParent(cardRt, false);
        UiFont.Stretch(selectRt);
        selectRt.offsetMin = new Vector2(24f, 24f);
        selectRt.offsetMax = new Vector2(-24f, -70f);

        Text help = UiFont.AddText(selectRt, "Help", "이 스테이지에서 사용할 학습 모델을 고르세요.\n학습이 끝난 데이터셋(best.pt 또는 last.pt)만 표시됩니다.", 18, TextAnchor.UpperLeft, mutedColor, UiType.Body);
        help.raycastTarget = false;
        help.horizontalOverflow = HorizontalWrapMode.Wrap;
        help.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform helpRt = help.rectTransform;
        helpRt.anchorMin = new Vector2(0f, 1f);
        helpRt.anchorMax = new Vector2(1f, 1f);
        helpRt.pivot = new Vector2(0.5f, 1f);
        helpRt.anchoredPosition = Vector2.zero;
        helpRt.sizeDelta = new Vector2(0f, 52f);

        Image listFrame = UiFont.AddImage(selectRt, "List", new Color(0.07f, 0.08f, 0.1f, 1f));
        RectTransform listRt = listFrame.rectTransform;
        listRt.anchorMin = new Vector2(0f, 0f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.offsetMin = new Vector2(0f, 86f);
        listRt.offsetMax = new Vector2(0f, -60f);
        listFrame.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = listFrame.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        GameObject content = new GameObject("Content");
        RectTransform contentRt = content.AddComponent<RectTransform>();
        contentRt.SetParent(listRt, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRt;
        scroll.viewport = listRt;
        listContent = contentRt;

        errorText = UiFont.AddText(selectRt, "Error", "", 16, TextAnchor.MiddleLeft, errorColor, UiType.Body);
        errorText.raycastTarget = false;
        RectTransform errorRt = errorText.rectTransform;
        errorRt.anchorMin = new Vector2(0f, 0f);
        errorRt.anchorMax = new Vector2(1f, 0f);
        errorRt.pivot = new Vector2(0.5f, 0f);
        errorRt.anchoredPosition = new Vector2(0f, 48f);
        errorRt.sizeDelta = new Vector2(0f, 28f);

        applyButton = CreateBottomButton(selectRt, "Apply", "적용하고 서버 시작", 0f, 0.62f, 0f, -6f);
        applyButton.onClick.AddListener(() =>
        {
            GameSfx.PlayUiClick();
            gameManager.ApplySelectedModel();
        });
        Button refresh = CreateBottomButton(selectRt, "Refresh", "목록 새로고침", 0.62f, 1f, 6f, 0f);
        refresh.onClick.AddListener(() =>
        {
            GameSfx.PlayUiClick();
            gameManager.RefreshTrainedModels();
            lastCount = -1;
        });

        waitView = new GameObject("Wait");
        RectTransform waitRt = waitView.AddComponent<RectTransform>();
        waitRt.SetParent(cardRt, false);
        UiFont.Stretch(waitRt);
        waitRt.offsetMin = new Vector2(24f, 24f);
        waitRt.offsetMax = new Vector2(-24f, -70f);

        Text waitBody = UiFont.AddText(waitRt, "Body", "선택한 모델로 로컬 YOLO 서버를 실행하는 중입니다.\nCMD 창에서 모델 로딩 로그를 확인할 수 있습니다.", 20, TextAnchor.UpperLeft, mutedColor, UiType.Body);
        waitBody.raycastTarget = false;
        waitBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        waitBody.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform waitBodyRt = waitBody.rectTransform;
        waitBodyRt.anchorMin = new Vector2(0f, 1f);
        waitBodyRt.anchorMax = new Vector2(1f, 1f);
        waitBodyRt.pivot = new Vector2(0.5f, 1f);
        waitBodyRt.anchoredPosition = Vector2.zero;
        waitBodyRt.sizeDelta = new Vector2(0f, 80f);

        waitStatus = UiFont.AddText(waitRt, "Status", "서버 응답을 기다리는 중...", 20, TextAnchor.MiddleLeft, textColor, UiType.BodyMedium);
        waitStatus.raycastTarget = false;
        waitStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform waitStatusRt = waitStatus.rectTransform;
        waitStatusRt.anchorMin = new Vector2(0f, 0.35f);
        waitStatusRt.anchorMax = new Vector2(1f, 0.7f);
        waitStatusRt.offsetMin = Vector2.zero;
        waitStatusRt.offsetMax = Vector2.zero;

        Text waitFoot = UiFont.AddText(waitRt, "Foot", "서버가 준비되면 게임이 시작됩니다.", 18, TextAnchor.LowerLeft, mutedColor, UiType.Body);
        waitFoot.raycastTarget = false;
        RectTransform waitFootRt = waitFoot.rectTransform;
        waitFootRt.anchorMin = new Vector2(0f, 0f);
        waitFootRt.anchorMax = new Vector2(1f, 0.2f);
        waitFootRt.offsetMin = Vector2.zero;
        waitFootRt.offsetMax = Vector2.zero;
    }

    public void Tick()
    {
        if (gameManager == null)
        {
            SetActive(root, false);
            return;
        }

        bool open = gameManager.IsModelPromptOpen;
        SetActive(root, open);
        if (!open)
            return;

        root.transform.SetAsLastSibling();
        bool waiting = gameManager.IsWaitingForServer;
        titleText.text = waiting ? "YOLO 서버 시작" : "YOLO 모델 선택";
        SetActive(selectView, !waiting);
        SetActive(waitView, waiting);

        if (waiting)
        {
            waitStatus.text = string.IsNullOrEmpty(gameManager.StatusMessage)
                ? "서버 응답을 기다리는 중..."
                : gameManager.StatusMessage;
            return;
        }

        IReadOnlyList<PythonRuntime.TrainedModelInfo> models = gameManager.TrainedModels;
        int count = models != null ? models.Count : 0;
        if (count != lastCount)
        {
            RebuildList(models);
            lastCount = count;
            lastSelected = -999;
        }

        int selected = gameManager.SelectedModelIndex;
        if (selected != lastSelected)
        {
            lastSelected = selected;
            for (int i = 0; i < rowImages.Count; i++)
            {
                if (rowImages[i] != null)
                    rowImages[i].color = i == selected ? selectedColor : rowColor;
            }
        }

        errorText.text = gameManager.StatusMessage ?? "";
        applyButton.interactable = selected >= 0 && selected < count;
    }

    void RebuildList(IReadOnlyList<PythonRuntime.TrainedModelInfo> models)
    {
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Object.Destroy(listContent.GetChild(i).gameObject);
        rowImages.Clear();

        int count = models != null ? models.Count : 0;
        if (count == 0)
        {
            Text empty = UiFont.AddText(listContent, "Empty", "학습된 모델이 없습니다. DatasetGeneratorScene에서 먼저 학습하세요.", 18, TextAnchor.MiddleLeft, mutedColor, UiType.Body);
            empty.raycastTarget = false;
            empty.horizontalOverflow = HorizontalWrapMode.Wrap;
            LayoutElement emptyLayout = empty.gameObject.AddComponent<LayoutElement>();
            emptyLayout.minHeight = 48f;
            emptyLayout.preferredHeight = 64f;
            return;
        }

        for (int i = 0; i < count; i++)
        {
            PythonRuntime.TrainedModelInfo model = models[i];
            Image row = UiFont.AddImage(listContent, "Model_" + i, rowColor);
            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 44f;
            layout.preferredHeight = 44f;
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            int index = i;
            button.onClick.AddListener(() =>
            {
                GameSfx.PlayUiClick();
                gameManager.SelectModel(index);
            });

            string label = model.DatasetName + "  (" + model.WeightsFile + ")";
            Text text = UiFont.AddText(row.rectTransform, "Label", label, 18, TextAnchor.MiddleLeft, textColor, UiType.BodyMedium);
            text.raycastTarget = false;
            UiFont.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(14f, 0f);
            text.rectTransform.offsetMax = new Vector2(-14f, 0f);
            rowImages.Add(row);
        }
    }

    static Button CreateBottomButton(RectTransform parent, string name, string label, float minX, float maxX, float left, float right)
    {
        Image image = UiFont.AddImage(parent, name, Color.white);
        RectTransform rt = image.rectTransform;
        rt.anchorMin = new Vector2(minX, 0f);
        rt.anchorMax = new Vector2(maxX, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 44f);
        rt.offsetMin = new Vector2(left, 0f);
        rt.offsetMax = new Vector2(right, 44f);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text text = UiFont.AddText(rt, "Label", label, 18, TextAnchor.MiddleCenter, new Color(0.12f, 0.13f, 0.15f, 1f), UiType.BodyMedium);
        text.raycastTarget = false;
        UiFont.Stretch(text.rectTransform);
        return button;
    }

    static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
