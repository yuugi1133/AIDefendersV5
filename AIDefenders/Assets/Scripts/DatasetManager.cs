using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DatasetAnimal
{
    public Target_Base animal;          // 랜덤화에 쓸 원본(씬에 배치)
    public int classID;                 // YOLO 클래스 ID
    public string label = "unknown";    // 클래스 이름(로그/구분용)
    public string[] animationStates;    // 이 동물의 애니메이션 상태
}

public class DatasetManager : MonoBehaviour
{
    [Header("Capture Settings")]
    public Camera datasetCamera;
    public RenderTexture renderTexture;
    [Range(1, 100)]
    public int jpegQuality = 70;        // CCTV EncodeToJPG(70)과 맞춤

    [Header("Animals")]
    public DatasetAnimal[] animalPool;  // 랜덤화할 대상 리스트
    public List<Target_Base> captureTargets = new List<Target_Base>();  // 실제 촬영 대상
    public int minCaptureCount = 1;
    public int maxCaptureCount = 3;
    public float minSpacing = 2f;
    public LayerMask groundMask;
    public float raycastHeight = 80f;
    public float spawnHeightOffset = 0.05f;
    [Range(0.2f, 1f)]
    public float spawnViewFill = 0.65f;

    [Header("Camera")]
    public Vector2 cameraDistanceRange = new Vector2(30f, 80f);
    public Vector2 cameraPitchRange = new Vector2(15f, 75f);
    public Vector2 cameraYawRange = new Vector2(0f, 360f);
    public Vector2 cameraFovRange = new Vector2(35f, 70f);
    public int maxSpawnAttempts = 30;
    public int maxOverlapResolvePasses = 4;

    Vector3 lastCaptureOrigin;
    float lastSpawnRadius = 8f;

    [Header("Lighting")]
    public Light directionalLight;

    [Header("Dataset")]
    public bool writeDatasetYaml = true;
    public Transform terrainsRoot;
    public string targetPrefabsFolder = "Assets/Targets";

    readonly List<int> captureClassIds = new List<int>();
    readonly List<string[]> captureAnimationStates = new List<string[]>();
    readonly List<string> datasetNames = new List<string>();
    readonly List<DatasetSample> datasetSamples = new List<DatasetSample>();
    readonly List<YoloBox> previewBoxes = new List<YoloBox>();
    Texture2D screenTexture;
    Texture2D previewTexture;
    Texture2D pixelTexture;
    Vector2 datasetListScroll;
    Vector2 sampleListScroll;
    Vector2 captureSettingsScroll;
    Vector2 includedTargetsScroll;
    Vector2 includedTerrainsScroll;
    Vector2 prefabPickScroll;
    Vector2 terrainPickScroll;
    string selectedDataset;
    string editingDataset;
    string selectedSampleId;
    bool editMode;
    bool showNameDialog;
    bool nameDialogIsRename;
    bool showPrefabDialog;
    bool showTerrainDialog;
    bool showGenerateDialog;
    bool showTrainDialog;
    bool captureSettingsDirty;
    string nameDialogValue = "";
    string nameDialogError = "";
    string generateDialogError = "";
    string trainDialogError = "";
    int pendingTrainCount = 70;
    int pendingValCount = 20;
    int pendingTestCount = 10;
    int trainEpochs = 30;
    int trainBatchSize = 32;
    int existingTrainImages;
    int existingValImages;
    Coroutine generateRoutine;
    System.Diagnostics.Process trainProcess;
    bool waitingForTrain;
    bool showTrainResult;
    TrainResultData trainResult;

    [System.Serializable]
    class TrainResultData
    {
        public string status = "";
        public string message = "";
        public string dataset = "";
        public int epochs;
        public int batch;
        public float precision;
        public float recall;
        public float map50;
        public float map50_95;
        public string lastWeights = "";
        public string bestWeights = "";
    }

    readonly List<Target_Base> includedTargets = new List<Target_Base>();
    readonly List<Target_Base> prefabCatalog = new List<Target_Base>();
    readonly List<GameObject> includedTerrains = new List<GameObject>();
    readonly List<GameObject> terrainCatalog = new List<GameObject>();
    readonly Dictionary<string, string> settingDrafts = new Dictionary<string, string>();
    string lastFocusedSetting;

    const string DatasetsFolderName = "Datasets";
    const float MinCameraDistance = 30f;
    static readonly Color[] PreviewBoxColors =
    {
        new Color(0.25f, 1f, 0.35f),
        new Color(1f, 0.85f, 0.2f),
        new Color(0.35f, 0.75f, 1f),
        new Color(1f, 0.4f, 0.35f),
        new Color(0.95f, 0.45f, 1f)
    };

    [System.Serializable]
    class CaptureSettingsData
    {
        public string[] targetPrefabPaths;
        public string[] terrainNames;
        public int minCaptureCount;
        public int maxCaptureCount;
        public float minSpacing;
        public float spawnViewFill;
        public float cameraDistanceMin;
        public float cameraDistanceMax;
        public float cameraPitchMin;
        public float cameraPitchMax;
        public float cameraYawMin;
        public float cameraYawMax;
        public float cameraFovMin;
        public float cameraFovMax;
        public int jpegQuality;
        public int trainSampleCount;
        public int valSampleCount;
        public int testSampleCount;
        public int trainEpochs;
        public int trainBatchSize;
    }

    class DatasetSample
    {
        public string id;
        public string imagePath;
        public string labelPath;
    }

    struct YoloBox
    {
        public int classId;
        public float cx;
        public float cy;
        public float w;
        public float h;
    }

