using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

public class HudDatasetUi
{
    readonly Color panelColor = UiSkin.Panel;
    readonly Color rowColor = UiSkin.Blue;
    readonly Color selectedColor = UiSkin.Sky;
    readonly Color textColor = Color.white;
    readonly Color mutedColor = UiSkin.Muted;
    readonly Color errorColor = new Color(1f, 0.45f, 0.4f, 1f);
    readonly Color buttonLabel = Color.white;
    readonly Color insetColor = UiSkin.Navy;

    readonly DatasetManager manager;
    readonly Canvas canvas;

    GameObject listRoot;
    GameObject headerRoot;
    Text headerBackLabel;
    Text headerStatus;
    Text overviewSelected;
    Text overviewCount;
    Button overviewEdit;
    GameObject editRoot;
    Transform datasetListContent;
    Transform sampleListContent;
    Transform targetListContent;
    Transform terrainListContent;
    Transform prefabPickContent;
    Transform terrainPickContent;
    Text editTitle;
    Text previewHint;
    Text framingHint;
    Text generateHint;
    Text manualHint;
    Text trainHint;
    RawImage previewImage;
    RectTransform previewBoxRoot;
    readonly List<RectTransform> previewBoxPool = new List<RectTransform>();
    InputField minCountField;
    InputField maxCountField;
    InputField jpegField;
    Button generateButton;
    Button manualButton;
    Button trainButton;
    GameObject manualRoot;
    GameObject manualPanel;
    GameObject manualToggle;
    Text manualToggleText;
    RectTransform viewfinderRt;
    Slider sunSlider;
    Text sunSliderLabel;
    Text manualStatus;
    Text manualError;
    Transform manualClassRow;
    Transform manualTerrainRow;
    Transform manualSplitRow;
    string lastManualClassKey;
    string lastManualTerrainKey;
    bool lastManualUiOpen;
    GameObject manualReviewRoot;
    RawImage reviewImage;
    RectTransform reviewBoxRoot;
    readonly List<RectTransform> manualReviewBoxPool = new List<RectTransform>();
    int lastManualReviewVersion = -1;

    GameObject dialogRoot;
    GameObject nameDialog;
    RectTransform nameDialogRt;
    GameObject createExtras;
    Transform createClassListContent;
    Transform createStageListContent;
    Text nameConfirmLabel;
    GameObject prefabDialog;
    GameObject terrainDialog;
    GameObject generateDialog;
    GameObject generateProgressDialog;
    GameObject trainDialog;
    GameObject trainProgressDialog;
    GameObject trainResultDialog;
    Text nameDialogTitle;
    Text nameDialogError;
    InputField nameField;
    InputField trainCountField;
    InputField valCountField;
    InputField testCountField;
    InputField epochField;
    InputField batchField;
    Text generateError;
    Text generateProgressStatus;
    Text generateProgressCount;
    Text generateProgressPercent;
    Image generateProgressFill;
    Text trainError;
    Text trainHelp;
    Text resultTitle;
    Text resultBody;
    Text trainProgressTitle;
    Text trainProgressStatus;
    Image trainProgressFill;
    RawImage trainGraphImage;
    Text trainStopLabel;
    Texture2D trainGraphTexture;
    int lastTrainGraphEpoch = -1;

    string lastDatasetKey;
    string lastSampleKey;
    string lastTargetKey;
    string lastTerrainKey;
    string lastPrefabPickKey;
    string lastCreateClassKey;
    string lastCreateStageKey;
    string lastTerrainPickKey;
    string lastSelectedSampleId;
    string lastSelectedDataset;
    string lastFramingText;
    int lastPreviewVersion = -1;
    int editingPreviewBox = -1;
    readonly List<Image> datasetRowImages = new List<Image>();
    readonly List<string> datasetRowIds = new List<string>();
    readonly List<Image> sampleRowImages = new List<Image>();
    readonly List<string> sampleRowIds = new List<string>();
    readonly HashSet<string> checkedSampleIds = new HashSet<string>();

    public HudDatasetUi(DatasetManager manager)
    {
        this.manager = manager;
        EnsureEventSystem();
        DestroyOldCanvases();
        canvas = BuildCanvas(manager.transform);
        BuildHeader();
        BuildListView();
        BuildEditView();
        BuildDialogs();
        BuildManualHud();
        ApplyShellVisibility();
    }

    public void Tick()
    {
        ApplyShellVisibility();
        bool edit = manager.editMode;

        if (manager.showManualCaptureDialog)
            RefreshManualHud();
        else if (edit)
            RefreshEdit();
        else
            RefreshDatasetList();

        RefreshDialogs();
    }

    void ApplyShellVisibility()
    {
        bool edit = manager.editMode;
        bool manual = manager.showManualCaptureDialog;
        if (!edit)
        {
            manager.showGenerateDialog = false;
            manager.showTrainDialog = false;
            manager.showTrainResult = false;
            if (!manager.showNameDialog)
            {
                manager.showPrefabDialog = false;
                manager.showTerrainDialog = false;
            }
        }
        else if (manual)
        {
            manager.showGenerateDialog = false;
            manager.showTrainDialog = false;
            manager.showNameDialog = false;
        }
        else if (manager.showGenerateDialog)
        {
            manager.showTrainDialog = false;
            manager.showNameDialog = false;
        }
        else if (manager.showTrainDialog || manager.waitingForTrain || manager.showTrainResult)
        {
            manager.showGenerateDialog = false;
            manager.showNameDialog = false;
        }

        SetActive(listRoot, !edit && !manual);
        SetActive(editRoot, edit && !manual);
        SetActive(manualRoot, manual);
        SetActive(headerRoot, !manual);
        RefreshHeader();
        if (headerRoot != null)
            headerRoot.transform.SetAsLastSibling();
        if (dialogRoot != null)
            dialogRoot.transform.SetAsLastSibling();
    }

    void RefreshDatasetList()
    {
        int count = manager.datasetNames.Count;
        string first = count > 0 ? manager.datasetNames[0] : "";
        string last = count > 0 ? manager.datasetNames[count - 1] : "";
        string key = count + "|" + first + "|" + last;
        if (key != lastDatasetKey)
        {
            lastDatasetKey = key;
            RebuildDatasetRows();
        }

        RefreshDatasetSelection();
        RefreshOverview();
    }

    void RebuildDatasetRows()
    {
        ClearChildren(datasetListContent);
        datasetRowImages.Clear();
        datasetRowIds.Clear();
        lastSelectedDataset = null;
        if (manager.datasetNames.Count == 0)
        {
            AddMuted(datasetListContent, "비어 있음");
            return;
        }

        for (int i = 0; i < manager.datasetNames.Count; i++)
        {
            string name = manager.datasetNames[i];
            string captured = name;
            Image row = CreateRow(datasetListContent, name, rowColor, 44f, () => manager.selectedDataset = captured);
            Text label = UiFont.AddText(row.rectTransform, "Label", name, 18, TextAnchor.MiddleLeft, textColor, UiType.BodyMedium);
            label.raycastTarget = false;
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(12f, 0f);
            labelRt.offsetMax = new Vector2(-108f, 0f);

            AddRowButton(row.rectTransform, "Edit", "편집", -100f, -54f, () => manager.EnterEditMode(captured));
            AddRowButton(row.rectTransform, "Delete", "삭제", -50f, -8f, () =>
            {
                manager.DeleteDataset(captured);
                lastDatasetKey = null;
            });
            datasetRowImages.Add(row);
            datasetRowIds.Add(name);
        }
    }

    void RefreshDatasetSelection()
    {
        if (lastSelectedDataset == manager.selectedDataset)
            return;
        lastSelectedDataset = manager.selectedDataset;
        for (int i = 0; i < datasetRowImages.Count; i++)
        {
            if (datasetRowImages[i] != null)
                datasetRowImages[i].color = datasetRowIds[i] == lastSelectedDataset ? selectedColor : rowColor;
        }
    }

    void RefreshEdit()
    {
        SetText(editTitle, string.IsNullOrEmpty(manager.editingDataset) ? "샘플" : manager.editingDataset);
        RefreshSampleList();
        RefreshPreview();

        bool generating = manager.IsGenerating;
        bool training = manager.waitingForTrain;
        bool canGenerate = !generating && !training;
        bool canManual = manager.CanManualCapture() && !generating && !training;
        bool canTrain = manager.CanTrainModel() && !generating && !training;
        if (generateButton != null && generateButton.interactable != canGenerate)
            generateButton.interactable = canGenerate;
        if (manualButton != null && manualButton.interactable != canManual)
            manualButton.interactable = canManual;
        if (trainButton != null && trainButton.interactable != canTrain)
            trainButton.interactable = canTrain;
        SetText(generateHint, generating ? "자동 샘플을 생성하는 중입니다." : "");
        SetText(manualHint, canManual || generating || training
            ? ""
            : "클래스와 촬영 카메라가 있어야 합니다.");
        SetText(trainHint, canTrain || generating || training
            ? ""
            : "학습(train)과 검증(val) 샘플이 각각 하나 이상 있어야 합니다.");
    }

    void RefreshSampleList()
    {
        int count = manager.datasetSamples.Count;
        string first = count > 0 && manager.datasetSamples[0] != null ? manager.datasetSamples[0].id : "";
        string last = count > 0 && manager.datasetSamples[count - 1] != null ? manager.datasetSamples[count - 1].id : "";
        string key = count + "|" + first + "|" + last;
        if (key != lastSampleKey)
        {
            lastSampleKey = key;
            RebuildSampleRows();
        }

        RefreshSampleSelection();
    }

    void RebuildSampleRows()
    {
        ClearChildren(sampleListContent);
        sampleRowImages.Clear();
        sampleRowIds.Clear();
        lastSelectedSampleId = null;
        PruneCheckedSamples();
        if (manager.datasetSamples.Count == 0)
        {
            AddMuted(sampleListContent, "비어 있음");
            return;
        }

        for (int i = 0; i < manager.datasetSamples.Count; i++)
        {
            DatasetManager.DatasetSample sample = manager.datasetSamples[i];
            DatasetManager.DatasetSample captured = sample;
            Image row = CreateRow(sampleListContent, sample.id, rowColor, 40f, () =>
            {
                manager.OpenSamplePreview(captured);
                lastPreviewVersion = -1;
            });
            AddSampleCheck(row.rectTransform, sample.id);
            Text label = UiFont.AddText(row.rectTransform, "Label", sample.id, 16, TextAnchor.MiddleLeft, textColor, UiType.Body);
            label.raycastTarget = false;
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(38f, 0f);
            labelRt.offsetMax = new Vector2(-56f, 0f);
            AddRowButton(row.rectTransform, "Delete", "삭제", -50f, -8f, () =>
            {
                manager.DeleteSample(captured);
                lastSampleKey = null;
            });
            sampleRowImages.Add(row);
            sampleRowIds.Add(sample.id);
        }
    }

    void RefreshSampleSelection()
    {
        if (lastSelectedSampleId == manager.selectedSampleId)
            return;
        lastSelectedSampleId = manager.selectedSampleId;
        for (int i = 0; i < sampleRowImages.Count; i++)
        {
            if (sampleRowImages[i] != null)
                sampleRowImages[i].color = sampleRowIds[i] == lastSelectedSampleId ? selectedColor : rowColor;
        }
    }

    void RefreshIncludedTargets()
    {
        string key = manager.includedTargets.Count.ToString();
        for (int i = 0; i < manager.includedTargets.Count; i++)
            key += "|" + (manager.includedTargets[i] != null ? manager.includedTargets[i].HudLabel : "");
        if (key == lastTargetKey)
            return;

        lastTargetKey = key;
        ClearChildren(targetListContent);
        if (manager.includedTargets.Count == 0)
        {
            AddMuted(targetListContent, "비어 있음");
            return;
        }

        for (int i = 0; i < manager.includedTargets.Count; i++)
        {
            Target_Base target = manager.includedTargets[i];
            if (target == null)
                continue;

            int index = i;
            string rowLabel = manager.GetClassIdForTarget(target).ToString(CultureInfo.InvariantCulture) + "  " + target.HudLabel;
            Image row = CreateRow(targetListContent, target.HudLabel, rowColor, 36f);
            Text label = UiFont.AddText(row.rectTransform, "Label", rowLabel, 16, TextAnchor.MiddleLeft, textColor, UiType.Body);
            label.raycastTarget = false;
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(10f, 0f);
            labelRt.offsetMax = new Vector2(-56f, 0f);
            AddRowButton(row.rectTransform, "Delete", "삭제", -50f, -8f, () =>
            {
                if (index >= 0 && index < manager.includedTargets.Count)
                    manager.includedTargets.RemoveAt(index);
                manager.captureSettingsDirty = true;
                manager.RefreshAutoCaptureFraming();
                lastTargetKey = null;
            });
        }
    }

    void RefreshIncludedTerrains()
    {
        string key = manager.includedTerrains.Count.ToString();
        for (int i = 0; i < manager.includedTerrains.Count; i++)
            key += "|" + (manager.includedTerrains[i] != null ? manager.includedTerrains[i].name : "");
        if (key == lastTerrainKey)
            return;

        lastTerrainKey = key;
        ClearChildren(terrainListContent);
        if (manager.includedTerrains.Count == 0)
        {
            AddMuted(terrainListContent, "비어 있음");
            return;
        }

        for (int i = 0; i < manager.includedTerrains.Count; i++)
        {
            GameObject terrain = manager.includedTerrains[i];
            if (terrain == null)
                continue;

            int index = i;
            Image row = CreateRow(terrainListContent, terrain.name, rowColor, 36f);
            Text label = UiFont.AddText(row.rectTransform, "Label", terrain.name, 16, TextAnchor.MiddleLeft, textColor, UiType.Body);
            label.raycastTarget = false;
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(10f, 0f);
            labelRt.offsetMax = new Vector2(-56f, 0f);
            AddRowButton(row.rectTransform, "Delete", "삭제", -50f, -8f, () =>
            {
                if (index >= 0 && index < manager.includedTerrains.Count)
                    manager.includedTerrains.RemoveAt(index);
                manager.captureSettingsDirty = true;
                lastTerrainKey = null;
            });
        }
    }