    void Start()
    {
        EnsureCaptureDefaults();
        ResolveTerrainsRoot();
        RefreshDatasetList();

        if (animalPool != null)
            HidePoolAnimals();

        RenderSettings.fogMode = FogMode.Exponential;

        if (datasetCamera == null || renderTexture == null)
            return;

        screenTexture = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGB24,
            false
        );
    }

    void Update()
    {
        if (waitingForTrain)
            PollTrainingProcess();
    }

    void OnDestroy()
    {
        ClearPreview();
        if (pixelTexture != null)
            Destroy(pixelTexture);
    }

    void OnGUI()
    {
        if (editMode)
        {
            DrawSampleEditor();
            DrawCaptureSettingsPanel();
        }
        else
            DrawDatasetSidebar();

        if (waitingForTrain)
            DrawTrainProgressDialog();
        else if (showTrainResult)
            DrawTrainResultDialog();
        else if (showGenerateDialog)
            DrawGenerateCountDialog();
        else if (showTrainDialog)
            DrawTrainDialog();
        else if (showPrefabDialog)
            DrawPrefabPickDialog();
        else if (showTerrainDialog)
            DrawTerrainPickDialog();
        else if (showNameDialog)
            DrawNameDialog();

        if (captureSettingsDirty)
            SaveCaptureSettings();

        lastFocusedSetting = GUI.GetNameOfFocusedControl();
    }

    void DrawDatasetSidebar()
    {
        Rect panel = new Rect(12f, 12f, 320f, Screen.height - 24f);
        GUI.Box(panel, "");

        GUILayout.BeginArea(new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, panel.height - 16f));
        GUILayout.Label("데이터셋");

        datasetListScroll = GUILayout.BeginScrollView(datasetListScroll, GUILayout.ExpandHeight(true));
        if (datasetNames.Count == 0)
            GUILayout.Label("비어 있음");

        for (int i = 0; i < datasetNames.Count; i++)
        {
            string name = datasetNames[i];
            GUILayout.BeginHorizontal();

            bool selected = name == selectedDataset;
            if (GUILayout.Toggle(selected, name, "Button", GUILayout.ExpandWidth(true), GUILayout.Height(24f)) && !selected)
                selectedDataset = name;

            if (GUILayout.Button("편집", GUILayout.Width(44f), GUILayout.Height(24f)))
                EnterEditMode(name);

            if (GUILayout.Button("삭제", GUILayout.Width(44f), GUILayout.Height(24f)))
                DeleteDataset(name);

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.Space(8f);

        if (GUILayout.Button("새로 만들기", GUILayout.Height(32f)))
            OpenNameDialog(false);

        GUILayout.Space(6f);
        if (GUILayout.Button("목록 새로고침", GUILayout.Height(24f)))
            RefreshDatasetList();

        GUILayout.EndArea();
    }

    void DrawSampleEditor()
    {
        Rect panel = new Rect(12f, 12f, 340f, Screen.height - 24f);
        GUI.Box(panel, "");

        GUILayout.BeginArea(new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, panel.height - 16f));

        GUILayout.BeginHorizontal();
        GUILayout.Label(string.IsNullOrEmpty(editingDataset) ? "샘플" : editingDataset);
        if (GUILayout.Button("뒤로", GUILayout.Width(52f), GUILayout.Height(24f)))
            ExitEditMode();
        GUILayout.EndHorizontal();

        GUILayout.Label("샘플 리스트");
        sampleListScroll = GUILayout.BeginScrollView(sampleListScroll, GUILayout.ExpandHeight(true));
        if (datasetSamples.Count == 0)
            GUILayout.Label("비어 있음");

        for (int i = 0; i < datasetSamples.Count; i++)
        {
            DatasetSample sample = datasetSamples[i];
            GUILayout.BeginHorizontal();

            bool selected = sample.id == selectedSampleId;
            if (GUILayout.Toggle(selected, sample.id, "Button", GUILayout.ExpandWidth(true), GUILayout.Height(24f)) && !selected)
                SelectSample(sample);

            if (GUILayout.Button("삭제", GUILayout.Width(44f), GUILayout.Height(24f)))
                DeleteSample(sample);

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.Space(6f);
        GUILayout.Label("미리보기");
        float previewHeight = Mathf.Clamp(Screen.height * 0.32f, 160f, 280f);
        Rect previewBox = GUILayoutUtility.GetRect(1f, previewHeight, GUILayout.ExpandWidth(true));
        DrawSamplePreview(previewBox);

        GUILayout.EndArea();
    }

    void DrawCaptureSettingsPanel()
    {
        float width = 360f;
        Rect panel = new Rect(Screen.width - width - 12f, 12f, width, Screen.height - 24f);
        GUI.Box(panel, "");
        GUILayout.BeginArea(new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, panel.height - 16f));

        GUILayout.Label("샘플 생성 설정");
        GUILayout.Space(4f);
        GUILayout.Label("대상");
        includedTargetsScroll = GUILayout.BeginScrollView(includedTargetsScroll, GUILayout.Height(110f));
        if (includedTargets.Count == 0)
            GUILayout.Label("비어 있음");

        int removeTarget = -1;
        for (int i = 0; i < includedTargets.Count; i++)
        {
            Target_Base target = includedTargets[i];
            if (target == null)
            {
                removeTarget = i;
                continue;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(target.gameObject.name, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("삭제", GUILayout.Width(44f), GUILayout.Height(22f)))
                removeTarget = i;

            GUILayout.EndHorizontal();
        }

        if (removeTarget >= 0)
        {
            includedTargets.RemoveAt(removeTarget);
            captureSettingsDirty = true;
        }

        GUILayout.EndScrollView();
        if (GUILayout.Button("대상 추가", GUILayout.Height(26f)))
        {
            RefreshPrefabCatalog();
            showPrefabDialog = true;
            showTerrainDialog = false;
        }

        GUILayout.Space(8f);
        GUILayout.Label("지형");
        includedTerrainsScroll = GUILayout.BeginScrollView(includedTerrainsScroll, GUILayout.Height(90f));
        if (includedTerrains.Count == 0)
            GUILayout.Label("비어 있음");

        int removeTerrain = -1;
        for (int i = 0; i < includedTerrains.Count; i++)
        {
            GameObject terrain = includedTerrains[i];
            if (terrain == null)
            {
                removeTerrain = i;
                continue;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(terrain.name, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("삭제", GUILayout.Width(44f), GUILayout.Height(22f)))
                removeTerrain = i;

            GUILayout.EndHorizontal();
        }

        if (removeTerrain >= 0)
        {
            includedTerrains.RemoveAt(removeTerrain);
            captureSettingsDirty = true;
        }

        GUILayout.EndScrollView();
        if (GUILayout.Button("지형 추가", GUILayout.Height(26f)))
        {
            RefreshTerrainCatalog();
            showTerrainDialog = true;
            showPrefabDialog = false;
        }

        GUILayout.Space(8f);
        captureSettingsScroll = GUILayout.BeginScrollView(captureSettingsScroll, GUILayout.ExpandHeight(true));
        bool minCountCommitted = DrawIntSetting("최소 대상 수", ref minCaptureCount, 1, 32);
        bool maxCountCommitted = DrawIntSetting("최대 대상 수", ref maxCaptureCount, 1, 32);
        if (minCountCommitted || maxCountCommitted)
            FixIntPair(ref minCaptureCount, ref maxCaptureCount, minCountCommitted);

        DrawFloatSetting("최소 간격", ref minSpacing, 0f, 50f);
        DrawRangeSetting("카메라 거리", ref cameraDistanceRange, MinCameraDistance, 500f);
        DrawRangeSetting("카메라 피치", ref cameraPitchRange, 0f, 89f);
        DrawRangeSetting("카메라 요", ref cameraYawRange, -360f, 360f);
        DrawRangeSetting("카메라 FOV", ref cameraFovRange, 10f, 120f);
        DrawIntSetting("JPEG 품질", ref jpegQuality, 1, 100);
        GUILayout.EndScrollView();

        GUILayout.Space(6f);
        bool canGenerate = CanGenerateSamples();
        GUI.enabled = canGenerate;
        if (GUILayout.Button("샘플 생성하기", GUILayout.Height(36f)))
        {
            SaveCaptureSettings();
            generateDialogError = "";
            showGenerateDialog = true;
            showTrainDialog = false;
            showPrefabDialog = false;
            showTerrainDialog = false;
        }
        GUI.enabled = true;
        if (!canGenerate && generateRoutine == null && !waitingForTrain)
            GUILayout.Label("대상과 지형을 각각 하나 이상 추가하세요.");

        GUILayout.Space(4f);
        bool canTrain = CanTrainModel();
        GUI.enabled = canTrain;
        if (GUILayout.Button("모델 학습시키기", GUILayout.Height(36f)))
        {
            RefreshSampleList();
            SaveCaptureSettings();
            trainDialogError = "";
            showTrainDialog = true;
            showGenerateDialog = false;
            showPrefabDialog = false;
            showTerrainDialog = false;
        }
        GUI.enabled = true;
        if (!canTrain && generateRoutine == null && !waitingForTrain)
            GUILayout.Label("학습(train)과 검증(val) 샘플이 각각 하나 이상 있어야 합니다.");

        GUILayout.EndArea();
    }

    void DrawPrefabPickDialog()
    {
        Rect box = CenterDialogRect(420f, 360f);
        GUI.Box(box, "대상 프리팹 선택");
        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 28f, box.width - 32f, box.height - 36f));
        GUILayout.Label("포함할 Target_Base 프리팹");

        prefabPickScroll = GUILayout.BeginScrollView(prefabPickScroll, GUILayout.ExpandHeight(true));
        int shown = 0;
        for (int i = 0; i < prefabCatalog.Count; i++)
        {
            Target_Base prefab = prefabCatalog[i];
            if (prefab == null || includedTargets.Contains(prefab))
                continue;

            shown++;
            if (GUILayout.Button(prefab.gameObject.name, GUILayout.Height(26f)))
            {
                includedTargets.Add(prefab);
                captureSettingsDirty = true;
                showPrefabDialog = false;
            }
        }

        if (shown == 0)
            GUILayout.Label("추가할 프리팹이 없습니다.");

        GUILayout.EndScrollView();
        if (GUILayout.Button("취소", GUILayout.Height(28f)))
            showPrefabDialog = false;

        GUILayout.EndArea();
    }

    void DrawTerrainPickDialog()
    {
        Rect box = CenterDialogRect(420f, 320f);
        GUI.Box(box, "지형 선택");
        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 28f, box.width - 32f, box.height - 36f));
        GUILayout.Label("Terrains 안의 지형");

        terrainPickScroll = GUILayout.BeginScrollView(terrainPickScroll, GUILayout.ExpandHeight(true));
        int shown = 0;
        for (int i = 0; i < terrainCatalog.Count; i++)
        {
            GameObject terrain = terrainCatalog[i];
            if (terrain == null || includedTerrains.Contains(terrain))
                continue;

            shown++;
            if (GUILayout.Button(terrain.name, GUILayout.Height(26f)))
            {
                includedTerrains.Add(terrain);
                captureSettingsDirty = true;
                showTerrainDialog = false;
            }
        }

        if (shown == 0)
            GUILayout.Label("추가할 지형이 없습니다.");

        GUILayout.EndScrollView();
        if (GUILayout.Button("취소", GUILayout.Height(28f)))
            showTerrainDialog = false;

        GUILayout.EndArea();
    }

    void DrawGenerateCountDialog()
    {
        Rect box = CenterDialogRect(400f, 250f);
        GUI.Box(box, "샘플 수");
        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 28f, box.width - 32f, box.height - 36f));
        GUILayout.Label("생성할 샘플 수");
        DrawIntSetting("학습(train)", ref pendingTrainCount, 0, 100000);
        DrawIntSetting("검증(val)", ref pendingValCount, 0, 100000);
        DrawIntSetting("테스트(test)", ref pendingTestCount, 0, 100000);

        if (!string.IsNullOrEmpty(generateDialogError))
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.4f);
            GUILayout.Label(generateDialogError);
            GUI.color = previous;
        }

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUI.enabled = generateRoutine == null;
        if (GUILayout.Button("최종 생성하기", GUILayout.Height(28f)))
            ConfirmGenerateCounts();
        GUI.enabled = true;

        if (GUILayout.Button("취소", GUILayout.Height(28f)))
            showGenerateDialog = false;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void DrawTrainDialog()
    {
        Rect box = CenterDialogRect(420f, 250f);
        GUI.Box(box, "모델 학습");
        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 28f, box.width - 32f, box.height - 36f));
        GUILayout.Label("학습 설정");
        GUILayout.Label("Python " + PythonRuntime.RequiredVersionLabel + " 가상환경으로 학습합니다.");
        DrawIntSetting("에포크", ref trainEpochs, 1, 1000);
        DrawIntSetting("배치 사이즈", ref trainBatchSize, 1, 256);

        bool venvReady = PythonRuntime.IsVenvReady();
        if (!venvReady)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.4f);
            GUILayout.Label("가상환경(.venv)이 없습니다. 메인 메뉴에서 라이브러리를 설치하세요.");
            GUI.color = previous;
        }

        if (!string.IsNullOrEmpty(trainDialogError))
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.4f);
            GUILayout.Label(trainDialogError);
            GUI.color = previous;
        }

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUI.enabled = generateRoutine == null && !waitingForTrain && venvReady;
        if (GUILayout.Button("학습 시작", GUILayout.Height(28f)))
            ConfirmStartTraining();
        GUI.enabled = true;

        if (GUILayout.Button("취소", GUILayout.Height(28f)))
            showTrainDialog = false;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void ConfirmStartTraining()
    {
        ApplySettingDrafts();
        RefreshSampleList();
        if (!CanTrainModel())
        {
            trainDialogError = "학습(train)과 검증(val) 샘플이 각각 하나 이상 있어야 합니다.";
            return;
        }

        if (trainEpochs < 1)
            trainEpochs = 1;
        if (trainBatchSize < 1)
            trainBatchSize = 1;

        trainDialogError = "";
        SaveCaptureSettings();
        showTrainDialog = false;
        StartYoloTraining();
    }

    void DrawTrainProgressDialog()
    {
        Rect box = CenterDialogRect(420f, 150f);
        GUI.Box(box, "모델 학습");
        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 28f, box.width - 32f, box.height - 36f));
        GUILayout.Label("모델 학습 진행 중");
        GUILayout.Label("CMD 창에서 에포크 진행률을 확인할 수 있습니다.");
        GUILayout.FlexibleSpace();
        GUILayout.Label("완료되면 결과가 이 창에 표시됩니다.");
        GUILayout.EndArea();
    }

    void DrawTrainResultDialog()
    {
        Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
        Rect box = CenterDialogRect(480f, 300f);
        GUI.Button(screen, GUIContent.none, GUIStyle.none);
        GUI.Box(box, trainResult != null && trainResult.status == "failed" ? "학습 실패" : "학습 완료");

        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 28f, box.width - 32f, box.height - 80f));
        if (trainResult == null)
        {
            GUILayout.Label("결과 파일을 읽지 못했습니다.");
        }
        else if (trainResult.status == "failed")
        {
            GUILayout.Label(string.IsNullOrEmpty(trainResult.message) ? "학습에 실패했습니다." : trainResult.message);
        }
        else
        {
            GUILayout.Label(string.IsNullOrEmpty(trainResult.message) ? "학습이 완료되었습니다." : trainResult.message);
            GUILayout.Space(8f);
            GUILayout.Label("Precision: " + trainResult.precision.ToString("0.###", CultureInfo.InvariantCulture));
            GUILayout.Label("Recall: " + trainResult.recall.ToString("0.###", CultureInfo.InvariantCulture));
            GUILayout.Label("mAP50: " + trainResult.map50.ToString("0.###", CultureInfo.InvariantCulture));
            GUILayout.Label("mAP50-95: " + trainResult.map50_95.ToString("0.###", CultureInfo.InvariantCulture));
            GUILayout.Space(8f);
            if (!string.IsNullOrEmpty(trainResult.lastWeights))
                GUILayout.Label("last.pt: " + Path.GetFileName(trainResult.lastWeights));
            if (!string.IsNullOrEmpty(trainResult.bestWeights))
                GUILayout.Label("best.pt: " + Path.GetFileName(trainResult.bestWeights));
        }
        GUILayout.EndArea();

        Rect closeRect = new Rect(box.x + 16f, box.y + box.height - 42f, box.width - 32f, 28f);
        if (GUI.Button(closeRect, "닫기"))
        {
            showTrainResult = false;
            trainResult = null;
        }
    }

    void StartYoloTraining()
    {
        string datasetName = string.IsNullOrEmpty(editingDataset) ? selectedDataset : editingDataset;
        if (string.IsNullOrEmpty(datasetName))
        {
            trainDialogError = "데이터셋을 먼저 선택하세요.";
            showTrainDialog = true;
            return;
        }

        string datasetFolder = GetDatasetFolder(datasetName);
        string yamlPath = Path.Combine(datasetFolder, "dataset.yaml");
        string resultPath = Path.Combine(datasetFolder, "train_result.json");
        string weightsDir = Path.Combine(datasetFolder, "weights");
        Directory.CreateDirectory(weightsDir);
        WriteDatasetYaml(datasetFolder);

        System.Diagnostics.Process process;
        string error;
        if (!PythonRuntime.TryStartYoloTraining(
            yamlPath,
            trainEpochs,
            trainBatchSize,
            datasetName,
            resultPath,
            weightsDir,
            out process,
            out error))
        {
            trainDialogError = error;
            showTrainDialog = true;
            return;
        }

        trainProcess = process;
        waitingForTrain = true;
        showTrainResult = false;
        trainResult = null;
        Debug.Log("YOLO 학습 시작: " + PythonRuntime.GetVenvPython() + " " + yamlPath);
    }

    void PollTrainingProcess()
    {
        try
        {
            if (trainProcess != null && !trainProcess.HasExited)
                return;
        }
        catch (System.Exception)
        {
        }

        FinishTraining();
    }

    void FinishTraining()
    {
        waitingForTrain = false;
        if (trainProcess != null)
        {
            trainProcess.Dispose();
            trainProcess = null;
        }

        string datasetName = string.IsNullOrEmpty(editingDataset) ? selectedDataset : editingDataset;
        string resultPath = string.IsNullOrEmpty(datasetName)
            ? ""
            : Path.Combine(GetDatasetFolder(datasetName), "train_result.json");

        trainResult = null;
        if (!string.IsNullOrEmpty(resultPath) && File.Exists(resultPath))
        {
            try
            {
                trainResult = JsonUtility.FromJson<TrainResultData>(File.ReadAllText(resultPath));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("학습 결과 JSON 읽기 실패: " + e.Message);
            }
        }

        if (trainResult == null)
        {
            trainResult = new TrainResultData();
            trainResult.status = "failed";
            trainResult.message = "학습 결과 파일을 찾지 못했습니다.";
        }
        else if (trainResult.status == "running")
        {
            trainResult.status = "failed";
            trainResult.message = "학습이 끝났지만 완료 결과를 받지 못했습니다.";
        }

        showTrainResult = true;
        RefreshAssets();
        Debug.Log("YOLO 학습 종료: " + trainResult.status + " " + trainResult.message);
    }

    void ConfirmGenerateCounts()
    {
        ApplySettingDrafts();
        if (!CanGenerateSamples())
        {
            generateDialogError = "대상과 지형을 각각 하나 이상 추가하세요.";
            return;
        }

        if (pendingTrainCount + pendingValCount + pendingTestCount <= 0)
        {
            generateDialogError = "생성할 샘플 수를 입력하세요.";
            return;
        }

        generateDialogError = "";
        SaveCaptureSettings();
        showGenerateDialog = false;

        if (generateRoutine != null)
            StopCoroutine(generateRoutine);

        generateRoutine = StartCoroutine(GenerateDataset());
    }

    Rect CenterDialogRect(float width, float height)
    {
        return new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );
    }

    bool DrawIntSetting(string label, ref int value, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(120f));
        bool commit = DrawDeferredTextField("int:" + label, value.ToString(), out string text);
        GUILayout.EndHorizontal();
        if (!commit)
            return false;

        int parsed;
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            settingDrafts["int:" + label] = value.ToString();
            return false;
        }

        parsed = Mathf.Clamp(parsed, min, max);
        if (parsed == value)
        {
            settingDrafts["int:" + label] = value.ToString();
            return false;
        }

        value = parsed;
        settingDrafts["int:" + label] = value.ToString();
        captureSettingsDirty = true;
        return true;
    }

    void DrawFloatSetting(string label, ref float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(120f));
        bool commit = DrawDeferredTextField(
            "float:" + label,
            value.ToString("0.###", CultureInfo.InvariantCulture),
            out string text);
        GUILayout.EndHorizontal();
        if (!commit)
            return;

        float parsed;
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
        {
            settingDrafts["float:" + label] = value.ToString("0.###", CultureInfo.InvariantCulture);
            return;
        }

        parsed = Mathf.Clamp(parsed, min, max);
        if (Mathf.Abs(parsed - value) < 0.0001f)
        {
            settingDrafts["float:" + label] = value.ToString("0.###", CultureInfo.InvariantCulture);
            return;
        }

        value = parsed;
        settingDrafts["float:" + label] = value.ToString("0.###", CultureInfo.InvariantCulture);
        captureSettingsDirty = true;
    }

    void DrawRangeSetting(string label, ref Vector2 range, float min, float max)
    {
        GUILayout.Label(label);
        GUILayout.BeginHorizontal();
        GUILayout.Label("최소", GUILayout.Width(36f));
        bool minCommit = DrawDeferredTextField(
            "range:" + label + ":min",
            range.x.ToString("0.###", CultureInfo.InvariantCulture),
            out string minText);
        GUILayout.Label("최대", GUILayout.Width(36f));
        bool maxCommit = DrawDeferredTextField(
            "range:" + label + ":max",
            range.y.ToString("0.###", CultureInfo.InvariantCulture),
            out string maxText);
        GUILayout.EndHorizontal();

        if (!minCommit && !maxCommit)
            return;

        bool changed = false;
        if (minCommit)
        {
            float parsedMin;
            if (float.TryParse(minText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedMin))
            {
                parsedMin = Mathf.Clamp(parsedMin, min, max);
                if (Mathf.Abs(parsedMin - range.x) > 0.0001f)
                {
                    range.x = parsedMin;
                    changed = true;
                }
            }
        }

        if (maxCommit)
        {
            float parsedMax;
            if (float.TryParse(maxText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedMax))
            {
                parsedMax = Mathf.Clamp(parsedMax, min, max);
                if (Mathf.Abs(parsedMax - range.y) > 0.0001f)
                {
                    range.y = parsedMax;
                    changed = true;
                }
            }
        }

        if (range.y < range.x)
        {
            if (minCommit)
                range.y = range.x;
            else
                range.x = range.y;
            changed = true;
        }

        settingDrafts["range:" + label + ":min"] = range.x.ToString("0.###", CultureInfo.InvariantCulture);
        settingDrafts["range:" + label + ":max"] = range.y.ToString("0.###", CultureInfo.InvariantCulture);

        if (changed)
            captureSettingsDirty = true;
    }

    bool DrawDeferredTextField(string id, string committedDisplay, out string text)
    {
        if (!settingDrafts.ContainsKey(id))
            settingDrafts[id] = committedDisplay;

        bool wasFocused = lastFocusedSetting == id;
        GUI.SetNextControlName(id);
        settingDrafts[id] = GUILayout.TextField(settingDrafts[id]);
        text = settingDrafts[id];

        bool focused = GUI.GetNameOfFocusedControl() == id;
        if (!focused && !wasFocused)
        {
            settingDrafts[id] = committedDisplay;
            text = committedDisplay;
            return false;
        }

        bool enter = focused &&
            Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
        if (enter)
            Event.current.Use();

        return enter || (wasFocused && !focused);
    }

    void FixIntPair(ref int minValue, ref int maxValue, bool editedMin)
    {
        if (maxValue >= minValue)
            return;

        if (editedMin)
            maxValue = minValue;
        else
            minValue = maxValue;

        captureSettingsDirty = true;
    }

    void ApplySettingDrafts()
    {
        ApplyIntDraft("int:최소 대상 수", ref minCaptureCount, 1, 32);
        ApplyIntDraft("int:최대 대상 수", ref maxCaptureCount, 1, 32);
        if (maxCaptureCount < minCaptureCount)
            maxCaptureCount = minCaptureCount;

        ApplyFloatDraft("float:최소 간격", ref minSpacing, 0f, 50f);
        ApplyRangeDraft("카메라 거리", ref cameraDistanceRange, MinCameraDistance, 500f);
        ApplyRangeDraft("카메라 피치", ref cameraPitchRange, 0f, 89f);
        ApplyRangeDraft("카메라 요", ref cameraYawRange, -360f, 360f);
        ApplyRangeDraft("카메라 FOV", ref cameraFovRange, 10f, 120f);
        ApplyIntDraft("int:JPEG 품질", ref jpegQuality, 1, 100);
        ApplyIntDraft("int:학습(train)", ref pendingTrainCount, 0, 100000);
        ApplyIntDraft("int:검증(val)", ref pendingValCount, 0, 100000);
        ApplyIntDraft("int:테스트(test)", ref pendingTestCount, 0, 100000);
        ApplyIntDraft("int:에포크", ref trainEpochs, 1, 1000);
        ApplyIntDraft("int:배치 사이즈", ref trainBatchSize, 1, 256);
        EnsureCaptureDefaults();
    }

    void ApplyIntDraft(string id, ref int value, int min, int max)
    {
        string text;
        if (!settingDrafts.TryGetValue(id, out text))
            return;

        int parsed;
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            return;

        value = Mathf.Clamp(parsed, min, max);
        settingDrafts[id] = value.ToString();
    }

    void ApplyFloatDraft(string id, ref float value, float min, float max)
    {
        string text;
        if (!settingDrafts.TryGetValue(id, out text))
            return;

        float parsed;
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            return;

        value = Mathf.Clamp(parsed, min, max);
        settingDrafts[id] = value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    void ApplyRangeDraft(string label, ref Vector2 range, float min, float max)
    {
        ApplyFloatDraft("range:" + label + ":min", ref range.x, min, max);
        ApplyFloatDraft("range:" + label + ":max", ref range.y, min, max);
        if (range.y < range.x)
            range.y = range.x;
        settingDrafts["range:" + label + ":min"] = range.x.ToString("0.###", CultureInfo.InvariantCulture);
        settingDrafts["range:" + label + ":max"] = range.y.ToString("0.###", CultureInfo.InvariantCulture);
    }

    void DrawNameDialog()
    {
        float width = 360f;
        float height = 150f;
        Rect box = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        GUI.Box(box, nameDialogIsRename ? "데이터셋 이름 변경" : "새 데이터셋");
        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 28f, box.width - 32f, box.height - 36f));
        GUILayout.Label("이름");
        GUI.SetNextControlName("DatasetNameField");
        nameDialogValue = GUILayout.TextField(nameDialogValue);

        if (!string.IsNullOrEmpty(nameDialogError))
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.4f);
            GUILayout.Label(nameDialogError);
            GUI.color = previous;
        }

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(nameDialogIsRename ? "변경" : "생성", GUILayout.Height(28f)))
            ConfirmNameDialog();

        if (GUILayout.Button("취소", GUILayout.Height(28f)))
            showNameDialog = false;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void OpenNameDialog(bool rename, string targetName = null)
    {
        nameDialogIsRename = rename;
        if (rename && !string.IsNullOrEmpty(targetName))
            selectedDataset = targetName;

        nameDialogValue = rename ? selectedDataset : "";
        nameDialogError = "";
        showNameDialog = true;
    }

    void EnterEditMode(string datasetName)
    {
        datasetName = SanitizeDatasetName(datasetName);
        if (string.IsNullOrEmpty(datasetName))
            return;

        selectedDataset = datasetName;
        editingDataset = datasetName;
        selectedSampleId = null;
        editMode = true;
        showNameDialog = false;
        showPrefabDialog = false;
        showTerrainDialog = false;
        showGenerateDialog = false;
        showTrainDialog = false;
        ClearPreview();
        EnsureCaptureDefaults();
        ResolveTerrainsRoot();
        RefreshPrefabCatalog();
        RefreshTerrainCatalog();
        LoadCaptureSettings();
        WriteDatasetYaml(GetDatasetFolder(editingDataset));
        RefreshSampleList();
    }

    void ExitEditMode()
    {
        SaveCaptureSettings();
        editMode = false;
        editingDataset = null;
        selectedSampleId = null;
        showPrefabDialog = false;
        showTerrainDialog = false;
        showGenerateDialog = false;
        showTrainDialog = false;
        ClearPreview();
        RefreshDatasetList();
    }

    void EnsureCaptureDefaults()
    {
        if (minCaptureCount < 1)
            minCaptureCount = 1;
        if (maxCaptureCount < 1)
            maxCaptureCount = 1;
        if (cameraDistanceRange.x < MinCameraDistance)
            cameraDistanceRange.x = MinCameraDistance;
        if (cameraDistanceRange.y < cameraDistanceRange.x)
            cameraDistanceRange.y = cameraDistanceRange.x;
        if (cameraPitchRange.sqrMagnitude < 0.0001f)
            cameraPitchRange = new Vector2(15f, 75f);
        if (cameraYawRange.y <= cameraYawRange.x)
            cameraYawRange = new Vector2(0f, 360f);
        if (cameraFovRange.sqrMagnitude < 0.0001f || cameraFovRange.y < cameraFovRange.x)
            cameraFovRange = new Vector2(35f, 70f);
        if (spawnViewFill < 0.2f)
            spawnViewFill = 0.65f;
        if (pendingTrainCount < 0)
            pendingTrainCount = 0;
        if (pendingValCount < 0)
            pendingValCount = 0;
        if (pendingTestCount < 0)
            pendingTestCount = 0;
        if (trainEpochs < 1)
            trainEpochs = 30;
        if (trainBatchSize < 1)
            trainBatchSize = 32;
    }

    void ResolveTerrainsRoot()
    {
        if (terrainsRoot != null)
            return;

        GameObject root = GameObject.Find("Terrains");
        if (root != null)
            terrainsRoot = root.transform;
    }

    void RefreshPrefabCatalog()
    {
        prefabCatalog.Clear();
#if UNITY_EDITOR
        string folder = string.IsNullOrEmpty(targetPrefabsFolder) ? "Assets/Targets" : targetPrefabsFolder;
        if (UnityEditor.AssetDatabase.IsValidFolder(folder))
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                Target_Base target = prefab.GetComponent<Target_Base>();
                if (target == null)
                    target = prefab.GetComponentInChildren<Target_Base>(true);
                if (target != null && !prefabCatalog.Contains(target))
                    prefabCatalog.Add(target);
            }
        }
#endif
        prefabCatalog.Sort((a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));
    }

    void RefreshTerrainCatalog()
    {
        terrainCatalog.Clear();
        ResolveTerrainsRoot();
        if (terrainsRoot == null)
            return;

        for (int i = 0; i < terrainsRoot.childCount; i++)
        {
            Transform child = terrainsRoot.GetChild(i);
            if (child == null)
                continue;

            if (child.GetComponent<Terrain>() == null && child.GetComponent<TerrainCollider>() == null)
                continue;

            terrainCatalog.Add(child.gameObject);
        }

        terrainCatalog.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
    }

    string GetCaptureSettingsPath()
    {
        if (string.IsNullOrEmpty(editingDataset))
            return null;

        return Path.Combine(GetDatasetFolder(editingDataset), "capture_settings.json");
    }

    void LoadCaptureSettings()
    {
        includedTargets.Clear();
        includedTerrains.Clear();
        settingDrafts.Clear();

        string path = GetCaptureSettingsPath();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            captureSettingsDirty = false;
            return;
        }

        CaptureSettingsData data = JsonUtility.FromJson<CaptureSettingsData>(File.ReadAllText(path));
        if (data == null)
            return;

        minCaptureCount = Mathf.Max(1, data.minCaptureCount);
        maxCaptureCount = Mathf.Max(minCaptureCount, data.maxCaptureCount);
        minSpacing = data.minSpacing;
        if (data.spawnViewFill > 0f)
            spawnViewFill = data.spawnViewFill;
        cameraDistanceRange = new Vector2(data.cameraDistanceMin, data.cameraDistanceMax);
        cameraPitchRange = new Vector2(data.cameraPitchMin, data.cameraPitchMax);
        cameraYawRange = new Vector2(data.cameraYawMin, data.cameraYawMax);
        if (data.cameraFovMax > 0f)
            cameraFovRange = new Vector2(data.cameraFovMin, data.cameraFovMax);
        jpegQuality = Mathf.Clamp(data.jpegQuality, 1, 100);
        if (data.trainSampleCount > 0)
            pendingTrainCount = data.trainSampleCount;
        if (data.valSampleCount > 0)
            pendingValCount = data.valSampleCount;
        if (data.testSampleCount > 0)
            pendingTestCount = data.testSampleCount;
        if (data.trainEpochs > 0)
            trainEpochs = data.trainEpochs;
        if (data.trainBatchSize > 0)
            trainBatchSize = data.trainBatchSize;

        EnsureCaptureDefaults();

        if (data.targetPrefabPaths != null)
        {
            for (int i = 0; i < data.targetPrefabPaths.Length; i++)
            {
                Target_Base target = LoadTargetPrefab(data.targetPrefabPaths[i]);
                if (target != null && !includedTargets.Contains(target))
                    includedTargets.Add(target);
            }
        }

        if (data.terrainNames != null)
        {
            for (int i = 0; i < data.terrainNames.Length; i++)
            {
                GameObject terrain = FindTerrainByName(data.terrainNames[i]);
                if (terrain != null && !includedTerrains.Contains(terrain))
                    includedTerrains.Add(terrain);
            }
        }

        captureSettingsDirty = false;
    }

    void SaveCaptureSettings()
    {
        ApplySettingDrafts();
        string path = GetCaptureSettingsPath();
        captureSettingsDirty = false;
        if (string.IsNullOrEmpty(path))
            return;

        string folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        CaptureSettingsData data = new CaptureSettingsData();
        data.targetPrefabPaths = new string[includedTargets.Count];
        for (int i = 0; i < includedTargets.Count; i++)
            data.targetPrefabPaths[i] = GetPrefabAssetPath(includedTargets[i]);

        data.terrainNames = new string[includedTerrains.Count];
        for (int i = 0; i < includedTerrains.Count; i++)
            data.terrainNames[i] = includedTerrains[i] != null ? includedTerrains[i].name : "";

        data.minCaptureCount = minCaptureCount;
        data.maxCaptureCount = maxCaptureCount;
        data.minSpacing = minSpacing;
        data.spawnViewFill = spawnViewFill;
        data.cameraDistanceMin = cameraDistanceRange.x;
        data.cameraDistanceMax = cameraDistanceRange.y;
        data.cameraPitchMin = cameraPitchRange.x;
        data.cameraPitchMax = cameraPitchRange.y;
        data.cameraYawMin = cameraYawRange.x;
        data.cameraYawMax = cameraYawRange.y;
        data.cameraFovMin = cameraFovRange.x;
        data.cameraFovMax = cameraFovRange.y;
        data.jpegQuality = jpegQuality;
        data.trainSampleCount = pendingTrainCount;
        data.valSampleCount = pendingValCount;
        data.testSampleCount = pendingTestCount;
        data.trainEpochs = trainEpochs;
        data.trainBatchSize = trainBatchSize;

        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        WriteDatasetYaml(GetDatasetFolder(editingDataset));
    }

    string GetPrefabAssetPath(Target_Base target)
    {
        if (target == null)
            return "";

#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(path))
            path = UnityEditor.AssetDatabase.GetAssetPath(target.gameObject);
        if (!string.IsNullOrEmpty(path))
            return path;
#endif
        return target.gameObject.name;
    }

    Target_Base LoadTargetPrefab(string pathOrName)
    {
        if (string.IsNullOrEmpty(pathOrName))
            return null;

#if UNITY_EDITOR
        if (pathOrName.Contains("/") || pathOrName.Contains("\\"))
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(pathOrName);
            if (prefab != null)
            {
                Target_Base target = prefab.GetComponent<Target_Base>();
                if (target == null)
                    target = prefab.GetComponentInChildren<Target_Base>(true);
                if (target != null)
                    return target;
            }
        }