    void RefreshPreview()
    {
        int version = (manager.previewTexture != null ? 1 : 0)
            + manager.previewBoxes.Count * 17
            + (manager.selectedSampleId ?? "").GetHashCode();
        if (version == lastPreviewVersion)
            return;
        lastPreviewVersion = version;

        Texture2D texture = manager.previewTexture;
        if (previewImage != null)
        {
            previewImage.enabled = texture != null;
            previewImage.texture = texture;
            AspectRatioFitter fitter = previewImage.GetComponent<AspectRatioFitter>();
            if (fitter != null && texture != null && texture.height > 0)
                fitter.aspectRatio = texture.width / (float)texture.height;
        }

        if (previewHint != null)
        {
            if (texture != null)
                previewHint.text = "";
            else
                previewHint.text = "샘플을 클릭하면 미리보기가 표시됩니다.";
        }

        int needed = texture != null ? manager.previewBoxes.Count : 0;
        while (previewBoxPool.Count < needed)
        {
            Image box = UiFont.AddImage(previewBoxRoot, "Box", Color.clear);
            box.raycastTarget = false;
            AddOutlineEdge(box.rectTransform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f));
            AddOutlineEdge(box.rectTransform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
            AddOutlineEdge(box.rectTransform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
            AddOutlineEdge(box.rectTransform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));
            AddBboxHandles(box.rectTransform);
            Text caption = UiFont.AddText(box.rectTransform, "Id", "", 14, TextAnchor.LowerLeft, Color.white, UiType.BodyBold);
            caption.raycastTarget = false;
            RectTransform capRt = caption.rectTransform;
            capRt.anchorMin = new Vector2(0f, 1f);
            capRt.anchorMax = new Vector2(1f, 1f);
            capRt.pivot = new Vector2(0f, 0f);
            capRt.anchoredPosition = new Vector2(2f, 2f);
            capRt.sizeDelta = new Vector2(-4f, 16f);
            previewBoxPool.Add(box.rectTransform);
        }

        for (int i = 0; i < previewBoxPool.Count; i++)
        {
            RectTransform boxRt = previewBoxPool[i];
            bool on = i < needed;
            SetActive(boxRt.gameObject, on);
            if (!on)
                continue;

            DatasetManager.YoloBox box = manager.previewBoxes[i];
            float xMin = Mathf.Clamp01(box.cx - box.w * 0.5f);
            float xMax = Mathf.Clamp01(box.cx + box.w * 0.5f);
            float yMin = Mathf.Clamp01(1f - (box.cy + box.h * 0.5f));
            float yMax = Mathf.Clamp01(1f - (box.cy - box.h * 0.5f));
            boxRt.anchorMin = new Vector2(xMin, yMin);
            boxRt.anchorMax = new Vector2(xMax, yMax);
            boxRt.offsetMin = Vector2.zero;
            boxRt.offsetMax = Vector2.zero;
            BindBboxHandles(boxRt, i);
            Text caption = boxRt.GetComponentInChildren<Text>();
            if (caption != null)
                caption.text = box.classId.ToString();
            TintPreviewBox(boxRt, i, PreviewBoxClassColor(box.classId));
        }
    }

    Color PreviewBoxClassColor(int classId)
    {
        return DatasetManager.PreviewBoxColors[Mathf.Abs(classId) % DatasetManager.PreviewBoxColors.Length];
    }

    void AddBboxHandles(RectTransform boxRt)
    {
        Vector2[] pivots =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        for (int c = 0; c < 4; c++)
        {
            Image pad = UiFont.AddImage(boxRt, "Handle" + c, new Color(1f, 1f, 1f, 0.001f));
            pad.raycastTarget = true;
            pad.type = Image.Type.Simple;
            RectTransform rt = pad.rectTransform;
            rt.anchorMin = pivots[c];
            rt.anchorMax = pivots[c];
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(40f, 40f);
            rt.anchoredPosition = Vector2.zero;
            pad.gameObject.AddComponent<UiBboxHandle>();

            Image knob = UiFont.AddImage(rt, "Knob", Color.white);
            knob.raycastTarget = false;
            knob.type = Image.Type.Simple;
            RectTransform knobRt = knob.rectTransform;
            knobRt.anchorMin = new Vector2(0.5f, 0.5f);
            knobRt.anchorMax = new Vector2(0.5f, 0.5f);
            knobRt.pivot = new Vector2(0.5f, 0.5f);
            knobRt.sizeDelta = new Vector2(14f, 14f);
            knobRt.anchoredPosition = Vector2.zero;
        }
    }

    void BindBboxHandles(RectTransform boxRt, int boxIndex)
    {
        Transform existing = boxRt.Find("Handle0");
        if (existing == null || existing.Find("Knob") == null)
        {
            for (int c = 0; c < 4; c++)
            {
                Transform old = boxRt.Find("Handle" + c);
                if (old != null)
                    Object.Destroy(old.gameObject);
            }
            AddBboxHandles(boxRt);
        }

        for (int c = 0; c < 4; c++)
        {
            Transform child = boxRt.Find("Handle" + c);
            if (child == null)
                continue;
            UiBboxHandle handle = child.GetComponent<UiBboxHandle>();
            if (handle != null)
            {
                handle.hud = this;
                handle.boxIndex = boxIndex;
                handle.corner = c;
            }
        }
    }

    public void SetPreviewBoxEditing(int boxIndex, bool editing)
    {
        int previous = editingPreviewBox;
        editingPreviewBox = editing ? boxIndex : -1;
        if (previous >= 0 && previous < previewBoxPool.Count && previous != editingPreviewBox)
            RefreshPreviewBoxTint(previous);
        if (boxIndex >= 0)
            RefreshPreviewBoxTint(boxIndex);
    }

    void RefreshPreviewBoxTint(int index)
    {
        if (index < 0 || index >= previewBoxPool.Count || index >= manager.previewBoxes.Count)
            return;
        Color color = PreviewBoxClassColor(manager.previewBoxes[index].classId);
        TintPreviewBox(previewBoxPool[index], index, color);
    }

    void TintPreviewBox(RectTransform boxRt, int boxIndex, Color color)
    {
        if (boxIndex == editingPreviewBox)
            color = Color.white;

        Image[] images = boxRt.GetComponentsInChildren<Image>(true);
        for (int e = 1; e < images.Length; e++)
        {
            if (images[e].name.StartsWith("Handle"))
                continue;
            images[e].color = color;
        }

        Text caption = boxRt.GetComponentInChildren<Text>();
        if (caption != null)
            caption.color = color;
    }