#endif

        for (int i = 0; i < prefabCatalog.Count; i++)
        {
            if (prefabCatalog[i] != null && prefabCatalog[i].gameObject.name == pathOrName)
                return prefabCatalog[i];
        }

        return null;
    }

    GameObject FindTerrainByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        for (int i = 0; i < terrainCatalog.Count; i++)
        {
            if (terrainCatalog[i] != null && terrainCatalog[i].name == name)
                return terrainCatalog[i];
        }

        return null;
    }

    void RefreshSampleList()
    {
        datasetSamples.Clear();
        existingTrainImages = 0;
        existingValImages = 0;
        if (string.IsNullOrEmpty(editingDataset))
            return;

        string root = GetDatasetFolder(editingDataset);
        string imagesRoot = Path.Combine(root, "images");
        string labelsRoot = Path.Combine(root, "labels");
        Dictionary<string, DatasetSample> map = new Dictionary<string, DatasetSample>();

        if (Directory.Exists(imagesRoot))
        {
            string[] imageFiles = Directory.GetFiles(imagesRoot, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < imageFiles.Length; i++)
            {
                string path = imageFiles[i];
                if (!IsImageFile(path))
                    continue;

                string id = MakeSampleId(imagesRoot, path);
                DatasetSample sample = GetOrCreateSample(map, id);
                sample.imagePath = path;

                string labelPath = Path.Combine(labelsRoot, id.Replace('/', Path.DirectorySeparatorChar) + ".txt");
                if (File.Exists(labelPath))
                    sample.labelPath = labelPath;
            }
        }

        if (Directory.Exists(labelsRoot))
        {
            string[] labelFiles = Directory.GetFiles(labelsRoot, "*.txt", SearchOption.AllDirectories);
            for (int i = 0; i < labelFiles.Length; i++)
            {
                string path = labelFiles[i];
                string id = MakeSampleId(labelsRoot, path);
                DatasetSample sample = GetOrCreateSample(map, id);
                sample.labelPath = path;

                if (string.IsNullOrEmpty(sample.imagePath))
                    sample.imagePath = FindImageForSample(imagesRoot, id);
            }
        }

        datasetSamples.AddRange(map.Values);
        datasetSamples.Sort((a, b) => string.CompareOrdinal(a.id, b.id));

        existingTrainImages = CountSplitImages("train");
        existingValImages = CountSplitImages("val");

        if (!string.IsNullOrEmpty(selectedSampleId) && !map.ContainsKey(selectedSampleId))
        {
            selectedSampleId = null;
            ClearPreview();
        }
    }

    DatasetSample GetOrCreateSample(Dictionary<string, DatasetSample> map, string id)
    {
        DatasetSample sample;
        if (map.TryGetValue(id, out sample))
            return sample;

        sample = new DatasetSample { id = id };
        map.Add(id, sample);
        return sample;
    }

    bool IsImageFile(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".jpg" || ext == ".jpeg" || ext == ".png";
    }

    string MakeSampleId(string root, string path)
    {
        string relative = MakeRelativePath(root, path);
        int ext = relative.LastIndexOf('.');
        if (ext > 0)
            relative = relative.Substring(0, ext);
        return relative.Replace('\\', '/');
    }

    string MakeRelativePath(string root, string path)
    {
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullPath = Path.GetFullPath(path);
        if (fullPath.StartsWith(fullRoot, System.StringComparison.OrdinalIgnoreCase))
            return fullPath.Substring(fullRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return Path.GetFileName(path);
    }

    string FindImageForSample(string imagesRoot, string id)
    {
        if (!Directory.Exists(imagesRoot))
            return null;

        string[] extensions = { ".jpg", ".jpeg", ".png" };
        for (int i = 0; i < extensions.Length; i++)
        {
            string candidate = Path.Combine(imagesRoot, id.Replace('/', Path.DirectorySeparatorChar) + extensions[i]);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    void SelectSample(DatasetSample sample)
    {
        if (sample == null)
            return;

        selectedSampleId = sample.id;
        LoadSamplePreview(sample);
    }

    void LoadSamplePreview(DatasetSample sample)
    {
        ClearPreview();
        if (sample == null)
            return;

        if (!string.IsNullOrEmpty(sample.imagePath) && File.Exists(sample.imagePath))
        {
            byte[] bytes = File.ReadAllBytes(sample.imagePath);
            previewTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            previewTexture.LoadImage(bytes);
        }

        ParseLabelFile(sample.labelPath);
    }

    void ParseLabelFile(string labelPath)
    {
        previewBoxes.Clear();
        if (string.IsNullOrEmpty(labelPath) || !File.Exists(labelPath))
            return;

        string[] lines = File.ReadAllLines(labelPath);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            string[] parts = line.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
                continue;

            YoloBox box;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out box.classId))
                continue;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out box.cx))
                continue;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out box.cy))
                continue;
            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out box.w))
                continue;
            if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out box.h))
                continue;

            previewBoxes.Add(box);
        }
    }

    void DrawSamplePreview(Rect area)
    {
        GUI.Box(area, "");
        Rect inner = new Rect(area.x + 6f, area.y + 6f, area.width - 12f, area.height - 12f);
        if (inner.width <= 1f || inner.height <= 1f)
            return;

        if (previewTexture == null)
        {
            GUI.Label(inner, string.IsNullOrEmpty(selectedSampleId) ? "샘플을 선택하세요." : "이미지 없음");
            return;
        }

        Rect imageRect = FitRect(inner, previewTexture.width, previewTexture.height);
        GUI.DrawTexture(imageRect, previewTexture, ScaleMode.StretchToFill);

        for (int i = 0; i < previewBoxes.Count; i++)
        {
            YoloBox box = previewBoxes[i];
            float x = (box.cx - box.w * 0.5f) * imageRect.width;
            float y = (box.cy - box.h * 0.5f) * imageRect.height;
            float w = box.w * imageRect.width;
            float h = box.h * imageRect.height;
            Rect boxRect = new Rect(imageRect.x + x, imageRect.y + y, w, h);
            Color color = PreviewBoxColors[Mathf.Abs(box.classId) % PreviewBoxColors.Length];
            DrawRectOutline(boxRect, color, 2f);

            string caption = box.classId.ToString();
            Rect labelRect = new Rect(boxRect.x, Mathf.Max(imageRect.y, boxRect.y - 16f), 80f, 16f);
            Color previous = GUI.color;
            GUI.color = color;
            GUI.Label(labelRect, caption);
            GUI.color = previous;
        }
    }

    Rect FitRect(Rect bounds, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return bounds;

        float aspect = width / (float)height;
        float boundsAspect = bounds.width / bounds.height;
        if (aspect > boundsAspect)
        {
            float h = bounds.width / aspect;
            return new Rect(bounds.x, bounds.y + (bounds.height - h) * 0.5f, bounds.width, h);
        }

        float w = bounds.height * aspect;
        return new Rect(bounds.x + (bounds.width - w) * 0.5f, bounds.y, w, bounds.height);
    }

    void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        DrawFillRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawFillRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawFillRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawFillRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    void DrawFillRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, GetPixelTexture());
        GUI.color = previous;
    }

    Texture2D GetPixelTexture()
    {
        if (pixelTexture == null)
        {
            pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixelTexture.SetPixel(0, 0, Color.white);
            pixelTexture.Apply();
        }

        return pixelTexture;
    }

    void DeleteSample(DatasetSample sample)
    {
        if (sample == null)
            return;

        DeleteAssetOrFile(sample.imagePath);
        DeleteAssetOrFile(sample.labelPath);

        if (selectedSampleId == sample.id)
        {
            selectedSampleId = null;
            ClearPreview();
        }

        RefreshSampleList();
        RefreshAssets();
    }

    void DeleteAssetOrFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

#if UNITY_EDITOR
        string assetPath = ToAssetPath(path);
        if (!string.IsNullOrEmpty(assetPath))
            UnityEditor.AssetDatabase.DeleteAsset(assetPath);
#endif

        DeleteFileIfExists(path);
        DeleteFileIfExists(path + ".meta");
    }

    string ToAssetPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return null;

        string dataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
        string path = Path.GetFullPath(fullPath).Replace('\\', '/');
        if (!path.StartsWith(dataPath, System.StringComparison.OrdinalIgnoreCase))
            return null;

        return "Assets" + path.Substring(dataPath.Length);
    }

    void ClearPreview()
    {
        previewBoxes.Clear();
        if (previewTexture == null)
            return;

        Destroy(previewTexture);
        previewTexture = null;
    }

    void ConfirmNameDialog()
    {
        string name = SanitizeDatasetName(nameDialogValue);
        if (string.IsNullOrEmpty(name))
        {
            nameDialogError = "이름을 입력하세요.";
            return;
        }

        if (nameDialogIsRename)
        {
            if (!RenameDataset(selectedDataset, name))
                return;
        }
        else if (!CreateDataset(name))
        {
            return;
        }

        showNameDialog = false;
        selectedDataset = name;
        RefreshDatasetList();
    }

    bool CreateDataset(string name)
    {
        if (DatasetExists(name))
        {
            nameDialogError = "이미 있는 이름입니다.";
            return false;
        }

        string folder = GetDatasetFolder(name);
        Directory.CreateDirectory(folder);
        CreateDirectories(folder);
        WriteDatasetYaml(folder);
        RefreshAssets();
        Debug.Log("데이터셋 생성: " + folder);
        return true;
    }

    bool RenameDataset(string from, string to)
    {
        from = SanitizeDatasetName(from);
        to = SanitizeDatasetName(to);

        if (string.IsNullOrEmpty(from))
        {
            nameDialogError = "바꿀 데이터셋을 선택하세요.";
            return false;
        }

        if (from == to)
            return true;

        if (DatasetExists(to))
        {
            nameDialogError = "이미 있는 이름입니다.";
            return false;
        }

        string fromFolder = GetDatasetFolder(from);
        string toFolder = GetDatasetFolder(to);
        string fromYaml = Path.Combine(GetDatasetsRoot(), from + ".yaml");
        string toYaml = Path.Combine(GetDatasetsRoot(), to + ".yaml");

        if (!Directory.Exists(fromFolder) && !File.Exists(fromYaml))
        {
            nameDialogError = "원본 폴더가 없습니다.";
            return false;
        }

#if UNITY_EDITOR
        string fromAsset = GetDatasetAssetPath(from);
        string toAsset = GetDatasetAssetPath(to);
        if (UnityEditor.AssetDatabase.IsValidFolder(fromAsset))
        {
            string error = UnityEditor.AssetDatabase.MoveAsset(fromAsset, toAsset);
            if (!string.IsNullOrEmpty(error))
            {
                nameDialogError = error;
                return false;
            }
        }

        if (File.Exists(fromYaml))
            UnityEditor.AssetDatabase.MoveAsset(fromAsset + ".yaml", toAsset + ".yaml");
#endif

        if (Directory.Exists(fromFolder) && !Directory.Exists(toFolder))
        {
            try
            {
                Directory.Move(fromFolder, toFolder);
            }
            catch (System.Exception e)
            {
                nameDialogError = "폴더 이름 변경 실패: " + e.Message;
                return false;
            }
        }

        if (File.Exists(fromYaml) && !File.Exists(toYaml))
        {
            try
            {
                File.Move(fromYaml, toYaml);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("yaml 이름 변경 실패: " + e.Message);
            }
        }

        if (Directory.Exists(fromFolder) && Directory.Exists(toFolder))
        {
            DeleteDirectoryIfExists(fromFolder);
            DeleteFileIfExists(fromFolder + ".meta");
        }

        DeleteFileIfExists(fromYaml);
        DeleteFileIfExists(fromYaml + ".meta");

        RefreshAssets();
        return true;
    }

    string GetDatasetAssetPath(string name)
    {
        return "Assets/" + DatasetsFolderName + "/" + name;
    }

    void DeleteDataset(string name)
    {
        name = SanitizeDatasetName(name);
        if (string.IsNullOrEmpty(name))
            return;

        string folder = GetDatasetFolder(name);
        string rootYaml = Path.Combine(GetDatasetsRoot(), name + ".yaml");

#if UNITY_EDITOR
        string assetFolder = GetDatasetAssetPath(name);
        UnityEditor.AssetDatabase.DeleteAsset(assetFolder);
        UnityEditor.AssetDatabase.DeleteAsset(assetFolder + ".yaml");
#endif

        DeleteDirectoryIfExists(folder);
        DeleteFileIfExists(rootYaml);
        DeleteFileIfExists(folder + ".meta");
        DeleteFileIfExists(rootYaml + ".meta");

        Debug.Log("데이터셋 삭제: " + name);
        if (selectedDataset == name)
            selectedDataset = null;

        RefreshDatasetList();
        RefreshAssets();
    }

    void DeleteDirectoryIfExists(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        try
        {
            ClearReadOnlyAttributes(path);
            Directory.Delete(path, true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("폴더 삭제 실패: " + path + "\n" + e.Message);
        }
    }

    void DeleteFileIfExists(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;

        try
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("파일 삭제 실패: " + path + "\n" + e.Message);
        }
    }

    void ClearReadOnlyAttributes(string path)
    {
        var directory = new DirectoryInfo(path);
        if (!directory.Exists)
            return;

        directory.Attributes = FileAttributes.Normal;
        foreach (FileSystemInfo entry in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            entry.Attributes = FileAttributes.Normal;
    }

    void RefreshDatasetList()
    {
        datasetNames.Clear();
        string root = GetDatasetsRoot();
        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);

        string[] folders = Directory.GetDirectories(root);
        for (int i = 0; i < folders.Length; i++)
            AddDatasetName(Path.GetFileName(folders[i]));

        string[] yamlFiles = Directory.GetFiles(root, "*.yaml");
        for (int i = 0; i < yamlFiles.Length; i++)
            AddDatasetName(Path.GetFileNameWithoutExtension(yamlFiles[i]));

        datasetNames.Sort();

        if (!string.IsNullOrEmpty(selectedDataset) && !datasetNames.Contains(selectedDataset))
            selectedDataset = null;

        if (string.IsNullOrEmpty(selectedDataset) && datasetNames.Count > 0)
            selectedDataset = datasetNames[0];
    }

    void AddDatasetName(string name)
    {
        name = SanitizeDatasetName(name);
        if (string.IsNullOrEmpty(name))
            return;

        if (!datasetNames.Contains(name))
            datasetNames.Add(name);
    }

    bool DatasetExists(string name)
    {
        if (Directory.Exists(GetDatasetFolder(name)))
            return true;

        return File.Exists(GetDatasetsRoot() + "/" + name + ".yaml");
    }

    string SanitizeDatasetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        name = name.Trim();
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
            name = name.Replace(invalid[i].ToString(), "");

        name = name.Replace("/", "").Replace("\\", "").Replace("..", "");
        return name.Trim();
    }

    string GetDatasetsRoot()
    {
        return Path.Combine(Application.dataPath, DatasetsFolderName);
    }

    string GetDatasetFolder(string name)
    {
        return Path.Combine(GetDatasetsRoot(), name);
    }

    IEnumerator GenerateDataset()
    {
        string datasetName = string.IsNullOrEmpty(editingDataset) ? selectedDataset : editingDataset;
        if (string.IsNullOrEmpty(datasetName))
        {
            Debug.LogWarning("데이터셋을 먼저 선택하세요.");
            generateRoutine = null;
            yield break;
        }

        selectedDataset = datasetName;
        EnsureCaptureDefaults();

        if (screenTexture == null && renderTexture != null)
        {
            screenTexture = new Texture2D(
                renderTexture.width,
                renderTexture.height,
                TextureFormat.RGB24,
                false
            );
        }

        string root = GetDatasetFolder(datasetName);
        CreateDirectories(root);
        WriteDatasetYaml(root);

        yield return CaptureSplit("train", pendingTrainCount);
        yield return CaptureSplit("val", pendingValCount);
        yield return CaptureSplit("test", pendingTestCount);

        ClearCaptureTargets();
        RefreshSampleList();
        generateRoutine = null;
        Debug.Log("Dataset generation complete!");
    }

    IEnumerator CaptureSplit(string split, int count)
    {
        for (int i = 0; i < count; i++)
        {
            RandomizeScene();
            RandomizeCaptureAnimations();
            ResolveScreenOverlaps();

            yield return null;
            ResolveScreenOverlaps();
            yield return new WaitForEndOfFrame();

            CaptureAndSave(i, split);
        }
    }

    string GetDatasetRoot()
    {
        if (string.IsNullOrEmpty(selectedDataset))
            return GetDatasetsRoot();

        return GetDatasetFolder(selectedDataset);
    }

    void CreateDirectories(string root)
    {
        if (string.IsNullOrEmpty(root))
            root = GetDatasetRoot();

        string[] splits = { "train", "val", "test" };

        foreach (string split in splits)
        {
            string imagePath = root + "/images/" + split;
            string labelPath = root + "/labels/" + split;

            if (!Directory.Exists(imagePath))
                Directory.CreateDirectory(imagePath);

            if (!Directory.Exists(labelPath))
                Directory.CreateDirectory(labelPath);
        }
    }

    void WriteDatasetYaml(string root)
    {
        if (!writeDatasetYaml)
            return;

        if (string.IsNullOrEmpty(root))
            root = GetDatasetRoot();

        Dictionary<int, string> names = BuildClassNames();

        int classCount = names.Count;
        string datasetRoot = Path.GetFullPath(root).Replace('\\', '/');
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("path: " + FormatYamlScalar(datasetRoot));
        builder.AppendLine("train: images/train");
        builder.AppendLine("val: images/val");
        builder.AppendLine("test: images/test");
        builder.AppendLine();
        builder.AppendLine("nc: " + classCount);
        builder.AppendLine("names:");

        for (int classId = 0; classId < classCount; classId++)
        {
            string label;
            if (!names.TryGetValue(classId, out label) || string.IsNullOrWhiteSpace(label))
                label = "class_" + classId;

            builder.Append("  ");
            builder.Append(classId);
            builder.Append(": ");
            builder.AppendLine(FormatYamlScalar(label));
        }

        string yamlPath = root + "/dataset.yaml";
        File.WriteAllText(yamlPath, builder.ToString(), new UTF8Encoding(false));
        Debug.Log("Wrote dataset.yaml (" + classCount + " classes): " + yamlPath);
        RefreshAssets();
    }

    void RefreshAssets()
    {
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    Dictionary<int, string> BuildClassNames()
    {
        Dictionary<int, string> names = new Dictionary<int, string>();
        List<string> labels = GetIncludedClassLabels();
        for (int i = 0; i < labels.Count; i++)
            names.Add(i, labels[i]);

        return names;
    }

    List<string> GetIncludedClassLabels()
    {
        List<string> labels = new List<string>();
        for (int i = 0; i < includedTargets.Count; i++)
        {
            string label = GetClassLabel(includedTargets[i]);
            if (string.IsNullOrEmpty(label) || labels.Contains(label))
                continue;

            labels.Add(label);
        }

        return labels;
    }

    string GetClassLabel(Target_Base target)
    {
        if (target == null)
            return "";

        string path = GetPrefabAssetPath(target);
        if (!string.IsNullOrEmpty(path))
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(fileName))
                return fileName.Trim();
        }

        return target.gameObject.name.Replace("(Clone)", "").Trim();
    }

    int GetClassIdForTarget(Target_Base source)
    {
        string label = GetClassLabel(source);
        if (string.IsNullOrEmpty(label))
            return 0;

        List<string> labels = GetIncludedClassLabels();
        int index = labels.IndexOf(label);
        return index >= 0 ? index : 0;
    }

    bool CanGenerateSamples()
    {
        if (generateRoutine != null || waitingForTrain)
            return false;

        return HasIncludedTarget() && HasIncludedTerrain();
    }

    bool CanTrainModel()
    {
        if (generateRoutine != null || waitingForTrain)
            return false;

        return existingTrainImages > 0 && existingValImages > 0;
    }

    int CountSplitImages(string split)
    {
        int count = 0;
        string prefix = split + "/";
        for (int i = 0; i < datasetSamples.Count; i++)
        {
            DatasetSample sample = datasetSamples[i];
            if (sample == null || string.IsNullOrEmpty(sample.imagePath))
                continue;

            if (sample.id != null && sample.id.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }

    bool HasIncludedTarget()
    {
        for (int i = 0; i < includedTargets.Count; i++)
        {
            if (includedTargets[i] != null)
                return true;
        }

        return false;
    }

    bool HasIncludedTerrain()
    {
        for (int i = 0; i < includedTerrains.Count; i++)
        {
            if (includedTerrains[i] != null)
                return true;
        }

        return false;
    }

    string FormatYamlScalar(string value)
    {
        bool needsQuotes =
            value.IndexOfAny(new[] { ':', '#', ',', '[', ']', '{', '}', '&', '*', '!', '|', '>', '\'', '"', '%' }) >= 0
            || value.Contains(" ");

        if (!needsQuotes)
            return value;

        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    void HidePoolAnimals()
    {
        if (animalPool == null)
            return;
        foreach (DatasetAnimal entry in animalPool)
        {
            if (entry == null || entry.animal == null)
                continue;

            entry.animal.gameObject.SetActive(false);
        }
    }

    void RandomizeScene()
    {
        RandomizeTerrain();

        float distance = Random.Range(cameraDistanceRange.x, cameraDistanceRange.y);
        float fov = Random.Range(cameraFovRange.x, cameraFovRange.y);
        float pitch = Random.Range(cameraPitchRange.x, cameraPitchRange.y);
        float yaw = Random.Range(cameraYawRange.x, cameraYawRange.y);
        Vector3 origin = GetCaptureOrigin();
        float spawnRadius = ComputeSpawnRadius(distance, fov);
        lastCaptureOrigin = origin;
        lastSpawnRadius = spawnRadius;

        PlaceCamera(origin, distance, fov, pitch, yaw);
        SpawnCaptureTargets(origin, spawnRadius);
        RandomizeLighting();
        RandomizeFog();
    }

    void RandomizeTerrain()
    {
        if (includedTerrains.Count == 0)
            return;

        GameObject chosen = includedTerrains[Random.Range(0, includedTerrains.Count)];
        for (int i = 0; i < terrainCatalog.Count; i++)
        {
            GameObject terrain = terrainCatalog[i];
            if (terrain == null)
                continue;

            bool active = terrain == chosen;
            if (terrain.activeSelf != active)
                terrain.SetActive(active);
        }
    }

    Vector3 GetCaptureOrigin()
    {
        Terrain terrain = null;
        for (int i = 0; i < includedTerrains.Count; i++)
        {
            GameObject go = includedTerrains[i];
            if (go == null || !go.activeInHierarchy)
                continue;

            terrain = go.GetComponent<Terrain>();
            if (terrain != null)
                break;
        }

        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 pos = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            Vector3 center = pos + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            center.y = pos.y + terrain.SampleHeight(center);
            return center;
        }

        return Vector3.zero;
    }

    float ComputeSpawnRadius(float distance, float fov)
    {
        float aspect = 1f;
        if (renderTexture != null && renderTexture.height > 0)
            aspect = renderTexture.width / (float)renderTexture.height;
        else if (datasetCamera != null)
            aspect = datasetCamera.aspect;

        float vertical = fov * Mathf.Deg2Rad;
        float horizontal = 2f * Mathf.Atan(Mathf.Tan(vertical * 0.5f) * aspect);
        float halfAngle = Mathf.Min(vertical, horizontal) * 0.5f;
        float fill = Mathf.Clamp(spawnViewFill, 0.2f, 1f);
        return Mathf.Max(0.5f, distance * Mathf.Tan(halfAngle) * fill);
    }

    void SpawnCaptureTargets(Vector3 origin, float spawnRadius)
    {
        ClearCaptureTargets();

        List<Target_Base> pool = new List<Target_Base>();
        if (includedTargets.Count > 0)
        {
            for (int i = 0; i < includedTargets.Count; i++)
            {
                if (includedTargets[i] != null)
                    pool.Add(includedTargets[i]);
            }
        }
        else if (animalPool != null)
        {
            for (int i = 0; i < animalPool.Length; i++)
            {
                if (animalPool[i] != null && animalPool[i].animal != null)
                    pool.Add(animalPool[i].animal);
            }
        }

        if (pool.Count == 0)
            return;

        int minCount = Mathf.Max(1, minCaptureCount);
        int maxCount = Mathf.Max(minCount, maxCaptureCount);
        int spawnCount = Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            Target_Base source = pool[Random.Range(0, pool.Count)];
            Target_Base clone = Instantiate(source);
            clone.isSample = true;
            clone.gameObject.SetActive(true);

            if (clone.rigidbody == null)
                clone.rigidbody = clone.GetComponent<Rigidbody>();

            if (clone.rigidbody != null)
            {
                clone.rigidbody.useGravity = false;
                clone.rigidbody.linearVelocity = Vector3.zero;
                clone.rigidbody.angularVelocity = Vector3.zero;
                clone.rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }

            PrepareTargetForCapture(clone);
            PlaceTargetWithoutOverlap(clone, origin, spawnRadius);

            captureTargets.Add(clone);
            captureClassIds.Add(GetClassIdForTarget(source));
            captureAnimationStates.Add(GetAnimationStatesForTarget(source));
        }
    }

    string[] GetAnimationStatesForTarget(Target_Base source)
    {
        if (source == null || animalPool == null)
            return new[] { "Move" };

        string sourceName = source.gameObject.name.Replace("(Clone)", "").Trim();
        for (int i = 0; i < animalPool.Length; i++)
        {
            DatasetAnimal entry = animalPool[i];
            if (entry == null || entry.animal == null)
                continue;

            if (entry.animal == source || entry.animal.gameObject.name == sourceName)
            {
                if (entry.animationStates != null && entry.animationStates.Length > 0)
                    return entry.animationStates;
            }
        }

        return new[] { "Move" };
    }

    void PlaceTargetWithoutOverlap(Target_Base clone, Vector3 origin, float spawnRadius)
    {
        clone.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Vector3 fallback = origin;
        TryRaycastGround(origin.x, origin.z, origin.y, out fallback);
        clone.transform.position = fallback;

        int attempts = Mathf.Max(1, maxSpawnAttempts);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector3 candidate = FindSpawnPosition(origin, spawnRadius);
            clone.transform.position = candidate;
            clone.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Physics.SyncTransforms();
            if (!HasViewBoxOverlap(clone))
                return;

            fallback = candidate;
        }

        clone.transform.position = fallback;
        Physics.SyncTransforms();
    }

    bool HasViewBoxOverlap(Target_Base target)
    {
        Rect box;
        if (!TryGetViewBox(target, out box))
            return false;

        for (int i = 0; i < captureTargets.Count; i++)
        {
            Target_Base other = captureTargets[i];
            if (other == null || other == target)
                continue;

            Rect otherBox;
            if (!TryGetViewBox(other, out otherBox))
                continue;

            if (box.Overlaps(otherBox, true))
                return true;
        }

        return false;
    }

    bool TryGetViewBox(Target_Base target, out Rect box)
    {
        box = default;
        if (target == null || datasetCamera == null)
            return false;

        Bounds bounds;
        if (!TryGetTargetBounds(target, out bounds))
            return false;

        Vector3[] corners = new Vector3[8];
        Vector3 minB = bounds.min;
        Vector3 maxB = bounds.max;
        corners[0] = new Vector3(minB.x, minB.y, minB.z);
        corners[1] = new Vector3(maxB.x, minB.y, minB.z);
        corners[2] = new Vector3(minB.x, maxB.y, minB.z);
        corners[3] = new Vector3(maxB.x, maxB.y, minB.z);
        corners[4] = new Vector3(minB.x, minB.y, maxB.z);
        corners[5] = new Vector3(maxB.x, minB.y, maxB.z);
        corners[6] = new Vector3(minB.x, maxB.y, maxB.z);
        corners[7] = new Vector3(maxB.x, maxB.y, maxB.z);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        int visibleCorners = 0;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 viewPoint = datasetCamera.WorldToViewportPoint(corners[i]);
            if (viewPoint.z <= 0f)
                continue;

            visibleCorners++;
            min.x = Mathf.Min(min.x, viewPoint.x);
            min.y = Mathf.Min(min.y, viewPoint.y);
            max.x = Mathf.Max(max.x, viewPoint.x);
            max.y = Mathf.Max(max.y, viewPoint.y);
        }

        if (visibleCorners == 0)
            return false;

        box = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return box.width > 0.0001f && box.height > 0.0001f;
    }

    bool TryGetTargetBounds(Target_Base target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
            return false;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    void PrepareTargetForCapture(Target_Base target)
    {
        if (target == null)
            return;

        if (target.animator != null)
            target.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        SkinnedMeshRenderer[] skins = target.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] != null)
                skins[i].updateWhenOffscreen = true;
        }
    }

    void ResolveScreenOverlaps()
    {
        if (captureTargets.Count < 2 || datasetCamera == null)
            return;

        int attempts = Mathf.Max(1, maxSpawnAttempts);
        int passes = Mathf.Max(1, maxOverlapResolvePasses);

        for (int pass = 0; pass < passes; pass++)
        {
            bool anyOverlap = false;
            for (int i = 0; i < captureTargets.Count; i++)
            {
                Target_Base target = captureTargets[i];
                if (target == null)
                    continue;

                if (!HasViewBoxOverlap(target))
                    continue;

                anyOverlap = true;
                bool resolved = false;
                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    RelocateAwayFromOverlaps(target, attempt);
                    Physics.SyncTransforms();
                    if (target.animator != null)
                        target.animator.Update(0f);

                    if (!HasViewBoxOverlap(target))
                    {
                        resolved = true;
                        break;
                    }
                }

                if (!resolved)
                    anyOverlap = true;
            }

            if (!anyOverlap)
                return;
        }
    }

    void RelocateAwayFromOverlaps(Target_Base target, int attempt)
    {
        Rect box;
        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 push = Vector2.zero;
        if (TryGetViewBox(target, out box))
        {
            center = box.center;
            int count = 0;
            for (int i = 0; i < captureTargets.Count; i++)
            {
                Target_Base other = captureTargets[i];
                if (other == null || other == target)
                    continue;

                Rect otherBox;
                if (!TryGetViewBox(other, out otherBox) || !box.Overlaps(otherBox, true))
                    continue;

                Vector2 delta = box.center - otherBox.center;
                if (delta.sqrMagnitude < 0.000001f)
                    delta = Random.insideUnitCircle;
                push += delta.normalized;
                count++;
            }

            if (count > 0)
                push /= count;
        }

        if (push.sqrMagnitude < 0.0001f)
            push = Random.insideUnitCircle.normalized;

        float step = 0.08f + 0.04f * attempt;
        Vector2 newCenter = center + push.normalized * step;
        newCenter.x = Mathf.Clamp(newCenter.x, 0.12f, 0.88f);
        newCenter.y = Mathf.Clamp(newCenter.y, 0.12f, 0.88f);

        Ray ray = datasetCamera.ViewportPointToRay(new Vector3(newCenter.x, newCenter.y, 0f));
        Vector3 groundPos;
        if (TryRaycastGroundAlongRay(ray, out groundPos))
        {
            ClampToSpawnRadius(ref groundPos);
            target.transform.position = groundPos;
            return;
        }

        Vector3 right = Vector3.ProjectOnPlane(datasetCamera.transform.right, Vector3.up);
        Vector3 forward = Vector3.ProjectOnPlane(datasetCamera.transform.forward, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        right.Normalize();
        forward.Normalize();

        float worldStep = Mathf.Max(minSpacing, lastSpawnRadius * step);
        Vector3 candidate = target.transform.position + (right * push.x + forward * push.y).normalized * worldStep;
        if (TryRaycastGround(candidate.x, candidate.z, lastCaptureOrigin.y, out groundPos))
        {
            ClampToSpawnRadius(ref groundPos);
            target.transform.position = groundPos;
        }
    }

    void ClampToSpawnRadius(ref Vector3 pos)
    {
        Vector3 offset = pos - lastCaptureOrigin;
        offset.y = 0f;
        float limit = Mathf.Max(minSpacing, lastSpawnRadius);
        if (offset.magnitude <= limit)
            return;

        offset = offset.normalized * limit;
        pos = lastCaptureOrigin + offset;
        Vector3 grounded;
        if (TryRaycastGround(pos.x, pos.z, lastCaptureOrigin.y, out grounded))
            pos = grounded;
    }

    bool TryRaycastGroundAlongRay(Ray ray, out Vector3 spawnPos)
    {
        spawnPos = lastCaptureOrigin;
        int mask = GetGroundMask();
        if (Physics.Raycast(ray, out RaycastHit hit, raycastHeight * 4f, mask, QueryTriggerInteraction.Ignore))
        {
            spawnPos = hit.point + Vector3.up * spawnHeightOffset;
            return true;
        }

        return false;
    }

    Vector3 FindSpawnPosition(Vector3 origin, float spawnRadius)
    {
        Vector3 fallback = origin;
        TryRaycastGround(origin.x, origin.z, origin.y, out fallback);

        int attempts = Mathf.Max(1, maxSpawnAttempts);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            float x = origin.x + offset.x;
            float z = origin.z + offset.y;
            Vector3 candidate;
            if (!TryRaycastGround(x, z, origin.y, out candidate))
                continue;

            bool farEnough = true;
            for (int i = 0; i < captureTargets.Count; i++)
            {
                Target_Base existing = captureTargets[i];
                if (existing == null)
                    continue;

                Vector3 delta = existing.transform.position - candidate;
                delta.y = 0f;
                if (delta.magnitude < minSpacing)
                {
                    farEnough = false;
                    break;
                }
            }

            if (farEnough)
                return candidate;
        }

        return fallback;
    }

    bool TryRaycastGround(float x, float z, float originY, out Vector3 spawnPos)
    {
        spawnPos = new Vector3(x, originY, z);
        int mask = GetGroundMask();
        Vector3 rayOrigin = new Vector3(x, originY + raycastHeight, z);

        if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                raycastHeight * 2f,
                mask,
                QueryTriggerInteraction.Ignore))
        {
            spawnPos = hit.point + Vector3.up * spawnHeightOffset;
            return true;
        }

        return false;
    }

    int GetGroundMask()
    {
        if (groundMask.value != 0)
            return groundMask.value;

        int mask = Physics.DefaultRaycastLayers;
        mask &= ~LayerMask.GetMask("Enemy", "Friendly", "Turret", "Bullet", "Ignore Raycast");
        return mask;
    }

    void PlaceCamera(Vector3 lookPoint, float distance, float fov, float pitch, float yaw)
    {
        if (datasetCamera == null)
            return;

        Vector3 offset = Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distance);
        datasetCamera.transform.position = lookPoint + offset;
        datasetCamera.transform.LookAt(lookPoint);
        datasetCamera.fieldOfView = fov;
    }

    void RandomizeLighting()
    {
        if (directionalLight == null)
            return;

        float sunPitch = Random.Range(15f, 80f);
        float sunYaw = Random.Range(0f, 360f);

        directionalLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);
        directionalLight.intensity = Random.Range(1.2f, 2f);
        directionalLight.color = new Color(
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f)
        );
    }

    void RandomizeFog()
    {
        RenderSettings.fog = Random.value > 0.5f;

        RenderSettings.fogColor = new Color(
            Random.Range(0.7f, 1f),
            Random.Range(0.7f, 1f),
            Random.Range(0.7f, 1f)
        );

        RenderSettings.fogDensity = Random.Range(0.002f, 0.02f);
    }

    void RandomizeCaptureAnimations()
    {
        for (int i = 0; i < captureTargets.Count; i++)
        {
            Target_Base target = captureTargets[i];
            if (target == null || target.animator == null)
                continue;

            string[] states = captureAnimationStates[i];
            if (states == null || states.Length == 0)
                continue;

            string state = states[Random.Range(0, states.Length)];
            target.animator.Play(state, 0, Random.value);
            target.animator.Update(0f);
        }
    }

    void ClearCaptureTargets()
    {
        foreach (Target_Base target in captureTargets)
        {
            if (target != null)
                Destroy(target.gameObject);
        }

        captureTargets.Clear();
        captureClassIds.Clear();
        captureAnimationStates.Clear();
    }

    void CaptureAndSave(int index, string split)
    {
        if (captureTargets.Count == 0)
            return;

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        datasetCamera.Render();

        screenTexture.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );
        screenTexture.Apply();

        RenderTexture.active = currentRT;

        byte[] bytes = screenTexture.EncodeToJPG(jpegQuality);

        string imageName = "sample_" + index;
        string imagePath =
            GetDatasetRoot() +
            "/images/" +
            split +
            "/" +
            imageName +
            ".jpg";

        File.WriteAllBytes(imagePath, bytes);
        SaveLabels(imageName, split);
    }

    void SaveLabels(string imageName, string split)
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < captureTargets.Count; i++)
        {
            string line = GenerateYOLOBBox(captureTargets[i], captureClassIds[i]);
            if (string.IsNullOrEmpty(line))
                continue;

            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append(line);
        }

        string labelPath =
            GetDatasetRoot() +
            "/labels/" +
            split +
            "/" +
            imageName +
            ".txt";

        File.WriteAllText(labelPath, builder.ToString());
    }

    string GenerateYOLOBBox(Target_Base target, int classID)
    {
        Rect box;
        if (!TryGetViewBox(target, out box))
            return null;

        float xMin = Mathf.Clamp01(box.xMin);
        float yMin = Mathf.Clamp01(box.yMin);
        float xMax = Mathf.Clamp01(box.xMax);
        float yMax = Mathf.Clamp01(box.yMax);

        float boxWidth = xMax - xMin;
        float boxHeight = yMax - yMin;
        if (boxWidth <= 0.001f || boxHeight <= 0.001f)
            return null;

        float xCenter = (xMin + xMax) * 0.5f;
        float yCenter = 1f - (yMin + yMax) * 0.5f;

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} {1:0.######} {2:0.######} {3:0.######} {4:0.######}",
            classID,
            xCenter,
            yCenter,
            boxWidth,
            boxHeight
        );
    }
}