    public void DragPreviewBoxCorner(int boxIndex, int corner, Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform root = ActivePreviewBoxRoot;
        if (root == null)
            return;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                root,
                screenPosition,
                eventCamera,
                out local))
            return;

        Rect rect = root.rect;
        if (rect.width < 1f || rect.height < 1f)
            return;

        Vector2 uv = new Vector2(
            Mathf.InverseLerp(rect.xMin, rect.xMax, local.x),
            Mathf.InverseLerp(rect.yMin, rect.yMax, local.y));
        manager.SetPreviewBoxCorner(boxIndex, corner, uv);
        ApplyPreviewBoxRect(boxIndex);
    }

    public void CommitPreviewBoxEdit()
    {
        manager.SavePreviewBoxes();
    }

    RectTransform ActivePreviewBoxRoot
    {
        get { return manager.showManualShotReview && reviewBoxRoot != null ? reviewBoxRoot : previewBoxRoot; }
    }

    List<RectTransform> ActivePreviewBoxPool
    {
        get { return manager.showManualShotReview ? manualReviewBoxPool : previewBoxPool; }
    }

    void ApplyPreviewBoxRect(int index)
    {
        List<RectTransform> pool = ActivePreviewBoxPool;
        if (index < 0 || index >= pool.Count || index >= manager.previewBoxes.Count)
            return;

        DatasetManager.YoloBox box = manager.previewBoxes[index];
        float xMin = Mathf.Clamp01(box.cx - box.w * 0.5f);
        float xMax = Mathf.Clamp01(box.cx + box.w * 0.5f);
        float yMin = Mathf.Clamp01(1f - (box.cy + box.h * 0.5f));
        float yMax = Mathf.Clamp01(1f - (box.cy - box.h * 0.5f));
        RectTransform boxRt = pool[index];
        boxRt.anchorMin = new Vector2(xMin, yMin);
        boxRt.anchorMax = new Vector2(xMax, yMax);
        boxRt.offsetMin = Vector2.zero;
        boxRt.offsetMax = Vector2.zero;
    }

    void RefreshDialogs()
    {
        bool generating = manager.IsGenerating;
        bool any = manager.showNameDialog || manager.showPrefabDialog || manager.showTerrainDialog
            || manager.showGenerateDialog || manager.showTrainDialog
            || manager.waitingForTrain || manager.showTrainResult
            || generating;

        SetActive(dialogRoot, any);
        if (dialogRoot != null)
        {
            for (int i = 0; i < dialogRoot.transform.childCount; i++)
            {
                GameObject child = dialogRoot.transform.GetChild(i).gameObject;
                bool on = false;
                if (child == nameDialog)
                    on = manager.showNameDialog;
                else if (child == prefabDialog)
                    on = manager.showPrefabDialog;
                else if (child == terrainDialog)
                    on = manager.showTerrainDialog;
                else if (child == generateDialog)
                    on = manager.showGenerateDialog && !generating;
                else if (child == generateProgressDialog)
                    on = generating;
                else if (child == trainDialog)
                    on = manager.showTrainDialog;
                else if (child == trainProgressDialog)
                    on = manager.waitingForTrain;
                else if (child == trainResultDialog)
                    on = manager.showTrainResult && !manager.waitingForTrain;
                SetActive(child, any && on);
            }
        }
        if (!any)
            return;

        if (manager.showNameDialog)
        {
            bool create = !manager.nameDialogIsRename;
            if (nameDialogTitle != null)
                nameDialogTitle.text = create ? "새 데이터셋" : "데이터셋 이름 변경";
            if (nameConfirmLabel != null)
                nameConfirmLabel.text = create ? "생성" : "변경";
            if (nameDialogError != null)
                nameDialogError.text = manager.nameDialogError ?? "";
            SyncField(nameField, manager.nameDialogValue ?? "");
            SetActive(createExtras, create);
            if (nameDialogRt != null)
                nameDialogRt.sizeDelta = create ? new Vector2(520f, 680f) : new Vector2(420f, 220f);
            if (create)
            {
                RefreshCreateClasses();
                RefreshCreateStages();
            }
        }

        if (manager.showPrefabDialog)
            RefreshPrefabPick();
        if (manager.showTerrainDialog)
            RefreshTerrainPick();

        if (manager.showGenerateDialog)
        {
            RefreshIncludedTargets();
            RefreshIncludedTerrains();
            SyncField(minCountField, manager.minCaptureCount.ToString());
            SyncField(maxCountField, manager.maxCaptureCount.ToString());
            SyncField(jpegField, manager.jpegQuality.ToString());
            SyncField(trainCountField, manager.pendingTrainCount.ToString());
            SyncField(valCountField, manager.pendingValCount.ToString());
            SyncField(testCountField, manager.pendingTestCount.ToString());
            string framing = string.Format(
                CultureInfo.InvariantCulture,
                "카메라 거리·간격은 표적 크기와 최대 수량으로 자동 조절됩니다.\n현재 거리 {0:0}–{1:0}",
                manager.cameraDistanceRange.x,
                manager.cameraDistanceRange.y);
            if (framing != lastFramingText)
            {
                lastFramingText = framing;
                SetText(framingHint, framing);
            }
            if (generateError != null)
                generateError.text = manager.generateDialogError ?? "";
        }

        if (manager.showTrainDialog)
        {
            SyncField(epochField, manager.trainEpochs.ToString());
            SyncField(batchField, manager.trainBatchSize.ToString());
            bool venvReady = PythonRuntime.IsVenvReady();
            if (trainHelp != null)
            {
                trainHelp.text = venvReady
                    ? "Python " + PythonRuntime.RequiredVersionLabel + " 가상환경으로 YOLOv8n을 학습합니다."
                    : "가상환경(.venv)이 없습니다. 메인 메뉴에서 라이브러리를 설치하세요.";
                trainHelp.color = venvReady ? mutedColor : errorColor;
            }
            if (trainError != null)
                trainError.text = manager.trainDialogError ?? "";
        }

        if (generating)
            RefreshGenerateProgress();
        if (manager.waitingForTrain)
            RefreshTrainProgress();
        if (manager.showTrainResult)
            RefreshTrainResult();
    }

    void RefreshGenerateProgress()
    {
        int total = Mathf.Max(0, manager.generateTotal);
        int done = Mathf.Clamp(manager.generateDone, 0, Mathf.Max(total, manager.generateDone));
        float fill = total > 0 ? Mathf.Clamp01((float)done / total) : 0f;
        string split = SplitLabel(manager.generateSplit);
        int splitCount = Mathf.Max(0, manager.generateSplitCount);
        int splitIndex = Mathf.Clamp(manager.generateSplitIndex, 0, Mathf.Max(splitCount, manager.generateSplitIndex));

        if (generateProgressStatus != null)
        {
            generateProgressStatus.text = string.IsNullOrEmpty(split)
                ? "장면을 준비하고 있습니다…"
                : split + " 샘플  " + splitIndex + " / " + splitCount;
        }
        if (generateProgressCount != null)
            generateProgressCount.text = total > 0 ? "전체  " + done + " / " + total : "전체  —";
        if (generateProgressPercent != null)
            generateProgressPercent.text = total > 0 ? Mathf.RoundToInt(fill * 100f) + "%" : "0%";
        if (generateProgressFill != null)
            generateProgressFill.fillAmount = fill;
    }

    static string SplitLabel(string split)
    {
        if (split == "train")
            return "학습(train)";
        if (split == "val")
            return "검증(val)";
        if (split == "test")
            return "테스트(test)";
        return "";
    }

    void RefreshPrefabPick()
    {
        bool forCreate = manager.prefabPickForCreate;
        List<Target_Base> selected = forCreate ? manager.createClassTargets : manager.includedTargets;
        string key = (forCreate ? "c" : "e") + "|" + manager.prefabCatalog.Count + "|" + selected.Count + "|" + manager.datasetClasses.Count;
        for (int i = 0; i < manager.prefabCatalog.Count; i++)
        {
            if (manager.prefabCatalog[i] != null)
                key += manager.prefabCatalog[i].HudLabel;
        }
        if (key == lastPrefabPickKey)
            return;
        lastPrefabPickKey = key;
        ClearChildren(prefabPickContent);
        int shown = 0;
        for (int i = 0; i < manager.prefabCatalog.Count; i++)
        {
            Target_Base prefab = manager.prefabCatalog[i];
            if (prefab == null || selected.Contains(prefab))
                continue;
            if (!forCreate && manager.datasetClasses.Count > 0 && !manager.IsDatasetClass(prefab))
                continue;
            shown++;
            Target_Base captured = prefab;
            Button button = CreateFullButton(prefabPickContent, captured.HudLabel, 36f);
            button.onClick.AddListener(() =>
            {
                GameSfx.PlayUiClick();
                if (manager.prefabPickForCreate)
                {
                    manager.AddCreateClassTarget(captured);
                    lastCreateClassKey = null;
                }
                else
                {
                    if (manager.datasetClasses.Count > 0 && !manager.IsDatasetClass(captured))
                        return;
                    manager.includedTargets.Add(captured);
                    manager.captureSettingsDirty = true;
                    manager.RefreshAutoCaptureFraming();
                    lastTargetKey = null;
                }

                manager.showPrefabDialog = false;
                lastPrefabPickKey = null;
            });
        }
        if (shown == 0)
            AddMuted(prefabPickContent, forCreate ? "추가할 프리팹이 없습니다." : "이 데이터셋에 더 넣을 클래스가 없습니다.");
    }

    void RefreshCreateClasses()
    {
        string key = manager.createClassTargets.Count.ToString();
        for (int i = 0; i < manager.createClassTargets.Count; i++)
        {
            Target_Base target = manager.createClassTargets[i];
            key += "|" + (target != null ? target.HudLabel : "");
        }
        if (key == lastCreateClassKey)
            return;
        lastCreateClassKey = key;
        ClearChildren(createClassListContent);
        if (manager.createClassTargets.Count == 0)
        {
            AddMuted(createClassListContent, "비어 있음");
            return;
        }

        for (int i = 0; i < manager.createClassTargets.Count; i++)
        {
            Target_Base target = manager.createClassTargets[i];
            if (target == null)
                continue;

            int index = i;
            string rowLabel = index.ToString(CultureInfo.InvariantCulture) + "  " + target.HudLabel;
            Image row = CreateRow(createClassListContent, target.HudLabel, rowColor, 36f);
            Text label = UiFont.AddText(row.rectTransform, "Label", rowLabel, 16, TextAnchor.MiddleLeft, textColor, UiType.Body);
            label.raycastTarget = false;
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(10f, 0f);
            labelRt.offsetMax = new Vector2(-56f, 0f);
            AddRowButton(row.rectTransform, "Delete", "삭제", -50f, -8f, () =>
            {
                manager.RemoveCreateClassTarget(index);
                lastCreateClassKey = null;
            });
        }
    }

    void RefreshCreateStages()
    {
        string key = manager.stageCatalog.Count.ToString();
        for (int i = 0; i < manager.stageCatalog.Count; i++)
        {
            if (manager.stageCatalog[i] != null)
                key += manager.stageCatalog[i].displayName;
        }
        if (key == lastCreateStageKey)
            return;
        lastCreateStageKey = key;
        ClearChildren(createStageListContent);
        if (manager.stageCatalog.Count == 0)
        {
            AddMuted(createStageListContent, "스테이지가 없습니다.");
            return;
        }

        for (int i = 0; i < manager.stageCatalog.Count; i++)
        {
            DatasetStageCatalog.Entry stage = manager.stageCatalog[i];
            if (stage == null)
                continue;

            DatasetStageCatalog.Entry captured = stage;
            Button button = CreateFullButton(createStageListContent, captured.displayName, 36f);
            button.onClick.AddListener(() =>
            {
                GameSfx.PlayUiClick();
                manager.AddCreateClassesFromStage(captured);
                lastCreateClassKey = null;
            });
        }
    }

    void RefreshTerrainPick()
    {
        string key = manager.terrainCatalog.Count + "|" + manager.includedTerrains.Count;
        for (int i = 0; i < manager.terrainCatalog.Count; i++)
        {
            if (manager.terrainCatalog[i] != null)
                key += manager.terrainCatalog[i].name;
        }
        if (key == lastTerrainPickKey)
            return;
        lastTerrainPickKey = key;
        ClearChildren(terrainPickContent);
        int shown = 0;
        for (int i = 0; i < manager.terrainCatalog.Count; i++)
        {
            GameObject terrain = manager.terrainCatalog[i];
            if (terrain == null || manager.includedTerrains.Contains(terrain))
                continue;
            shown++;
            GameObject captured = terrain;
            Button button = CreateFullButton(terrainPickContent, captured.name, 36f);
            button.onClick.AddListener(() =>
            {
                GameSfx.PlayUiClick();
                manager.includedTerrains.Add(captured);
                manager.captureSettingsDirty = true;
                manager.showTerrainDialog = false;
                lastTerrainKey = null;
                lastTerrainPickKey = null;
            });
        }
        if (shown == 0)
            AddMuted(terrainPickContent, "추가할 지형이 없습니다.");
    }

    void RefreshTrainResult()
    {
        DatasetManager.TrainResultData result = manager.trainResult;
        bool failed = result == null || result.status == "failed";
        bool stopped = result != null && result.status == "stopped";
        if (resultTitle != null)
            resultTitle.text = failed ? "학습 실패" : stopped ? "학습 중지" : "학습 완료";
        if (resultBody == null)
            return;

        if (result == null)
        {
            resultBody.text = "결과 파일을 읽지 못했습니다.";
            return;
        }

        if (failed)
        {
            resultBody.text = string.IsNullOrEmpty(result.message) ? "학습에 실패했습니다." : result.message;
            return;
        }

        System.Text.StringBuilder body = new System.Text.StringBuilder();
        body.AppendLine(string.IsNullOrEmpty(result.message) ? "학습이 완료되었습니다." : result.message);
        body.AppendLine();
        body.AppendLine("Precision: " + result.precision.ToString("0.###", CultureInfo.InvariantCulture));
        body.AppendLine("Recall: " + result.recall.ToString("0.###", CultureInfo.InvariantCulture));
        body.AppendLine("mAP50: " + result.map50.ToString("0.###", CultureInfo.InvariantCulture));
        body.AppendLine("mAP50-95: " + result.map50_95.ToString("0.###", CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(result.lastWeights))
            body.AppendLine("last.pt: " + Path.GetFileName(result.lastWeights));
        if (!string.IsNullOrEmpty(result.bestWeights))
            body.AppendLine("best.pt: " + Path.GetFileName(result.bestWeights));
        resultBody.text = body.ToString();
    }

    void RefreshTrainProgress()
    {
        DatasetManager.TrainResultData result = manager.trainResult;
        int epoch = result != null ? result.epoch : 0;
        int total = result != null && result.epochs > 0 ? result.epochs : Mathf.Max(1, manager.trainEpochs);
        float fill = Mathf.Clamp01((float)epoch / total);
        if (trainProgressFill != null)
            trainProgressFill.fillAmount = fill;

        if (trainProgressTitle != null)
            trainProgressTitle.text = manager.trainStopLevel > 0 ? "학습 중지 중" : "학습하는 중";

        if (trainProgressStatus != null)
        {
            string status = result != null && !string.IsNullOrEmpty(result.message)
                ? result.message
                : "학습하는 중";
            string train = result != null ? result.trainAcc.ToString("0.###", CultureInfo.InvariantCulture) : "-";
            string val = result != null ? result.valAcc.ToString("0.###", CultureInfo.InvariantCulture) : "-";
            trainProgressStatus.text = status
                + "  (" + epoch.ToString(CultureInfo.InvariantCulture)
                + "/" + total.ToString(CultureInfo.InvariantCulture) + ")"
                + "\n학습 정확도 " + train
                + "    검증 정확도 " + val;
        }

        if (trainStopLabel != null)
            trainStopLabel.text = manager.trainStopLevel < 1 ? "중지" : "강제 중지";

        DrawTrainGraph(result);
    }

    void DrawTrainGraph(DatasetManager.TrainResultData result)
    {
        if (trainGraphImage == null)
            return;

        int epoch = result != null ? result.epoch : 0;
        int trainCount = result != null && result.trainHistory != null ? result.trainHistory.Length : 0;
        int valCount = result != null && result.valHistory != null ? result.valHistory.Length : 0;
        int stamp = epoch * 1000 + trainCount * 10 + valCount;
        if (trainGraphTexture != null && stamp == lastTrainGraphEpoch)
            return;
        lastTrainGraphEpoch = stamp;

        const int width = 640;
        const int height = 240;
        if (trainGraphTexture == null)
        {
            trainGraphTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            trainGraphTexture.filterMode = FilterMode.Bilinear;
            trainGraphTexture.wrapMode = TextureWrapMode.Clamp;
            trainGraphImage.texture = trainGraphTexture;
        }

        Color bg = new Color(0.07f, 0.08f, 0.1f, 1f);
        Color grid = new Color(1f, 1f, 1f, 0.08f);
        Color axis = new Color(1f, 1f, 1f, 0.18f);
        Color trainColor = new Color(0.35f, 0.78f, 1f, 1f);
        Color valColor = new Color(1f, 0.72f, 0.28f, 1f);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = bg;

        for (int g = 1; g < 4; g++)
        {
            int y = Mathf.RoundToInt((height - 1) * (g / 4f));
            DrawGraphHLine(pixels, width, height, y, grid);
        }
        DrawGraphHLine(pixels, width, height, 0, axis);
        DrawGraphVLine(pixels, width, height, 0, axis);

        DrawGraphSeries(pixels, width, height, result != null ? result.trainHistory : null, trainColor);
        DrawGraphSeries(pixels, width, height, result != null ? result.valHistory : null, valColor);

        trainGraphTexture.SetPixels(pixels);
        trainGraphTexture.Apply(false);
        trainGraphImage.texture = trainGraphTexture;
    }

    static void DrawGraphSeries(Color[] pixels, int width, int height, float[] values, Color color)
    {
        if (values == null || values.Length == 0)
            return;

        int count = values.Length;
        for (int i = 0; i < count; i++)
        {
            float x = count == 1 ? 0f : i / (float)(count - 1);
            float y = Mathf.Clamp01(values[i]);
            int px = Mathf.RoundToInt(x * (width - 1));
            int py = Mathf.RoundToInt(y * (height - 1));
            PlotGraphDot(pixels, width, height, px, py, color);
            if (i == 0)
                continue;
            float px0 = (i - 1) / (float)Mathf.Max(1, count - 1);
            float py0 = Mathf.Clamp01(values[i - 1]);
            DrawGraphLine(
                pixels,
                width,
                height,
                Mathf.RoundToInt(px0 * (width - 1)),
                Mathf.RoundToInt(py0 * (height - 1)),
                px,
                py,
                color);
        }
    }

    static void DrawGraphLine(Color[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int x = x0;
        int y = y0;
        while (true)
        {
            PlotGraphDot(pixels, width, height, x, y, color);
            if (x == x1 && y == y1)
                break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    static void DrawGraphHLine(Color[] pixels, int width, int height, int y, Color color)
    {
        if (y < 0 || y >= height)
            return;
        int row = y * width;
        for (int x = 0; x < width; x++)
            pixels[row + x] = color;
    }

    static void DrawGraphVLine(Color[] pixels, int width, int height, int x, Color color)
    {
        if (x < 0 || x >= width)
            return;
        for (int y = 0; y < height; y++)
            pixels[y * width + x] = color;
    }

    static void PlotGraphDot(Color[] pixels, int width, int height, int x, int y, Color color)
    {
        for (int oy = -1; oy <= 1; oy++)
        {
            int py = y + oy;
            if (py < 0 || py >= height)
                continue;
            for (int ox = -1; ox <= 1; ox++)
            {
                int px = x + ox;
                if (px < 0 || px >= width)
                    continue;
                pixels[py * width + px] = color;
            }
        }
    }

    void DestroyOldCanvases()
    {
        Transform host = manager != null ? manager.transform : null;
        if (trainGraphTexture != null)
        {
            Object.Destroy(trainGraphTexture);
            trainGraphTexture = null;
        }
        lastTrainGraphEpoch = -1;
        if (host != null)
        {
            for (int i = host.childCount - 1; i >= 0; i--)
            {
                Transform child = host.GetChild(i);
                if (child != null && child.name == "DatasetCanvas")
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].name == "DatasetCanvas")
                Object.DestroyImmediate(canvases[i].gameObject);
        }
    }

    Canvas BuildCanvas(Transform parent)
    {
        GameObject canvasGo = new GameObject("DatasetCanvas");
        canvasGo.layer = 5;
        canvasGo.transform.SetParent(parent, false);
        Canvas built = canvasGo.AddComponent<Canvas>();
        built.renderMode = RenderMode.ScreenSpaceOverlay;
        built.sortingOrder = 100;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        return built;
    }

    void BuildHeader()
    {
        Image bar = UiFont.AddImage(canvas.transform, "FactoryHeader", panelColor);
        headerRoot = bar.gameObject;
        RectTransform rt = bar.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(12f, -94f);
        rt.offsetMax = new Vector2(-12f, -10f);

        Button back = CreateCompactButton(rt, "Back", "‹ 뒤로", 150f, 48f, () => manager.GoBack());
        RectTransform backRt = back.transform as RectTransform;
        backRt.anchorMin = backRt.anchorMax = new Vector2(0f, 0.5f);
        backRt.pivot = new Vector2(0f, 0.5f);
        backRt.anchoredPosition = new Vector2(18f, 0f);
        backRt.sizeDelta = new Vector2(150f, 48f);
        headerBackLabel = back.GetComponentInChildren<Text>();

        Text title = UiFont.AddText(rt, "Title", "데이터셋 팩토리", 32, TextAnchor.MiddleLeft, Color.white, UiType.Title);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0f, 0.5f);
        titleRt.pivot = new Vector2(0f, 0.5f);
        titleRt.anchoredPosition = new Vector2(186f, 0f);
        titleRt.sizeDelta = new Vector2(520f, 52f);

        headerStatus = UiFont.AddText(rt, "Status", "나만의 인식 모델 만들기", 20, TextAnchor.MiddleRight, mutedColor, UiType.Body);
        RectTransform statusRt = headerStatus.rectTransform;
        statusRt.anchorMin = statusRt.anchorMax = new Vector2(1f, 0.5f);
        statusRt.pivot = new Vector2(1f, 0.5f);
        statusRt.anchoredPosition = new Vector2(-24f, 0f);
        statusRt.sizeDelta = new Vector2(560f, 40f);
    }

    void BuildOverview(RectTransform parent)
    {
        GameObject panel = CreateFillPanel(parent);
        VerticalLayoutGroup column = panel.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(36, 36, 28, 28);
        column.spacing = 14f;
        column.childAlignment = TextAnchor.UpperLeft;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        RectTransform rt = panel.transform as RectTransform;

        Text heading = UiFont.AddText(rt, "Heading", "AI 학습 작업실", 36, TextAnchor.MiddleLeft, Color.white, UiType.Title);
        LayoutElement headingLayout = heading.gameObject.AddComponent<LayoutElement>();
        headingLayout.preferredHeight = 48f;
        Text lead = UiFont.AddText(rt, "Lead", "데이터셋을 선택하고 편집을 눌러 샘플을 만들거나 모델을 학습하세요.", 20, TextAnchor.MiddleLeft, mutedColor, UiType.Body);
        lead.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement leadLayout = lead.gameObject.AddComponent<LayoutElement>();
        leadLayout.preferredHeight = 36f;

        GameObject steps = new GameObject("Workflow");
        RectTransform stepsRt = steps.AddComponent<RectTransform>();
        stepsRt.SetParent(rt, false);
        HorizontalLayoutGroup stepsLayout = steps.AddComponent<HorizontalLayoutGroup>();
        stepsLayout.spacing = 16f;
        stepsLayout.childAlignment = TextAnchor.UpperLeft;
        stepsLayout.childControlWidth = true;
        stepsLayout.childControlHeight = true;
        stepsLayout.childForceExpandWidth = true;
        stepsLayout.childForceExpandHeight = true;
        LayoutElement stepsElement = steps.AddComponent<LayoutElement>();
        stepsElement.preferredHeight = 210f;
        stepsElement.minHeight = 210f;
        AddWorkflowStep(stepsRt, "01", "데이터셋 준비", "새 데이터셋을 만들거나\n왼쪽 목록에서 선택하세요.");
        AddWorkflowStep(stepsRt, "02", "샘플 생성", "대상·지형과 촬영 조건을 정해\n학습용 이미지를 생성하세요.");
        AddWorkflowStep(stepsRt, "03", "모델 학습", "학습과 검증 샘플을 확인하고\n게임에서 사용할 모델을 만드세요.");

        overviewSelected = UiFont.AddText(rt, "Selected", "선택한 데이터셋이 없습니다.", 24, TextAnchor.MiddleLeft, Color.white, UiType.Title);
        LayoutElement selectedLayout = overviewSelected.gameObject.AddComponent<LayoutElement>();
        selectedLayout.preferredHeight = 40f;
        overviewCount = UiFont.AddText(rt, "Count", "보유 데이터셋  0개", 18, TextAnchor.MiddleLeft, mutedColor, UiType.Body);
        LayoutElement countLayout = overviewCount.gameObject.AddComponent<LayoutElement>();
        countLayout.preferredHeight = 28f;

        GameObject actions = new GameObject("Actions");
        RectTransform actionsRt = actions.AddComponent<RectTransform>();
        actionsRt.SetParent(rt, false);
        HorizontalLayoutGroup actionsLayout = actions.AddComponent<HorizontalLayoutGroup>();
        actionsLayout.spacing = 12f;
        actionsLayout.childAlignment = TextAnchor.MiddleLeft;
        actionsLayout.childControlWidth = true;
        actionsLayout.childControlHeight = true;
        actionsLayout.childForceExpandWidth = false;
        actionsLayout.childForceExpandHeight = false;
        LayoutElement actionsElement = actions.AddComponent<LayoutElement>();
        actionsElement.preferredHeight = 56f;
        actionsElement.minHeight = 56f;
        PaintButton(CreateCompactButton(actionsRt, "New", "+ 새 데이터셋", 220f, 52f, () =>
        {
            lastCreateClassKey = null;
            lastCreateStageKey = null;
            manager.OpenNameDialog(false);
        }), UiSkin.Gold, UiSkin.Navy);
        overviewEdit = CreateCompactButton(actionsRt, "Edit", "선택한 데이터셋 편집  ›", 320f, 52f, () =>
        {
            if (!string.IsNullOrEmpty(manager.selectedDataset))
                manager.EnterEditMode(manager.selectedDataset);
        });
        PaintButton(overviewEdit, UiSkin.Green, Color.white);
    }

    static void AddWorkflowStep(Transform parent, string number, string title, string description)
    {
        Image card = UiFont.AddImage(parent, "Step_" + number, UiSkin.Navy);
        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        Text num = UiFont.AddText(card.rectTransform, "Number", number, 28, TextAnchor.MiddleLeft, Color.white, UiType.Title);
        LayoutElement numLayout = num.gameObject.AddComponent<LayoutElement>();
        numLayout.preferredHeight = 36f;
        Text heading = UiFont.AddText(card.rectTransform, "Title", title, 22, TextAnchor.MiddleLeft, Color.white, UiType.BodyBold);
        LayoutElement headingLayout = heading.gameObject.AddComponent<LayoutElement>();
        headingLayout.preferredHeight = 30f;
        Text body = UiFont.AddText(card.rectTransform, "Body", description, 16, TextAnchor.UpperLeft, UiSkin.Muted, UiType.Body);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement bodyLayout = body.gameObject.AddComponent<LayoutElement>();
        bodyLayout.preferredHeight = 72f;
    }

    void RefreshHeader()
    {
        if (headerBackLabel != null)
            headerBackLabel.text = manager.editMode ? "‹ 목록으로" : "‹ 뒤로";
        if (headerStatus == null)
            return;
        if (manager.IsGenerating)
            headerStatus.text = "샘플 생성 중…";
        else if (manager.waitingForTrain)
            headerStatus.text = "모델 학습 중…";
        else if (manager.editMode)
            headerStatus.text = "샘플 편집 · 생성 · 학습";
        else
            headerStatus.text = "나만의 인식 모델 만들기";
    }

    void RefreshOverview()
    {
        if (overviewSelected != null)
            overviewSelected.text = string.IsNullOrEmpty(manager.selectedDataset)
                ? "선택한 데이터셋이 없습니다."
                : "선택한 데이터셋  /  " + manager.selectedDataset;
        if (overviewCount != null)
            overviewCount.text = "보유 데이터셋  " + manager.datasetNames.Count + "개";
        if (overviewEdit != null)
            overviewEdit.interactable = !string.IsNullOrEmpty(manager.selectedDataset);
    }

    static void PaintButton(Button button, Color background, Color label)
    {
        if (button == null)
            return;
        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = background;
        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
            text.color = label;
    }

    void BuildListView()
    {
        listRoot = new GameObject("ListMode");
        RectTransform listRt = listRoot.AddComponent<RectTransform>();
        listRt.SetParent(canvas.transform, false);
        UiFont.Stretch(listRt);

        Image backdrop = UiFont.AddImage(listRt, "Backdrop", new Color(0.025f, 0.08f, 0.16f, 0.72f));
        backdrop.raycastTarget = false;
        UiFont.Stretch(backdrop.rectTransform);

        GameObject panel = CreateSidePanel("DatasetList", true, listRt);
        RectTransform rt = panel.transform as RectTransform;
        VerticalLayoutGroup column = panel.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(16, 16, 16, 16);
        column.spacing = 10f;
        column.childAlignment = TextAnchor.UpperLeft;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        HorizontalLayoutGroup header = CreateHeader(rt);
        LayoutElement headerLayout = header.GetComponent<LayoutElement>();
        headerLayout.preferredHeight = 36f;
        headerLayout.minHeight = 36f;

        Text title = UiFont.AddText(header.transform, "Title", "데이터셋", 24, TextAnchor.MiddleLeft, textColor, UiType.Title);
        title.raycastTarget = false;
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
        titleLayout.preferredHeight = 36f;
        titleLayout.minHeight = 36f;

        datasetListContent = CreateScroll("List", rt, 1f).content;
        PaintButton(CreateBarButton(rt, "Create", "+ 새 데이터셋", () =>
        {
            lastCreateClassKey = null;
            lastCreateStageKey = null;
            manager.OpenNameDialog(false);
        }), UiSkin.Gold, UiSkin.Navy);
        CreateBarButton(rt, "Refresh", "목록 새로고침", () =>
        {
            manager.RefreshDatasetList();
            lastDatasetKey = null;
        });
        BuildOverview(listRt);
    }

    void BuildEditView()
    {
        editRoot = new GameObject("EditView");
        RectTransform editRt = editRoot.AddComponent<RectTransform>();
        editRt.SetParent(canvas.transform, false);
        UiFont.Stretch(editRt);

        GameObject left = CreateSidePanel("SamplePanel", true, editRt);
        VerticalLayoutGroup leftCol = left.AddComponent<VerticalLayoutGroup>();
        leftCol.padding = new RectOffset(16, 16, 16, 16);
        leftCol.spacing = 8f;
        leftCol.childControlWidth = true;
        leftCol.childControlHeight = true;
        leftCol.childForceExpandWidth = true;
        leftCol.childForceExpandHeight = false;

        RectTransform leftRt = left.transform as RectTransform;
        HorizontalLayoutGroup header = CreateHeader(leftRt);
        editTitle = UiFont.AddText(header.transform, "Title", "샘플", 20, TextAnchor.MiddleLeft, textColor, UiType.Title);
        editTitle.raycastTarget = false;
        LayoutElement titleLayout = editTitle.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
        titleLayout.preferredHeight = 28f;
        titleLayout.minHeight = 28f;

        AddSectionLabel(leftRt, "샘플 리스트");
        HorizontalLayoutGroup sampleActions = CreateHeader(leftRt);
        sampleActions.spacing = 6f;
        sampleActions.childForceExpandWidth = true;
        LayoutElement sampleActionsLayout = sampleActions.GetComponent<LayoutElement>();
        sampleActionsLayout.preferredHeight = 28f;
        AddFlexActionButton(sampleActions.transform, "DeleteChecked", "선택 삭제", () =>
        {
            List<DatasetManager.DatasetSample> selected = new List<DatasetManager.DatasetSample>();
            for (int i = 0; i < manager.datasetSamples.Count; i++)
            {
                DatasetManager.DatasetSample sample = manager.datasetSamples[i];
                if (sample != null && checkedSampleIds.Contains(sample.id))
                    selected.Add(sample);
            }
            manager.DeleteSamples(selected);
            checkedSampleIds.Clear();
            lastSampleKey = null;
        });
        AddFlexActionButton(sampleActions.transform, "ClearChecked", "선택 취소", () =>
        {
            checkedSampleIds.Clear();
            lastSampleKey = null;
        });
        AddFlexActionButton(sampleActions.transform, "DeleteAll", "전체 삭제", () =>
        {
            manager.DeleteAllSamples();
            checkedSampleIds.Clear();
            lastSampleKey = null;
        });
        sampleListContent = CreateScroll("Samples", leftRt, 1f).content;

        generateButton = CreateBarButton(leftRt, "AutoGenerate", "자동 샘플 생성", () =>
        {
            lastTargetKey = null;
            lastTerrainKey = null;
            manager.OpenAutoGenerate();
        });
        generateHint = UiFont.AddText(leftRt, "GenerateHint", "", 15, TextAnchor.MiddleLeft, errorColor, UiType.Body);
        generateHint.raycastTarget = false;
        generateHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement genHintLayout = generateHint.gameObject.AddComponent<LayoutElement>();
        genHintLayout.preferredHeight = 22f;

        manualButton = CreateBarButton(leftRt, "ManualCapture", "수동 샘플 촬영", () =>
        {
            lastManualClassKey = null;
            lastManualTerrainKey = null;
            manager.OpenManualCapture();
        });
        manualHint = UiFont.AddText(leftRt, "ManualHint", "", 15, TextAnchor.MiddleLeft, errorColor, UiType.Body);
        manualHint.raycastTarget = false;
        manualHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement manualHintLayout = manualHint.gameObject.AddComponent<LayoutElement>();
        manualHintLayout.preferredHeight = 22f;

        trainButton = CreateBarButton(leftRt, "Train", "피아식별 모델 학습", () =>
        {
            manager.RefreshSampleList();
            manager.SaveCaptureSettings();
            manager.trainDialogError = "";
            manager.showTrainDialog = true;
            manager.showGenerateDialog = false;
            manager.showManualCaptureDialog = false;
            manager.showPrefabDialog = false;
            manager.showTerrainDialog = false;
        });
        trainHint = UiFont.AddText(leftRt, "TrainHint", "", 15, TextAnchor.MiddleLeft, errorColor, UiType.Body);
        trainHint.raycastTarget = false;
        trainHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement trainHintLayout = trainHint.gameObject.AddComponent<LayoutElement>();
        trainHintLayout.preferredHeight = 22f;

        GameObject right = CreateFillPanel(editRt);
        VerticalLayoutGroup rightCol = right.AddComponent<VerticalLayoutGroup>();
        rightCol.padding = new RectOffset(16, 16, 16, 16);
        rightCol.spacing = 8f;
        rightCol.childControlWidth = true;
        rightCol.childControlHeight = true;
        rightCol.childForceExpandWidth = true;
        rightCol.childForceExpandHeight = false;
        RectTransform rightRt = right.transform as RectTransform;
        AddTitle(rightRt, "미리보기");
        Text bboxHelp = UiFont.AddText(
            rightRt,
            "BboxHelp",
            "마우스로 표적의 박스 모서리를 잡고 드래그하여 조절할 수 있습니다.",
            15,
            TextAnchor.MiddleLeft,
            mutedColor,
            UiType.Body);
        bboxHelp.raycastTarget = false;
        bboxHelp.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement bboxHelpLayout = bboxHelp.gameObject.AddComponent<LayoutElement>();
        bboxHelpLayout.preferredHeight = 36f;

        Image previewFrame = UiFont.AddImage(rightRt, "Preview", insetColor);
        LayoutElement previewLayout = previewFrame.gameObject.AddComponent<LayoutElement>();
        previewLayout.flexibleHeight = 1f;
        previewLayout.minHeight = 280f;
        previewFrame.gameObject.AddComponent<RectMask2D>();

        previewImage = new GameObject("Image").AddComponent<RawImage>();
        previewImage.raycastTarget = false;
        RectTransform imageRt = previewImage.rectTransform;
        imageRt.SetParent(previewFrame.rectTransform, false);
        UiFont.Stretch(imageRt);
        imageRt.offsetMin = new Vector2(12f, 12f);
        imageRt.offsetMax = new Vector2(-12f, -12f);
        AspectRatioFitter fitter = previewImage.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1f;
        previewBoxRoot = imageRt;

        previewHint = UiFont.AddText(
            previewFrame.rectTransform,
            "Hint",
            "샘플을 클릭하면 미리보기가 표시됩니다.",
            20,
            TextAnchor.MiddleCenter,
            mutedColor,
            UiType.Body);
        previewHint.raycastTarget = false;
        UiFont.Stretch(previewHint.rectTransform);
    }

    void BuildDialogs()
    {
        Image dim = UiFont.AddImage(canvas.transform, "Dialogs", new Color(0f, 0.02f, 0.07f, 0.65f));
        UiFont.Stretch(dim.rectTransform);
        dialogRoot = dim.gameObject;
        Canvas overlay = dialogRoot.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 250;
        dialogRoot.AddComponent<GraphicRaycaster>();

        nameDialog = CreateCard(dim.rectTransform, "NameDialog", new Vector2(520f, 680f));
        nameDialogRt = nameDialog.transform as RectTransform;
        nameDialogTitle = AddCardTitle(nameDialog.transform, "새 데이터셋");
        nameField = CreateDialogField(nameDialog.transform, "이름", false);
        nameField.onValueChanged.AddListener(value => manager.nameDialogValue = value);

        createExtras = new GameObject("CreateExtras");
        RectTransform extrasRt = createExtras.AddComponent<RectTransform>();
        extrasRt.SetParent(nameDialog.transform, false);
        VerticalLayoutGroup extrasLayout = createExtras.AddComponent<VerticalLayoutGroup>();
        extrasLayout.spacing = 8f;
        extrasLayout.childAlignment = TextAnchor.UpperLeft;
        extrasLayout.childControlWidth = true;
        extrasLayout.childControlHeight = true;
        extrasLayout.childForceExpandWidth = true;
        extrasLayout.childForceExpandHeight = false;
        LayoutElement extrasElement = createExtras.AddComponent<LayoutElement>();
        extrasElement.flexibleHeight = 1f;
        extrasElement.minHeight = 320f;
        extrasElement.preferredHeight = 380f;

        AddSectionLabel(extrasRt, "클래스 (대상)");
        Text classHelp = UiFont.AddText(
            extrasRt,
            "ClassHelp",
            "추가한 순서대로 클래스 인덱스가 정해지며, 생성 후에는 바꿀 수 없습니다.",
            14,
            TextAnchor.MiddleLeft,
            mutedColor,
            UiType.Body);
        classHelp.raycastTarget = false;
        classHelp.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement classHelpLayout = classHelp.gameObject.AddComponent<LayoutElement>();
        classHelpLayout.preferredHeight = 36f;
        createClassListContent = CreateScroll("CreateClasses", extrasRt, 0f, 140f).content;
        CreateBarButton(extrasRt, "AddClass", "대상 추가", () =>
        {
            manager.OpenPrefabPick(true);
            lastPrefabPickKey = null;
        });

        AddSectionLabel(extrasRt, "게임 스테이지");
        Text stageHelp = UiFont.AddText(
            extrasRt,
            "StageHelp",
            "스테이지를 누르면 그 스테이지에 등장하는 대상이 클래스 목록에 추가됩니다.",
            14,
            TextAnchor.MiddleLeft,
            mutedColor,
            UiType.Body);
        stageHelp.raycastTarget = false;
        stageHelp.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement stageHelpLayout = stageHelp.gameObject.AddComponent<LayoutElement>();
        stageHelpLayout.preferredHeight = 36f;
        createStageListContent = CreateScroll("CreateStages", extrasRt, 0f, 140f).content;

        nameDialogError = AddCardError(nameDialog.transform);
        GameObject nameButtons = new GameObject("Buttons");
        RectTransform nameButtonsRt = nameButtons.AddComponent<RectTransform>();
        nameButtonsRt.SetParent(nameDialog.transform, false);
        HorizontalLayoutGroup nameButtonsLayout = nameButtons.AddComponent<HorizontalLayoutGroup>();
        nameButtonsLayout.spacing = 8f;
        nameButtonsLayout.childAlignment = TextAnchor.MiddleRight;
        nameButtonsLayout.childControlWidth = true;
        nameButtonsLayout.childControlHeight = true;
        nameButtonsLayout.childForceExpandWidth = false;
        nameButtonsLayout.childForceExpandHeight = false;
        LayoutElement nameButtonsElement = nameButtons.AddComponent<LayoutElement>();
        nameButtonsElement.preferredHeight = 32f;
        nameButtonsElement.minHeight = 32f;
        Button nameConfirm = CreateCompactButton(nameButtonsRt, "Confirm", "생성", 128f, 32f, () =>
        {
            manager.ConfirmNameDialog();
            lastDatasetKey = null;
            lastCreateClassKey = null;
        });
        nameConfirmLabel = nameConfirm.GetComponentInChildren<Text>();
        CreateCompactButton(nameButtonsRt, "Cancel", "취소", 88f, 32f, () =>
        {
            manager.showNameDialog = false;
            manager.showPrefabDialog = false;
            lastCreateClassKey = null;
        });

        prefabDialog = CreateCard(dim.rectTransform, "PrefabDialog", new Vector2(480f, 520f));
        AddCardTitle(prefabDialog.transform, "대상 프리팹 선택");
        prefabPickContent = CreateScroll("PrefabPick", prefabDialog.transform, 1f).content;
        AddCardButtons(prefabDialog.transform, null, null, () => manager.showPrefabDialog = false);

        terrainDialog = CreateCard(dim.rectTransform, "TerrainDialog", new Vector2(480f, 460f));
        AddCardTitle(terrainDialog.transform, "지형 선택");
        terrainPickContent = CreateScroll("TerrainPick", terrainDialog.transform, 1f).content;
        AddCardButtons(terrainDialog.transform, null, null, () => manager.showTerrainDialog = false);

        generateDialog = CreateCard(dim.rectTransform, "GenerateDialog", new Vector2(540f, 760f));
        AddCardTitle(generateDialog.transform, "자동 샘플 생성");
        AddSectionLabel(generateDialog.transform, "대상");
        targetListContent = CreateScroll("Targets", generateDialog.transform, 0f, 120f).content;
        CreateBarButton(generateDialog.transform, "AddTarget", "대상 추가", () =>
        {
            manager.OpenPrefabPick(false);
            lastPrefabPickKey = null;
            if (prefabDialog != null)
                prefabDialog.transform.SetAsLastSibling();
        });
        AddSectionLabel(generateDialog.transform, "지형");
        terrainListContent = CreateScroll("Terrains", generateDialog.transform, 0f, 100f).content;
        CreateBarButton(generateDialog.transform, "AddTerrain", "지형 추가", () =>
        {
            manager.RefreshTerrainCatalog();
            manager.showTerrainDialog = true;
            manager.showPrefabDialog = false;
            lastTerrainPickKey = null;
            if (terrainDialog != null)
                terrainDialog.transform.SetAsLastSibling();
        });
        minCountField = CreateIntRow(generateDialog.transform, "최소 대상 수", () =>
        {
            CommitInt(minCountField, ref manager.minCaptureCount, 1, 32);
            if (manager.maxCaptureCount < manager.minCaptureCount)
                manager.maxCaptureCount = manager.minCaptureCount;
            manager.captureSettingsDirty = true;
            manager.RefreshAutoCaptureFraming();
        });
        maxCountField = CreateIntRow(generateDialog.transform, "최대 대상 수", () =>
        {
            CommitInt(maxCountField, ref manager.maxCaptureCount, 1, 32);
            if (manager.maxCaptureCount < manager.minCaptureCount)
                manager.minCaptureCount = manager.maxCaptureCount;
            manager.captureSettingsDirty = true;
            manager.RefreshAutoCaptureFraming();
        });
        jpegField = CreateIntRow(generateDialog.transform, "JPEG 품질", () =>
        {
            CommitInt(jpegField, ref manager.jpegQuality, 1, 100);
            manager.captureSettingsDirty = true;
        });
        framingHint = UiFont.AddText(generateDialog.transform, "Framing", "", 15, TextAnchor.UpperLeft, mutedColor, UiType.Body);
        framingHint.raycastTarget = false;
        framingHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        framingHint.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement framingLayout = framingHint.gameObject.AddComponent<LayoutElement>();
        framingLayout.preferredHeight = 40f;
        trainCountField = CreateIntRow(generateDialog.transform, "학습(train)", () =>
            CommitInt(trainCountField, ref manager.pendingTrainCount, 0, 100000));
        valCountField = CreateIntRow(generateDialog.transform, "검증(val)", () =>
            CommitInt(valCountField, ref manager.pendingValCount, 0, 100000));
        testCountField = CreateIntRow(generateDialog.transform, "테스트(test)", () =>
            CommitInt(testCountField, ref manager.pendingTestCount, 0, 100000));
        generateError = AddCardError(generateDialog.transform);
        GameObject generateButtons = new GameObject("Buttons");
        RectTransform generateButtonsRt = generateButtons.AddComponent<RectTransform>();
        generateButtonsRt.SetParent(generateDialog.transform, false);
        HorizontalLayoutGroup generateButtonsLayout = generateButtons.AddComponent<HorizontalLayoutGroup>();
        generateButtonsLayout.spacing = 8f;
        generateButtonsLayout.childAlignment = TextAnchor.MiddleRight;
        generateButtonsLayout.childControlWidth = true;
        generateButtonsLayout.childControlHeight = true;
        generateButtonsLayout.childForceExpandWidth = false;
        generateButtonsLayout.childForceExpandHeight = false;
        LayoutElement generateButtonsElement = generateButtons.AddComponent<LayoutElement>();
        generateButtonsElement.preferredHeight = 32f;
        generateButtonsElement.minHeight = 32f;
        CreateCompactButton(generateButtonsRt, "Confirm", "최종 생성하기", 148f, 32f, () => manager.ConfirmGenerateCounts());
        CreateCompactButton(generateButtonsRt, "Back", "뒤로", 88f, 32f, () => manager.showGenerateDialog = false);

        generateProgressDialog = CreateCard(dim.rectTransform, "GenerateProgress", new Vector2(720f, 280f));
        AddCardTitle(generateProgressDialog.transform, "샘플을 생성하는 중");
        generateProgressStatus = UiFont.AddText(
            generateProgressDialog.transform,
            "Status",
            "장면을 준비하고 있습니다…",
            18,
            TextAnchor.MiddleLeft,
            mutedColor,
            UiType.Body);
        generateProgressStatus.raycastTarget = false;
        generateProgressStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement generateStatusLayout = generateProgressStatus.gameObject.AddComponent<LayoutElement>();
        generateStatusLayout.preferredHeight = 32f;
        generateStatusLayout.minHeight = 32f;

        GameObject generateMeta = new GameObject("Meta");
        RectTransform generateMetaRt = generateMeta.AddComponent<RectTransform>();
        generateMetaRt.SetParent(generateProgressDialog.transform, false);
        HorizontalLayoutGroup generateMetaLayout = generateMeta.AddComponent<HorizontalLayoutGroup>();
        generateMetaLayout.childAlignment = TextAnchor.MiddleLeft;
        generateMetaLayout.childControlWidth = true;
        generateMetaLayout.childControlHeight = true;
        generateMetaLayout.childForceExpandWidth = true;
        generateMetaLayout.childForceExpandHeight = false;
        LayoutElement generateMetaElement = generateMeta.AddComponent<LayoutElement>();
        generateMetaElement.preferredHeight = 28f;
        generateProgressCount = UiFont.AddText(generateMetaRt, "Count", "전체  0 / 0", 20, TextAnchor.MiddleLeft, Color.white, UiType.BodyBold);
        LayoutElement countLayout = generateProgressCount.gameObject.AddComponent<LayoutElement>();
        countLayout.flexibleWidth = 1f;
        generateProgressPercent = UiFont.AddText(generateMetaRt, "Percent", "0%", 22, TextAnchor.MiddleRight, UiSkin.Gold, UiType.Title);
        LayoutElement percentLayout = generateProgressPercent.gameObject.AddComponent<LayoutElement>();
        percentLayout.preferredWidth = 90f;

        Image generateBar = UiFont.AddImage(generateProgressDialog.transform, "Bar", insetColor);
        LayoutElement generateBarLayout = generateBar.gameObject.AddComponent<LayoutElement>();
        generateBarLayout.preferredHeight = 28f;
        generateBarLayout.minHeight = 28f;
        generateProgressFill = UiFont.AddImage(generateBar.rectTransform, "Fill", UiSkin.Sky);
        generateProgressFill.type = Image.Type.Filled;
        generateProgressFill.fillMethod = Image.FillMethod.Horizontal;
        generateProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        generateProgressFill.fillAmount = 0f;
        UiFont.Stretch(generateProgressFill.rectTransform);
        Text waitHint = UiFont.AddText(
            generateProgressDialog.transform,
            "Hint",
            "잠시만 기다려 주세요. 생성이 끝나면 샘플 목록이 갱신됩니다.",
            16,
            TextAnchor.MiddleLeft,
            mutedColor,
            UiType.Body);
        waitHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement waitHintLayout = waitHint.gameObject.AddComponent<LayoutElement>();
        waitHintLayout.preferredHeight = 36f;

        trainDialog = CreateCard(dim.rectTransform, "TrainDialog", new Vector2(480f, 360f));
        AddCardTitle(trainDialog.transform, "모델 학습");
        trainHelp = UiFont.AddText(trainDialog.transform, "Help", "", 16, TextAnchor.UpperLeft, mutedColor, UiType.Body);
        trainHelp.raycastTarget = false;
        trainHelp.horizontalOverflow = HorizontalWrapMode.Wrap;
        trainHelp.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement helpLayout = trainHelp.gameObject.AddComponent<LayoutElement>();
        helpLayout.preferredHeight = 48f;
        epochField = CreateIntRow(trainDialog.transform, "에포크", () =>
            CommitInt(epochField, ref manager.trainEpochs, 1, 1000));
        batchField = CreateIntRow(trainDialog.transform, "배치 사이즈", () =>
            CommitInt(batchField, ref manager.trainBatchSize, 1, 256));
        trainError = AddCardError(trainDialog.transform);
        AddCardButtons(trainDialog.transform, "학습 시작", () => manager.ConfirmStartTraining(), () => manager.showTrainDialog = false);

        trainProgressDialog = CreateCard(dim.rectTransform, "TrainProgress", new Vector2(720f, 560f));
        trainProgressTitle = AddCardTitle(trainProgressDialog.transform, "학습하는 중");
        trainProgressStatus = UiFont.AddText(
            trainProgressDialog.transform,
            "Status",
            "학습하는 중",
            16,
            TextAnchor.MiddleLeft,
            mutedColor,
            UiType.Body);
        trainProgressStatus.raycastTarget = false;
        trainProgressStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
        trainProgressStatus.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement statusLayout = trainProgressStatus.gameObject.AddComponent<LayoutElement>();
        statusLayout.preferredHeight = 48f;
        statusLayout.minHeight = 40f;

        Image graphFrame = UiFont.AddImage(trainProgressDialog.transform, "Graph", insetColor);
        LayoutElement graphLayout = graphFrame.gameObject.AddComponent<LayoutElement>();
        graphLayout.preferredHeight = 260f;
        graphLayout.minHeight = 220f;
        graphLayout.flexibleHeight = 1f;
        trainGraphImage = new GameObject("Plot").AddComponent<RawImage>();
        trainGraphImage.color = Color.white;
        trainGraphImage.raycastTarget = false;
        RectTransform plotRt = trainGraphImage.rectTransform;
        plotRt.SetParent(graphFrame.rectTransform, false);
        UiFont.Stretch(plotRt);
        plotRt.offsetMin = new Vector2(8f, 8f);
        plotRt.offsetMax = new Vector2(-8f, -8f);

        Image bar = UiFont.AddImage(trainProgressDialog.transform, "Bar", insetColor);
        LayoutElement barLayout = bar.gameObject.AddComponent<LayoutElement>();
        barLayout.preferredHeight = 22f;
        barLayout.minHeight = 22f;
        trainProgressFill = UiFont.AddImage(bar.rectTransform, "Fill", UiSkin.Sky);
        trainProgressFill.type = Image.Type.Filled;
        trainProgressFill.fillMethod = Image.FillMethod.Horizontal;
        trainProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        trainProgressFill.fillAmount = 0f;
        UiFont.Stretch(trainProgressFill.rectTransform);

        trainStopLabel = CreateBarButton(trainProgressDialog.transform, "Stop", "중지", () => manager.RequestTrainStop())
            .GetComponentInChildren<Text>();

        trainResultDialog = CreateCard(dim.rectTransform, "TrainResult", new Vector2(520f, 420f));
        resultTitle = AddCardTitle(trainResultDialog.transform, "학습 완료");
        resultBody = UiFont.AddText(trainResultDialog.transform, "Body", "", 18, TextAnchor.UpperLeft, textColor, UiType.Body);
        resultBody.raycastTarget = false;
        resultBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        resultBody.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement resultLayout = resultBody.gameObject.AddComponent<LayoutElement>();
        resultLayout.flexibleHeight = 1f;
        AddCardButtons(trainResultDialog.transform, "닫기", () =>
        {
            manager.showTrainResult = false;
            manager.trainResult = null;
        }, null);

        SetActive(nameDialog, false);
        SetActive(prefabDialog, false);
        SetActive(terrainDialog, false);
        SetActive(generateDialog, false);
        SetActive(generateProgressDialog, false);
        SetActive(trainDialog, false);
        SetActive(trainProgressDialog, false);
        SetActive(trainResultDialog, false);
        SetActive(dialogRoot, false);
        SetActive(editRoot, false);
        SetActive(listRoot, true);
    }

    void BuildManualHud()
    {
        manualRoot = new GameObject("ManualMode");
        RectTransform rootRt = manualRoot.AddComponent<RectTransform>();
        rootRt.SetParent(canvas.transform, false);
        UiFont.Stretch(rootRt);

        Image finder = UiFont.AddImage(rootRt, "Viewfinder", Color.clear);
        finder.raycastTarget = false;
        viewfinderRt = finder.rectTransform;
        viewfinderRt.anchorMin = new Vector2(0.2f, 0.16f);
        viewfinderRt.anchorMax = new Vector2(0.8f, 0.84f);
        viewfinderRt.offsetMin = Vector2.zero;
        viewfinderRt.offsetMax = Vector2.zero;
        AddOutlineEdge(viewfinderRt, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 3f));
        AddOutlineEdge(viewfinderRt, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f));
        AddOutlineEdge(viewfinderRt, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(3f, 0f));
        AddOutlineEdge(viewfinderRt, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(3f, 0f));
        Image[] edges = viewfinderRt.GetComponentsInChildren<Image>();
        for (int i = 1; i < edges.Length; i++)
            edges[i].color = new Color(1f, 1f, 1f, 0.92f);
        Text finderLabel = UiFont.AddText(viewfinderRt, "Label", "촬영 범위", 16, TextAnchor.UpperCenter, Color.white, UiType.BodyMedium);
        finderLabel.raycastTarget = false;
        RectTransform finderLabelRt = finderLabel.rectTransform;
        finderLabelRt.anchorMin = new Vector2(0f, 1f);
        finderLabelRt.anchorMax = new Vector2(1f, 1f);
        finderLabelRt.pivot = new Vector2(0.5f, 0f);
        finderLabelRt.anchoredPosition = new Vector2(0f, 6f);
        finderLabelRt.sizeDelta = new Vector2(0f, 22f);

        manualToggle = CreatePanel(
            "ManualToggle",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -24f),
            new Vector2(420f, 48f));
        manualToggle.transform.SetParent(rootRt, false);
        manualToggleText = UiFont.AddText(
            manualToggle.transform,
            "Label",
            "Tab  촬영 설정 열기",
            20,
            TextAnchor.MiddleCenter,
            textColor,
            UiType.BodyMedium);
        manualToggleText.raycastTarget = false;
        UiFont.Stretch(manualToggleText.rectTransform);
        AddPointerDown(manualToggle, eventData =>
        {
            if (!IsLeftPointer(eventData))
                return;
            GameSfx.PlayUiClick();
            manager.ToggleManualSettingsUi();
        });

        Image panel = UiFont.AddImage(rootRt, "ManualPanel", panelColor);
        manualPanel = panel.gameObject;
        RectTransform panelRt = panel.rectTransform;
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.offsetMin = new Vector2(24f, 24f);
        panelRt.offsetMax = new Vector2(444f, -84f);
        VerticalLayoutGroup column = manualPanel.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(14, 14, 14, 14);
        column.spacing = 8f;
        column.childAlignment = TextAnchor.UpperLeft;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        AddTitle(panelRt, "수동 촬영 설정");
        AddSectionLabel(panelRt, "대상을 끌어 배치 · 휠로 회전");
        manualClassRow = CreateManualList(panelRt, "Classes", 1.2f);
        AddSectionLabel(panelRt, "지형");
        manualTerrainRow = CreateManualList(panelRt, "Terrains", 0.7f);
        AddSectionLabel(panelRt, "저장 위치");
        manualSplitRow = CreateChipHost(panelRt, "Splits");
        CreateBarButton(panelRt, "Fog", "무작위 안개 추가", () => manager.AddRandomManualFog());
        CreateBarButton(panelRt, "FogReset", "안개 초기화", () => manager.ResetManualFog());
        sunSliderLabel = AddSectionLabel(panelRt, "태양 회전");
        sunSlider = CreateSunSlider(panelRt);
        CreateBarButton(panelRt, "Undo", "마지막 대상 삭제", () => manager.UndoLastManualTarget());
        CreateBarButton(panelRt, "Shoot", "촬영", () =>
        {
            manager.CaptureManualSample();
            lastSampleKey = null;
        });
        CreateBarButton(panelRt, "Back", "뒤로", () => manager.CloseManualCapture());
        manualStatus = UiFont.AddText(panelRt, "Status", "", 14, TextAnchor.UpperLeft, mutedColor, UiType.Body);
        manualStatus.raycastTarget = false;
        manualStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
        manualStatus.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement statusLayout = manualStatus.gameObject.AddComponent<LayoutElement>();
        statusLayout.preferredHeight = 48f;
        manualError = UiFont.AddText(panelRt, "Error", "", 14, TextAnchor.MiddleLeft, errorColor, UiType.Body);
        manualError.raycastTarget = false;
        manualError.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement errorLayout = manualError.gameObject.AddComponent<LayoutElement>();
        errorLayout.preferredHeight = 28f;

        BuildManualReview(rootRt);
        SetActive(manualRoot, false);
    }

    void BuildManualReview(RectTransform rootRt)
    {
        Image card = UiFont.AddImage(rootRt, "ShotReview", panelColor);
        manualReviewRoot = card.gameObject;
        RectTransform cardRt = card.rectTransform;
        cardRt.anchorMin = new Vector2(1f, 0f);
        cardRt.anchorMax = new Vector2(1f, 1f);
        cardRt.pivot = new Vector2(1f, 1f);
        cardRt.offsetMin = new Vector2(-720f, 24f);
        cardRt.offsetMax = new Vector2(-24f, -24f);
        VerticalLayoutGroup column = manualReviewRoot.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(16, 16, 16, 16);
        column.spacing = 8f;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        AddTitle(cardRt, "촬영 미리보기");
        Text help = UiFont.AddText(
            cardRt,
            "Help",
            "마우스로 표적의 박스 모서리를 잡고 드래그하여 조절할 수 있습니다.",
            15,
            TextAnchor.MiddleLeft,
            mutedColor,
            UiType.Body);
        help.raycastTarget = false;
        help.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement helpLayout = help.gameObject.AddComponent<LayoutElement>();
        helpLayout.preferredHeight = 36f;

        Image frame = UiFont.AddImage(cardRt, "Frame", insetColor);
        LayoutElement frameLayout = frame.gameObject.AddComponent<LayoutElement>();
        frameLayout.flexibleHeight = 1f;
        frameLayout.minHeight = 280f;
        frame.gameObject.AddComponent<RectMask2D>();

        reviewImage = new GameObject("Image").AddComponent<RawImage>();
        reviewImage.raycastTarget = false;
        RectTransform imageRt = reviewImage.rectTransform;
        imageRt.SetParent(frame.rectTransform, false);
        UiFont.Stretch(imageRt);
        imageRt.offsetMin = new Vector2(12f, 12f);
        imageRt.offsetMax = new Vector2(-12f, -12f);
        AspectRatioFitter fitter = reviewImage.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1f;
        reviewBoxRoot = imageRt;

        HorizontalLayoutGroup buttons = CreateHeader(cardRt);
        buttons.childForceExpandWidth = true;
        AddFlexActionButton(buttons.transform, "DeleteShot", "삭제", () =>
        {
            manager.DiscardManualShot();
            lastManualReviewVersion = -1;
        });
        AddFlexActionButton(buttons.transform, "SaveShot", "저장", () =>
        {
            manager.CommitManualShot();
            lastSampleKey = null;
            lastManualReviewVersion = -1;
        });
        SetActive(manualReviewRoot, false);
    }

    Transform CreateChipHost(Transform parent, string name)
    {
        GameObject host = new GameObject(name);
        RectTransform rt = host.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        HorizontalLayoutGroup layout = host.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        LayoutElement element = host.AddComponent<LayoutElement>();
        element.minHeight = 32f;
        element.preferredHeight = 32f;
        return rt;
    }

    Transform CreateManualList(Transform parent, string name, float flex)
    {
        Image frame = UiFont.AddImage(parent, name, insetColor);
        LayoutElement layout = frame.gameObject.AddComponent<LayoutElement>();
        layout.flexibleHeight = flex;
        layout.minHeight = 160f;
        layout.preferredHeight = 180f;

        GameObject viewport = new GameObject("Viewport");
        RectTransform viewportRt = viewport.AddComponent<RectTransform>();
        viewportRt.SetParent(frame.rectTransform, false);
        UiFont.Stretch(viewportRt);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;
        viewport.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content");
        RectTransform contentRt = content.AddComponent<RectTransform>();
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;
        VerticalLayoutGroup list = content.AddComponent<VerticalLayoutGroup>();
        list.padding = new RectOffset(8, 8, 8, 8);
        list.spacing = 6f;
        list.childAlignment = TextAnchor.UpperCenter;
        list.childControlWidth = true;
        list.childControlHeight = false;
        list.childForceExpandWidth = true;
        list.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scroll = frame.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;
        scroll.viewport = viewportRt;
        scroll.content = contentRt;
        return contentRt;
    }

    static void ClearManualList(Transform parent)
    {
        if (parent == null)
            return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    Slider CreateSunSlider(Transform parent)
    {
        Image track = UiFont.AddImage(parent, "SunSlider", insetColor);
        LayoutElement trackLayout = track.gameObject.AddComponent<LayoutElement>();
        trackLayout.preferredHeight = 22f;
        trackLayout.minHeight = 22f;
        Slider slider = track.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 360f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        Image handle = UiFont.AddImage(track.rectTransform, "Handle", Color.white);
        RectTransform handleRt = handle.rectTransform;
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(16f, 0f);
        slider.handleRect = handleRt;
        slider.targetGraphic = handle;
        slider.onValueChanged.AddListener(value => manager.SetManualSunYaw(value));
        return slider;
    }

    void RefreshManualHud()
    {
        bool open = manager.showManualSettingsUi || manager.showManualShotReview;
        SetActive(manualPanel, manager.showManualSettingsUi);
        SetActive(manualReviewRoot, manager.showManualShotReview);
        if (viewfinderRt != null)
            SetActive(viewfinderRt.gameObject, !manager.showManualShotReview);
        if (manualToggleText != null)
            manualToggleText.text = manager.showManualSettingsUi ? "촬영 모드  Tab 닫기" : "Tab  촬영 설정 열기";
        if (open != lastManualUiOpen)
        {
            lastManualUiOpen = open;
            lastManualClassKey = null;
            lastManualTerrainKey = null;
        }

        Vector2 min;
        Vector2 max;
        if (manager.TryGetManualViewfinder(out min, out max) && viewfinderRt != null)
        {
            viewfinderRt.anchorMin = min;
            viewfinderRt.anchorMax = max;
            viewfinderRt.offsetMin = Vector2.zero;
            viewfinderRt.offsetMax = Vector2.zero;
        }

        SetText(manualStatus, manager.manualCaptureStatus ?? "");
        SetText(manualError, manager.manualCaptureError ?? "");
        if (sunSliderLabel != null)
            sunSliderLabel.text = "태양 회전  " + manager.manualSunYaw.ToString("0", CultureInfo.InvariantCulture);
        if (sunSlider != null && !IsLeftMouseHeld())
            sunSlider.value = manager.manualSunYaw;

        string[] splits = { "train", "val", "test" };
        if (manualSplitRow != null && manualSplitRow.childCount != splits.Length)
        {
            ClearChildren(manualSplitRow);
            for (int i = 0; i < splits.Length; i++)
            {
                string split = splits[i];
                Button button = CreateFullButton(manualSplitRow, split, 28f);
                button.onClick.AddListener(() =>
                {
                    GameSfx.PlayUiClick();
                    manager.manualSplit = split;
                });
            }
        }

        if (manualSplitRow != null)
        {
            for (int i = 0; i < manualSplitRow.childCount && i < splits.Length; i++)
            {
                Image image = manualSplitRow.GetChild(i).GetComponent<Image>();
                if (image != null)
                    image.color = manager.manualSplit == splits[i] ? selectedColor : rowColor;
            }
        }

        if (manager.showManualShotReview)
            RefreshManualReview();

        List<Target_Base> pool = manager.GetManualClassPool();
        string classKey = pool.Count.ToString();
        for (int i = 0; i < pool.Count; i++)
            classKey += "|" + (pool[i] != null ? pool[i].HudLabel : "");
        if (classKey != lastManualClassKey || (manualClassRow != null && manualClassRow.childCount == 0))
        {
            lastManualClassKey = classKey;
            ClearManualList(manualClassRow);
            if (pool.Count == 0)
                AddMuted(manualClassRow, "클래스가 없습니다.");
            for (int i = 0; i < pool.Count; i++)
            {
                Target_Base target = pool[i];
                if (target == null)
                    continue;
                int index = i;
                Image row = CreateRow(manualClassRow, target.HudLabel, rowColor, 40f);
                Text label = UiFont.AddText(
                    row.rectTransform,
                    "Label",
                    index.ToString(CultureInfo.InvariantCulture) + "  " + target.HudLabel,
                    16,
                    TextAnchor.MiddleLeft,
                    textColor,
                    UiType.Body);
                label.raycastTarget = false;
                RectTransform labelRt = label.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(10f, 0f);
                labelRt.offsetMax = new Vector2(-8f, 0f);
                AddPointerDown(row.gameObject, eventData =>
                {
                    if (!IsLeftPointer(eventData))
                        return;
                    GameSfx.PlayUiClick();
                    manager.BeginManualGhost(index);
                });
            }
            if (manualClassRow is RectTransform classRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(classRt);
        }

        if (manualClassRow != null)
        {
            for (int i = 0; i < manualClassRow.childCount; i++)
            {
                Image image = manualClassRow.GetChild(i).GetComponent<Image>();
                if (image != null)
                    image.color = i == manager.manualPlaceClassIndex ? selectedColor : rowColor;
            }
        }

        List<GameObject> terrains = manager.GetManualTerrainPool();
        string terrainKey = terrains.Count.ToString();
        GameObject activeTerrain = null;
        for (int i = 0; i < terrains.Count; i++)
        {
            GameObject terrain = terrains[i];
            if (terrain == null)
                continue;
            terrainKey += terrain.name + terrain.activeSelf;
            if (terrain.activeInHierarchy)
                activeTerrain = terrain;
        }
        if (terrainKey != lastManualTerrainKey || (manualTerrainRow != null && manualTerrainRow.childCount == 0))
        {
            lastManualTerrainKey = terrainKey;
            ClearManualList(manualTerrainRow);
            if (terrains.Count == 0)
                AddMuted(manualTerrainRow, "지형이 없습니다.");
            for (int i = 0; i < terrains.Count; i++)
            {
                GameObject terrain = terrains[i];
                if (terrain == null)
                    continue;
                GameObject captured = terrain;
                Button button = CreateFullButton(manualTerrainRow, captured.name, 36f);
                button.onClick.AddListener(() =>
                {
                    GameSfx.PlayUiClick();
                    manager.SetManualTerrain(captured);
                    lastManualTerrainKey = null;
                });
            }
            if (manualTerrainRow is RectTransform terrainRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(terrainRt);
        }

        if (manualTerrainRow != null)
        {
            for (int i = 0; i < manualTerrainRow.childCount; i++)
            {
                Image image = manualTerrainRow.GetChild(i).GetComponent<Image>();
                Transform child = manualTerrainRow.GetChild(i);
                bool on = activeTerrain != null && child.name == activeTerrain.name;
                if (image != null)
                    image.color = on ? selectedColor : rowColor;
            }
        }

        if (manualPanel != null && manualPanel.activeInHierarchy)
            LayoutRebuilder.ForceRebuildLayoutImmediate(manualPanel.transform as RectTransform);
    }

    void RefreshManualReview()
    {
        Texture2D texture = manager.previewTexture;
        int version = (texture != null ? 1 : 0) + manager.previewBoxes.Count * 17 + (manager.pendingManualName ?? "").GetHashCode();
        if (reviewImage != null)
        {
            reviewImage.enabled = texture != null;
            reviewImage.texture = texture;
            AspectRatioFitter fitter = reviewImage.GetComponent<AspectRatioFitter>();
            if (fitter != null && texture != null && texture.height > 0)
                fitter.aspectRatio = texture.width / (float)texture.height;
        }

        if (version == lastManualReviewVersion)
            return;
        lastManualReviewVersion = version;

        int needed = texture != null ? manager.previewBoxes.Count : 0;
        while (manualReviewBoxPool.Count < needed)
        {
            Image box = UiFont.AddImage(reviewBoxRoot, "Box", Color.clear);
            box.raycastTarget = false;
            AddOutlineEdge(box.rectTransform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f));
            AddOutlineEdge(box.rectTransform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
            AddOutlineEdge(box.rectTransform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
            AddOutlineEdge(box.rectTransform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));
            AddBboxHandles(box.rectTransform);
            Text caption = UiFont.AddText(box.rectTransform, "Id", "", 14, TextAnchor.LowerLeft, Color.white, UiType.BodyBold);
            caption.raycastTarget = false;
            RectTransform capRt = caption.rectTransform;
            capRt.anchorMin = new Vector2(0f, 1f);
            capRt.anchorMax = new Vector2(1f, 1f);
            capRt.pivot = new Vector2(0f, 0f);
            capRt.anchoredPosition = new Vector2(2f, 2f);
            capRt.sizeDelta = new Vector2(-4f, 16f);
            manualReviewBoxPool.Add(box.rectTransform);
        }

        for (int i = 0; i < manualReviewBoxPool.Count; i++)
        {
            RectTransform boxRt = manualReviewBoxPool[i];
            bool on = i < needed;
            SetActive(boxRt.gameObject, on);
            if (!on)
                continue;

            DatasetManager.YoloBox box = manager.previewBoxes[i];
            float xMin = Mathf.Clamp01(box.cx - box.w * 0.5f);
            float xMax = Mathf.Clamp01(box.cx + box.w * 0.5f);
            float yMin = Mathf.Clamp01(1f - (box.cy + box.h * 0.5f));
            float yMax = Mathf.Clamp01(1f - (box.cy - box.h * 0.5f));
            boxRt.anchorMin = new Vector2(xMin, yMin);
            boxRt.anchorMax = new Vector2(xMax, yMax);
            boxRt.offsetMin = Vector2.zero;
            boxRt.offsetMax = Vector2.zero;
            BindBboxHandles(boxRt, i);
            Text caption = boxRt.GetComponentInChildren<Text>();
            if (caption != null)
                caption.text = box.classId.ToString();
            TintPreviewBox(boxRt, i, PreviewBoxClassColor(box.classId));
        }
    }

    public bool IsPointerOverManualUi()
    {
        Vector2 screen = ReadMouseScreenPosition();
        if (manualPanel != null && manualPanel.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(manualPanel.transform as RectTransform, screen, null))
            return true;
        if (manualToggle != null && manualToggle.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(manualToggle.transform as RectTransform, screen, null))
            return true;
        if (manualReviewRoot != null && manualReviewRoot.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(manualReviewRoot.transform as RectTransform, screen, null))
            return true;
        return false;
    }

    GameObject CreateFillPanel(Transform parent)
    {
        Image panel = UiFont.AddImage(parent, "PreviewPanel", panelColor);
        RectTransform rt = panel.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(424f, 24f);
        rt.offsetMax = new Vector2(-24f, -110f);
        return panel.gameObject;
    }

    GameObject CreateSidePanel(string name, bool left, Transform parent = null)
    {
        Image panel = UiFont.AddImage(parent != null ? parent : canvas.transform, name, panelColor);
        RectTransform rt = panel.rectTransform;
        if (left)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(24f, 24f);
            rt.offsetMax = new Vector2(400f, -110f);
        }
        else
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(-400f, 24f);
            rt.offsetMax = new Vector2(-24f, -110f);
        }
        return panel.gameObject;
    }

    GameObject CreateCard(RectTransform parent, string name, Vector2 size)
    {
        Image card = UiFont.AddImage(parent, name, UiSkin.Navy);
        RectTransform rt = card.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return card.gameObject;
    }

    Text AddCardTitle(Transform parent, string text)
    {
        Text title = UiFont.AddText(parent, "Title", text, 24, TextAnchor.MiddleLeft, textColor, UiType.Title);
        title.raycastTarget = false;
        LayoutElement layout = title.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 36f;
        return title;
    }

    Text AddCardError(Transform parent)
    {
        Text error = UiFont.AddText(parent, "Error", "", 16, TextAnchor.MiddleLeft, errorColor, UiType.Body);
        error.raycastTarget = false;
        error.horizontalOverflow = HorizontalWrapMode.Wrap;
        LayoutElement layout = error.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 24f;
        return error;
    }

    void AddCardButtons(Transform parent, string confirmLabel, UnityEngine.Events.UnityAction confirm, UnityEngine.Events.UnityAction cancel)
    {
        GameObject row = new GameObject("Buttons");
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8f;
        rowLayout.childAlignment = TextAnchor.MiddleRight;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        LayoutElement rowElement = row.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 32f;
        rowElement.minHeight = 32f;

        if (!string.IsNullOrEmpty(confirmLabel) && confirm != null)
            CreateCompactButton(rt, "Confirm", confirmLabel, 128f, 32f, confirm);
        if (cancel != null)
            CreateCompactButton(rt, "Cancel", "취소", 88f, 32f, cancel);
    }

    HorizontalLayoutGroup CreateHeader(Transform parent)
    {
        GameObject row = new GameObject("Header");
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        LayoutElement element = row.AddComponent<LayoutElement>();
        element.preferredHeight = 28f;
        element.minHeight = 28f;
        element.flexibleHeight = 0f;
        return layout;
    }

    void AddTitle(Transform parent, string text)
    {
        Text title = UiFont.AddText(parent, "Title", text, 24, TextAnchor.MiddleLeft, textColor, UiType.Title);
        title.raycastTarget = false;
        LayoutElement layout = title.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 36f;
    }

    Text AddSectionLabel(Transform parent, string text)
    {
        Text label = UiFont.AddText(parent, text, text, 18, TextAnchor.MiddleLeft, mutedColor, UiType.BodyMedium);
        label.raycastTarget = false;
        LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 24f;
        return label;
    }

    GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        Image panel = UiFont.AddImage(canvas.transform, name, panelColor);
        RectTransform rt = panel.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return panel.gameObject;
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

    static bool IsLeftPointer(BaseEventData eventData)
    {
        PointerEventData pointer = eventData as PointerEventData;
        return pointer == null || pointer.button == PointerEventData.InputButton.Left;
    }

    static bool IsLeftMouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.leftButton.isPressed;
        return false;
#else
        return Input.GetMouseButton(0);
#endif
    }

    static Vector2 ReadMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    void AddMuted(Transform parent, string text)
    {
        Text label = UiFont.AddText(parent, "Empty", text, 16, TextAnchor.MiddleLeft, mutedColor, UiType.Body);
        label.raycastTarget = false;
        LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 28f;
    }

    ScrollRect CreateScroll(string name, Transform parent, float flex, float height = 0f)
    {
        Image frame = UiFont.AddImage(parent, name, insetColor);
        LayoutElement layout = frame.gameObject.AddComponent<LayoutElement>();
        layout.flexibleHeight = flex;
        if (height > 0f)
        {
            layout.minHeight = height;
            layout.preferredHeight = height;
        }
        else
            layout.minHeight = 80f;
        frame.gameObject.AddComponent<RectMask2D>();
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
        contentRt.sizeDelta = Vector2.zero;
        VerticalLayoutGroup list = content.AddComponent<VerticalLayoutGroup>();
        list.padding = new RectOffset(8, 8, 8, 8);
        list.spacing = 6f;
        list.childControlWidth = true;
        list.childControlHeight = true;
        list.childForceExpandWidth = true;
        list.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRt;
        scroll.viewport = frame.rectTransform;
        return scroll;
    }

    Image CreateRow(
        Transform parent,
        string name,
        Color color,
        float height,
        UnityEngine.Events.UnityAction onSelect = null,
        UnityEngine.Events.UnityAction onDoubleClick = null)
    {
        Image row = UiFont.AddImage(parent, name, color);
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        row.gameObject.AddComponent<UiScrollRelay>();
        if (onDoubleClick != null)
        {
            UiDoubleClick click = row.gameObject.AddComponent<UiDoubleClick>();
            click.onClick = onSelect;
            click.onDoubleClick = onDoubleClick;
        }
        else if (onSelect != null)
        {
            Button select = row.gameObject.AddComponent<Button>();
            select.targetGraphic = row;
            select.transition = Selectable.Transition.None;
            select.onClick.AddListener(() =>
            {
                GameSfx.PlayUiClick();
                onSelect();
            });
        }

        return row;
    }

    void PruneCheckedSamples()
    {
        if (checkedSampleIds.Count == 0)
            return;
        HashSet<string> live = new HashSet<string>();
        for (int i = 0; i < manager.datasetSamples.Count; i++)
        {
            DatasetManager.DatasetSample sample = manager.datasetSamples[i];
            if (sample != null)
                live.Add(sample.id);
        }
        checkedSampleIds.RemoveWhere(id => !live.Contains(id));
    }

    void AddSampleCheck(RectTransform parent, string sampleId)
    {
        Image box = UiFont.AddImage(parent, "Check", insetColor);
        RectTransform boxRt = box.rectTransform;
        boxRt.anchorMin = new Vector2(0f, 0.5f);
        boxRt.anchorMax = new Vector2(0f, 0.5f);
        boxRt.pivot = new Vector2(0f, 0.5f);
        boxRt.anchoredPosition = new Vector2(8f, 0f);
        boxRt.sizeDelta = new Vector2(22f, 22f);

        Image mark = UiFont.AddImage(boxRt, "Mark", Color.white);
        mark.raycastTarget = false;
        RectTransform markRt = mark.rectTransform;
        markRt.anchorMin = new Vector2(0.5f, 0.5f);
        markRt.anchorMax = new Vector2(0.5f, 0.5f);
        markRt.pivot = new Vector2(0.5f, 0.5f);
        markRt.sizeDelta = new Vector2(12f, 12f);
        markRt.anchoredPosition = Vector2.zero;
        mark.enabled = checkedSampleIds.Contains(sampleId);

        Toggle toggle = box.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = mark;
        toggle.transition = Selectable.Transition.None;
        toggle.isOn = checkedSampleIds.Contains(sampleId);
        toggle.onValueChanged.AddListener(on =>
        {
            if (on)
                checkedSampleIds.Add(sampleId);
            else
                checkedSampleIds.Remove(sampleId);
            mark.enabled = on;
        });
        box.gameObject.AddComponent<UiScrollRelay>();
    }

    void AddFlexActionButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction click)
    {
        Button button = CreateCompactButton(parent, name, label, 72f, 28f, click);
        LayoutElement layout = button.GetComponent<LayoutElement>();
        layout.preferredWidth = 0f;
        layout.minWidth = 64f;
        layout.flexibleWidth = 1f;
        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
            text.fontSize = 13;
    }

    void AddRowButton(RectTransform parent, string name, string label, float left, float right, UnityEngine.Events.UnityAction click)
    {
        Image image = UiFont.AddImage(parent, name, UiSkin.Blue);
        RectTransform rt = image.rectTransform;
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(right, 0f);
        rt.sizeDelta = new Vector2(Mathf.Abs(left - right) - 4f, 28f);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        image.gameObject.AddComponent<UiScrollRelay>();
        button.onClick.AddListener(() =>
        {
            GameSfx.PlayUiClick();
            click();
        });
        Text text = UiFont.AddText(rt, "Label", label, 14, TextAnchor.MiddleCenter, buttonLabel, UiType.BodyMedium);
        text.raycastTarget = false;
        UiFont.Stretch(text.rectTransform);
    }

    Button CreateBarButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction click)
    {
        Image image = UiFont.AddImage(parent, name, UiSkin.Blue);
        LayoutElement layout = image.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 40f;
        layout.minHeight = 40f;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            GameSfx.PlayUiClick();
            click();
        });
        Text text = UiFont.AddText(image.rectTransform, "Label", label, 18, TextAnchor.MiddleCenter, buttonLabel, UiType.BodyMedium);
        text.raycastTarget = false;
        UiFont.Stretch(text.rectTransform);
        return button;
    }

    Button CreateCompactButton(Transform parent, string name, string label, float width, float height, UnityEngine.Events.UnityAction click)
    {
        Image image = UiFont.AddImage(parent, name, UiSkin.Blue);
        LayoutElement layout = image.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        layout.preferredHeight = height;
        layout.minHeight = height;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            GameSfx.PlayUiClick();
            click();
        });
        Text text = UiFont.AddText(image.rectTransform, "Label", label, 16, TextAnchor.MiddleCenter, buttonLabel, UiType.BodyMedium);
        text.raycastTarget = false;
        UiFont.Stretch(text.rectTransform);
        return button;
    }

    static void AddOutlineEdge(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
    {
        Image edge = UiFont.AddImage(parent, name, Color.white);
        edge.raycastTarget = false;
        RectTransform rt = edge.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    Button CreateFullButton(Transform parent, string label, float height)
    {
        Image image = UiFont.AddImage(parent, label, rowColor);
        LayoutElement layout = image.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        image.gameObject.AddComponent<UiScrollRelay>();
        Text text = UiFont.AddText(image.rectTransform, "Label", label, 16, TextAnchor.MiddleLeft, textColor, UiType.BodyMedium);
        text.raycastTarget = false;
        UiFont.Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(12f, 0f);
        text.rectTransform.offsetMax = new Vector2(-12f, 0f);
        return button;
    }

    InputField CreateIntRow(Transform parent, string label, UnityEngine.Events.UnityAction onEnd)
    {
        GameObject row = new GameObject(label);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        LayoutElement element = row.AddComponent<LayoutElement>();
        element.preferredHeight = 26f;
        element.minHeight = 26f;
        element.flexibleHeight = 0f;

        Text text = UiFont.AddText(rt, "Label", label, 15, TextAnchor.MiddleLeft, textColor, UiType.Body);
        text.raycastTarget = false;
        LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
        textLayout.preferredWidth = 120f;
        textLayout.flexibleWidth = 1f;
        textLayout.preferredHeight = 26f;

        return CreateField(rt, true, onEnd);
    }

    InputField CreateDialogField(Transform parent, string label, bool integer)
    {
        Text text = UiFont.AddText(parent, "FieldLabel", label, 16, TextAnchor.MiddleLeft, mutedColor, UiType.Body);
        text.raycastTarget = false;
        LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
        textLayout.preferredHeight = 22f;
        return CreateField(parent, integer, null, true);
    }

    InputField CreateField(Transform parent, bool integer, UnityEngine.Events.UnityAction onEnd, bool expand = false)
    {
        Image image = UiFont.AddImage(parent, "Field", insetColor);
        LayoutElement layout = image.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 24f;
        layout.minHeight = 24f;
        layout.minWidth = 56f;
        if (expand)
        {
            layout.flexibleWidth = 1f;
            layout.preferredWidth = 0f;
        }
        else
        {
            layout.preferredWidth = 72f;
            layout.flexibleWidth = 0f;
        }
        InputField field = image.gameObject.AddComponent<InputField>();
        Text value = UiFont.AddText(image.rectTransform, "Text", "", 15, TextAnchor.MiddleLeft, textColor, UiType.Body);
        value.raycastTarget = true;
        UiFont.Stretch(value.rectTransform);
        value.rectTransform.offsetMin = new Vector2(8f, 0f);
        value.rectTransform.offsetMax = new Vector2(-8f, 0f);
        field.textComponent = value;
        field.contentType = integer ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;
        if (onEnd != null)
            field.onEndEdit.AddListener(_ => onEnd());
        return field;
    }

    static void CommitInt(InputField field, ref int value, int min, int max)
    {
        if (field == null)
            return;
        int parsed;
        if (!int.TryParse(field.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            field.text = value.ToString();
            return;
        }

        value = Mathf.Clamp(parsed, min, max);
        field.text = value.ToString();
    }

    static void SyncField(InputField field, string value)
    {
        if (field == null || field.isFocused)
            return;
        if (field.text != value)
            field.text = value;
    }

    static void SetText(Text text, string value)
    {
        if (text != null && text.text != value)
            text.text = value;
    }

    static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.Destroy(parent.GetChild(i).gameObject);
    }

    static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        EventSystem eventSystem = go.AddComponent<EventSystem>();
        eventSystem.sendNavigationEvents = false;
        go.AddComponent<InputSystemUIInputModule>();
    }
}

public class UiDoubleClick : MonoBehaviour, IPointerClickHandler
{
    public UnityEngine.Events.UnityAction onClick;
    public UnityEngine.Events.UnityAction onDoubleClick;
    float lastClickTime;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        GameSfx.PlayUiClick();
        if (onDoubleClick != null && Time.unscaledTime - lastClickTime < 0.35f)
        {
            lastClickTime = 0f;
            onDoubleClick();
            return;
        }

        lastClickTime = Time.unscaledTime;
        if (onClick != null)
            onClick();
    }
}

public class UiBboxHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public HudDatasetUi hud;
    public int boxIndex;
    public int corner;

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        if (hud != null)
            hud.SetPreviewBoxEditing(boxIndex, true);
        Apply(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Finish();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        if (hud != null)
            hud.SetPreviewBoxEditing(boxIndex, true);
        Apply(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        Apply(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Finish();
    }

    void Finish()
    {
        if (hud == null)
            return;
        hud.CommitPreviewBoxEdit();
        hud.SetPreviewBoxEditing(boxIndex, false);
    }

    void Apply(PointerEventData eventData)
    {
        if (hud != null)
            hud.DragPreviewBoxCorner(boxIndex, corner, eventData.position, eventData.pressEventCamera);
    }
}

public class UiScrollRelay : MonoBehaviour, IScrollHandler
{
    ScrollRect scroll;
    DatasetManager datasetManager;

    public void OnScroll(PointerEventData eventData)
    {
        if (datasetManager == null)
            datasetManager = FindFirstObjectByType<DatasetManager>();
        if (datasetManager != null && datasetManager.IsManualDragging)
        {
            datasetManager.ApplyManualGhostScroll(eventData.scrollDelta.y);
            return;
        }

        if (scroll == null)
            scroll = GetComponentInParent<ScrollRect>();
        if (scroll != null)
            scroll.OnScroll(eventData);
    }
}
