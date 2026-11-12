using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    public float spawnViewFill = 0.8f;

    [Header("Camera")]
    public Vector2 cameraDistanceRange = new Vector2(30f, 80f);
    public Vector2 cameraPitchRange = new Vector2(15f, 75f);
    public Vector2 cameraYawRange = new Vector2(0f, 360f);
    public Vector2 cameraFovRange = new Vector2(35f, 70f);
    public int maxSpawnAttempts = 40;
    public int maxOverlapResolvePasses = 8;

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
    public readonly List<string> datasetNames = new List<string>();
    public readonly List<DatasetSample> datasetSamples = new List<DatasetSample>();
    public readonly List<YoloBox> previewBoxes = new List<YoloBox>();
    Texture2D screenTexture;
    public Texture2D previewTexture;
    public string selectedDataset;
    public string editingDataset;
    public string selectedSampleId;
    [System.NonSerialized] public bool editMode;
    [System.NonSerialized] public bool showNameDialog;
    [System.NonSerialized] public bool nameDialogIsRename;
    [System.NonSerialized] public bool showPrefabDialog;
    [System.NonSerialized] public bool showTerrainDialog;
    [System.NonSerialized] public bool showGenerateDialog;
    [System.NonSerialized] public bool showManualCaptureDialog;
    [System.NonSerialized] public bool showManualSettingsUi;
    [System.NonSerialized] public bool showManualShotReview;
    [System.NonSerialized] public bool showTrainDialog;
    public bool captureSettingsDirty;
    public string nameDialogValue = "";
    public string nameDialogError = "";
    public string generateDialogError = "";
    public string manualCaptureError = "";
    public string manualCaptureStatus = "";
    public string manualSplit = "train";
    public int manualPlaceClassIndex;
    public string trainDialogError = "";
    public float manualSunYaw;
    float manualSunPitch = 50f;
    float manualDistance = 50f;
    float manualPitch = 35f;
    float manualYaw = 40f;
    float manualFov = 50f;
    float manualFlyYaw;
    float manualFlyPitch;
    Camera viewCamera;
    Camera manualCaptureCamera;
    Vector3 manualHomePos;
    Quaternion manualHomeRot;
    bool datasetCameraWasEnabled = true;
    GameObject manualGhost;
    Target_Base manualGhostSource;
    float manualGhostYaw;
    bool manualGhostCanPlace;
    int lastGhostScrollFrame = -1;
    CCTVMoveLimit manualMoveLimit;
    Bounds manualMoveBounds;
    bool hasManualMoveBounds;
    bool savedFog;
    Color savedFogColor;
    float savedFogDensity;
    FogMode savedFogMode;
    public string pendingManualSplit = "train";
    public string pendingManualName = "";
    HudDatasetUi hud;
    string returnScene = "MainMenu";
    public int pendingTrainCount = 70;
    public int pendingValCount = 20;
    public int pendingTestCount = 10;
    public int trainEpochs = 30;
    public int trainBatchSize = 32;
    int existingTrainImages;
    int existingValImages;
    Coroutine generateRoutine;
    public string generateSplit = "";
    public int generateSplitIndex;
    public int generateSplitCount;
    public int generateDone;
    public int generateTotal;
    System.Diagnostics.Process trainProcess;
    [System.NonSerialized] public bool waitingForTrain;
    [System.NonSerialized] public bool showTrainResult;
    public TrainResultData trainResult;

    [System.Serializable]
    public class TrainResultData
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
        public int epoch;
        public float trainAcc;
        public float valAcc;
        public float[] trainHistory;
        public float[] valHistory;
    }

    [System.NonSerialized] public int trainStopLevel;
    System.DateTime lastTrainResultWrite;
    float nextTrainPoll;

    public readonly List<Target_Base> includedTargets = new List<Target_Base>();
    public readonly List<Target_Base> datasetClasses = new List<Target_Base>();
    public readonly List<Target_Base> createClassTargets = new List<Target_Base>();
    public readonly List<Target_Base> prefabCatalog = new List<Target_Base>();
    public readonly List<DatasetStageCatalog.Entry> stageCatalog = new List<DatasetStageCatalog.Entry>();
    public readonly List<GameObject> includedTerrains = new List<GameObject>();
    public readonly List<GameObject> terrainCatalog = new List<GameObject>();
    public bool prefabPickForCreate;
    public const string Yolov8nModelName = "yolov8n.pt";

    const string DatasetsFolderName = "Datasets";
    const float MinCameraDistance = 8f;
    const float MaxCameraDistance = 180f;
    public static readonly Color[] PreviewBoxColors =
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
        public string[] classPrefabPaths;
    }

    public class DatasetSample
    {
        public string id;
        public string imagePath;
        public string labelPath;
    }

    public struct YoloBox
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
        ResetHudFlags();
        returnScene = SceneLoader.PreviousSceneName == "TurretLab" ? "TurretLab" : "MainMenu";

        hud = new HudDatasetUi(this);

        if (datasetCamera == null || renderTexture == null)
            return;

        screenTexture = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGB24,
            false
        );
    }

    public bool IsGenerating
    {
        get { return generateRoutine != null; }
    }

    void ResetHudFlags()
    {
        editMode = false;
        showNameDialog = false;
        showPrefabDialog = false;
        showTerrainDialog = false;
        showGenerateDialog = false;
        showManualCaptureDialog = false;
        showManualSettingsUi = false;
        showManualShotReview = false;
        showTrainDialog = false;
        showTrainResult = false;
        waitingForTrain = false;
        trainStopLevel = 0;
        nameDialogIsRename = false;
        prefabPickForCreate = false;
    }

    void Update()
    {
        if (waitingForTrain)
            PollTrainingProcess();
        if (hud == null)
        {
            if (showManualCaptureDialog)
                CloseManualCapture();
            ResetHudFlags();
            hud = new HudDatasetUi(this);
        }
        TickManualCapture();
        hud.Tick();
        if (captureSettingsDirty)
            SaveCaptureSettings();
    }

    void OnDestroy()
    {
        if (showManualCaptureDialog)
            CloseManualCapture();
        ClearPreview();
    }

    public void ConfirmStartTraining()
    {
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
        string stopPath = GetTrainStopPath(datasetName);
        string weightsDir = Path.Combine(datasetFolder, "weights");
        Directory.CreateDirectory(weightsDir);
        WriteDatasetYaml(datasetFolder);
        DeleteFileIfExists(stopPath);
        trainStopLevel = 0;
        lastTrainResultWrite = default(System.DateTime);
        nextTrainPoll = 0f;
        trainResult = new TrainResultData();
        trainResult.status = "running";
        trainResult.message = "학습하는 중";
        trainResult.dataset = datasetName;
        trainResult.epochs = trainEpochs;
        trainResult.batch = trainBatchSize;
        trainResult.epoch = 0;
        trainResult.trainHistory = new float[0];
        trainResult.valHistory = new float[0];
        File.WriteAllText(resultPath, JsonUtility.ToJson(trainResult, true));

        System.Diagnostics.Process process;
        string error;
        if (!PythonRuntime.TryStartYoloTraining(
            yamlPath,
            Yolov8nModelName,
            trainEpochs,
            trainBatchSize,
            datasetName,
            resultPath,
            weightsDir,
            stopPath,
            out process,
            out error))
        {
            trainDialogError = error;
            showTrainDialog = true;
            waitingForTrain = false;
            return;
        }

        trainProcess = process;
        waitingForTrain = true;
        showTrainResult = false;
        Debug.Log("YOLO 학습 시작: " + PythonRuntime.GetVenvPython() + " " + yamlPath);
    }

    public void RequestTrainStop()
    {
        if (!waitingForTrain)
            return;

        if (trainStopLevel < 1)
        {
            trainStopLevel = 1;
            string datasetName = string.IsNullOrEmpty(editingDataset) ? selectedDataset : editingDataset;
            string stopPath = GetTrainStopPath(datasetName);
            try
            {
                File.WriteAllText(stopPath, "stop");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("학습 중지 플래그를 쓰지 못했습니다: " + e.Message);
            }

            if (trainResult != null)
            {
                trainResult.status = "stopping";
                trainResult.message = "다음 에포크에서 중지합니다.";
            }
            return;
        }

        trainStopLevel = 2;
        KillTrainProcess();
        if (trainResult != null)
        {
            trainResult.status = "stopped";
            trainResult.message = "학습을 강제 중지했습니다.";
        }
    }

    string GetTrainStopPath(string datasetName)
    {
        if (string.IsNullOrEmpty(datasetName))
            datasetName = editingDataset;
        if (string.IsNullOrEmpty(datasetName))
            datasetName = selectedDataset;
        return Path.Combine(GetDatasetFolder(datasetName), "train_stop.txt");
    }

    string GetTrainResultPath()
    {
        string datasetName = string.IsNullOrEmpty(editingDataset) ? selectedDataset : editingDataset;
        if (string.IsNullOrEmpty(datasetName))
            return "";
        return Path.Combine(GetDatasetFolder(datasetName), "train_result.json");
    }

    void KillTrainProcess()
    {
        if (trainProcess == null)
            return;
        try
        {
            int pid = trainProcess.Id;
            System.Diagnostics.Process killer = System.Diagnostics.Process.Start(
                "taskkill",
                "/PID " + pid.ToString(CultureInfo.InvariantCulture) + " /T /F");
            if (killer != null)
                killer.WaitForExit(4000);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("학습 프로세스 종료 실패: " + e.Message);
            try
            {
                if (!trainProcess.HasExited)
                    trainProcess.Kill();
            }
            catch (System.Exception)
            {
            }
        }
    }

    void PollTrainingProcess()
    {
        ReadTrainProgress();
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

    void ReadTrainProgress()
    {
        if (Time.unscaledTime < nextTrainPoll)
            return;
        nextTrainPoll = Time.unscaledTime + 0.4f;

        string resultPath = GetTrainResultPath();
        if (string.IsNullOrEmpty(resultPath) || !File.Exists(resultPath))
            return;

        try
        {
            System.DateTime write = File.GetLastWriteTimeUtc(resultPath);
            if (write == lastTrainResultWrite && trainResult != null)
                return;
            lastTrainResultWrite = write;
            TrainResultData parsed = JsonUtility.FromJson<TrainResultData>(File.ReadAllText(resultPath));
            if (parsed != null)
                trainResult = parsed;
        }
        catch (System.Exception)
        {
        }
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
        DeleteFileIfExists(GetTrainStopPath(datasetName));
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
        else if (trainResult.status == "running" || trainResult.status == "stopping")
        {
            if (trainStopLevel > 0)
            {
                trainResult.status = "stopped";
                trainResult.message = trainStopLevel > 1
                    ? "학습을 강제 중지했습니다."
                    : "학습을 중지하고 가중치를 저장했습니다.";
            }
            else
            {
                trainResult.status = "failed";
                trainResult.message = "학습이 끝났지만 완료 결과를 받지 못했습니다.";
            }
        }

        if (trainResult != null && trainResult.status != "failed")
            CopyTrainWeights(datasetName, trainResult);

        trainStopLevel = 0;
        showTrainResult = true;
        RefreshAssets();
        Debug.Log("YOLO 학습 종료: " + trainResult.status + " " + trainResult.message);
    }

    public void ConfirmGenerateCounts()
    {
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

    public void OpenNameDialog(bool rename, string targetName = null)
    {
        nameDialogIsRename = rename;
        if (rename && !string.IsNullOrEmpty(targetName))
            selectedDataset = targetName;

        nameDialogValue = rename ? selectedDataset : "";
        nameDialogError = "";
        createClassTargets.Clear();
        prefabPickForCreate = false;
        if (!rename)
        {
            RefreshPrefabCatalog();
            RefreshStageCatalog();
        }

        showNameDialog = true;
        showGenerateDialog = false;
        showManualCaptureDialog = false;
        showTrainDialog = false;
        showPrefabDialog = false;
        showTerrainDialog = false;
    }

    public void OpenPrefabPick(bool forCreate)
    {
        prefabPickForCreate = forCreate;
        RefreshPrefabCatalog();
        showPrefabDialog = true;
        if (!forCreate)
            showTerrainDialog = false;
    }

    public void AddCreateClassTarget(Target_Base target)
    {
        if (target == null || createClassTargets.Contains(target))
            return;
        createClassTargets.Add(target);
    }

    public void RemoveCreateClassTarget(int index)
    {
        if (index < 0 || index >= createClassTargets.Count)
            return;
        createClassTargets.RemoveAt(index);
    }

    public void AddCreateClassesFromStage(DatasetStageCatalog.Entry stage)
    {
        List<Target_Base> fromStage = new List<Target_Base>();
        DatasetStageCatalog.CollectTargets(stage, fromStage);
        for (int i = 0; i < fromStage.Count; i++)
            AddCreateClassTarget(fromStage[i]);
    }

    public void RefreshStageCatalog()
    {
        stageCatalog.Clear();
        for (int i = 0; i < DatasetStageCatalog.Stages.Length; i++)
        {
            DatasetStageCatalog.Entry entry = DatasetStageCatalog.Stages[i];
            if (entry == null || DatasetStageCatalog.LoadStageData(entry) == null)
                continue;
            stageCatalog.Add(entry);
        }
    }

    public bool IsDatasetClass(Target_Base target)
    {
        return target != null && datasetClasses.Contains(target);
    }

    public void EnterEditMode(string datasetName)
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
        showManualCaptureDialog = false;
        showTrainDialog = false;
        ClearPreview();
        EnsureCaptureDefaults();
        ResolveTerrainsRoot();
        RefreshPrefabCatalog();
        RefreshTerrainCatalog();
        LoadCaptureSettings();
        RefreshAutoCaptureFraming();
        WriteDatasetYaml(GetDatasetFolder(editingDataset));
        RefreshSampleList();
    }

    public void GoBack()
    {
        if (waitingForTrain || generateRoutine != null)
            return;
        if (showNameDialog || showPrefabDialog || showTerrainDialog || showGenerateDialog || showTrainDialog || showTrainResult)
            return;
        if (showManualCaptureDialog)
        {
            CloseManualCapture();
            return;
        }
        if (editMode)
        {
            ExitEditMode();
            return;
        }
        SceneLoader.Load(string.IsNullOrEmpty(returnScene) ? "MainMenu" : returnScene);
    }

    public void ExitEditMode()
    {
        SaveCaptureSettings();
        editMode = false;
        editingDataset = null;
        selectedSampleId = null;
        showPrefabDialog = false;
        showTerrainDialog = false;
        showGenerateDialog = false;
        showTrainDialog = false;
        if (showManualCaptureDialog)
            CloseManualCapture();
        ClearCaptureTargets();
        ClearPreview();
        RefreshDatasetList();
    }

    void EnsureCaptureDefaults()
    {
        if (minCaptureCount < 1)
            minCaptureCount = 1;
        if (maxCaptureCount < 1)
            maxCaptureCount = 1;
        ApplyInternalCameraDefaults();
        if (cameraDistanceRange.x < MinCameraDistance)
            cameraDistanceRange.x = MinCameraDistance;
        if (cameraDistanceRange.y < cameraDistanceRange.x)
            cameraDistanceRange.y = cameraDistanceRange.x;
        if (spawnViewFill < 0.55f)
            spawnViewFill = 0.8f;
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

    void ApplyInternalCameraDefaults()
    {
        cameraPitchRange = new Vector2(18f, 62f);
        cameraYawRange = new Vector2(0f, 360f);
        cameraFovRange = new Vector2(40f, 62f);
    }

    public void RefreshAutoCaptureFraming()
    {
        ApplyInternalCameraDefaults();

        float smallest = float.MaxValue;
        float largest = 0f;
        float typicalSum = 0f;
        int counted = 0;
        for (int i = 0; i < includedTargets.Count; i++)
        {
            float size = MeasureTargetSize(includedTargets[i]);
            if (size < 0.05f)
                continue;

            smallest = Mathf.Min(smallest, size);
            largest = Mathf.Max(largest, size);
            typicalSum += size;
            counted++;
        }

        if (counted == 0)
            return;

        float typical = typicalSum / counted;
        float spacing = Mathf.Clamp(typical * 1.45f, 2f, 28f);
        spawnViewFill = 0.8f;
        int crowd = Mathf.Max(1, maxCaptureCount);
        float packRadius = 0.9f * typical * Mathf.Sqrt(crowd);
        packRadius += spacing * Mathf.Max(0f, Mathf.Sqrt(crowd) - 1f) * 0.75f;

        float wideFov = cameraFovRange.y;
        float narrowFov = cameraFovRange.x;
        float closeLargest = DistanceForViewFill(largest, wideFov, 0.52f);
        float farSmallest = DistanceForViewFill(smallest, narrowFov, 0.11f);
        float fill = Mathf.Clamp(spawnViewFill, 0.55f, 1f);
        float packDist = packRadius / Mathf.Max(0.001f, Mathf.Tan(0.5f * wideFov * Mathf.Deg2Rad) * fill);

        float minD = Mathf.Max(MinCameraDistance, closeLargest, packDist);
        float maxD = Mathf.Max(minD * 1.45f, farSmallest, packDist * 1.55f);
        maxD = Mathf.Min(MaxCameraDistance, maxD);
        minD = Mathf.Min(minD, maxD);

        bool changed =
            Mathf.Abs(cameraDistanceRange.x - minD) > 0.05f ||
            Mathf.Abs(cameraDistanceRange.y - maxD) > 0.05f ||
            Mathf.Abs(minSpacing - spacing) > 0.05f;

        cameraDistanceRange = new Vector2(minD, maxD);
        minSpacing = spacing;
        if (changed)
            captureSettingsDirty = true;
    }

    static float DistanceForViewFill(float worldSize, float fovDegrees, float fill)
    {
        float half = 0.5f * fovDegrees * Mathf.Deg2Rad;
        fill = Mathf.Max(0.04f, fill);
        return worldSize / (2f * Mathf.Tan(half) * fill);
    }

    static float MeasureTargetSize(Target_Base target)
    {
        if (target == null)
            return 2f;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        float maxDim = 0f;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer mesh = renderers[i];
            if (mesh == null || !(mesh is MeshRenderer || mesh is SkinnedMeshRenderer))
                continue;

            Bounds local = mesh.localBounds;
            Vector3 scale = mesh.transform.lossyScale;
            Vector3 size = new Vector3(
                Mathf.Abs(local.size.x * scale.x),
                Mathf.Abs(local.size.y * scale.y),
                Mathf.Abs(local.size.z * scale.z));
            maxDim = Mathf.Max(maxDim, size.x, size.y, size.z);
        }

        return maxDim > 0.05f ? maxDim : 2f;
    }

    int PickSpawnCount()
    {
        int minCount = Mathf.Max(1, minCaptureCount);
        int maxCount = Mathf.Max(minCount, maxCaptureCount);
        return Random.Range(minCount, maxCount + 1);
    }

    float PickCameraDistance(int spawnCount)
    {
        float minD = cameraDistanceRange.x;
        float maxD = Mathf.Max(minD, cameraDistanceRange.y);
        float crowd = Mathf.InverseLerp(1f, Mathf.Max(1, maxCaptureCount), spawnCount);
        float t = Mathf.Clamp01(crowd + Random.Range(-0.12f, 0.12f));
        return Mathf.Lerp(minD, maxD, t);
    }

    void ResolveTerrainsRoot()
    {
        if (terrainsRoot != null)
            return;

        GameObject root = GameObject.Find("Terrains");
        if (root != null)
            terrainsRoot = root.transform;
    }

    public void RefreshPrefabCatalog()
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
        prefabCatalog.Sort((a, b) => string.CompareOrdinal(a.HudLabel, b.HudLabel));
    }

    public void RefreshTerrainCatalog()
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
        datasetClasses.Clear();
        includedTerrains.Clear();

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

        string[] classPaths = data.classPrefabPaths != null && data.classPrefabPaths.Length > 0
            ? data.classPrefabPaths
            : data.targetPrefabPaths;
        AppendTargets(classPaths, datasetClasses);
        AppendTargets(data.targetPrefabPaths, includedTargets);
        if (datasetClasses.Count == 0 && includedTargets.Count > 0)
            datasetClasses.AddRange(includedTargets);
        FilterIncludedToDatasetClasses();
        if (includedTargets.Count == 0)
            includedTargets.AddRange(datasetClasses);

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
        RefreshAutoCaptureFraming();
    }

    public void SaveCaptureSettings()
    {
        string path = GetCaptureSettingsPath();
        captureSettingsDirty = false;
        if (string.IsNullOrEmpty(path))
            return;

        string folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        CaptureSettingsData data = new CaptureSettingsData();
        data.classPrefabPaths = PathsForTargets(datasetClasses);
        data.targetPrefabPaths = PathsForTargets(includedTargets);

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

    string[] PathsForTargets(List<Target_Base> targets)
    {
        string[] paths = new string[targets.Count];
        for (int i = 0; i < targets.Count; i++)
            paths[i] = GetPrefabAssetPath(targets[i]);
        return paths;
    }

    void AppendTargets(string[] paths, List<Target_Base> dest)
    {
        if (paths == null)
            return;
        for (int i = 0; i < paths.Length; i++)
        {
            Target_Base target = LoadTargetPrefab(paths[i]);
            if (target != null && !dest.Contains(target))
                dest.Add(target);
        }
    }

    void FilterIncludedToDatasetClasses()
    {
        if (datasetClasses.Count == 0)
            return;
        for (int i = includedTargets.Count - 1; i >= 0; i--)
        {
            if (!datasetClasses.Contains(includedTargets[i]))
                includedTargets.RemoveAt(i);
        }
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

    public void RefreshSampleList()
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

    public void HighlightSample(DatasetSample sample)
    {
        if (sample == null)
            return;
        selectedSampleId = sample.id;
    }

    public void SelectSample(DatasetSample sample)
    {
        if (sample == null)
            return;

        selectedSampleId = sample.id;
        LoadSamplePreview(sample);
    }

    public void OpenSamplePreview(DatasetSample sample)
    {
        SelectSample(sample);
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

    public void SetPreviewBoxCorner(int index, int corner, Vector2 uiUv)
    {
        if (index < 0 || index >= previewBoxes.Count)
            return;

        YoloBox box = previewBoxes[index];
        float xMin = Mathf.Clamp01(box.cx - box.w * 0.5f);
        float xMax = Mathf.Clamp01(box.cx + box.w * 0.5f);
        float yMin = Mathf.Clamp01(1f - (box.cy + box.h * 0.5f));
        float yMax = Mathf.Clamp01(1f - (box.cy - box.h * 0.5f));
        float x = Mathf.Clamp01(uiUv.x);
        float y = Mathf.Clamp01(uiUv.y);
        const float minSize = 0.008f;

        switch (corner)
        {
            case 0:
                xMin = Mathf.Min(x, xMax - minSize);
                yMin = Mathf.Min(y, yMax - minSize);
                break;
            case 1:
                xMax = Mathf.Max(x, xMin + minSize);
                yMin = Mathf.Min(y, yMax - minSize);
                break;
            case 2:
                xMin = Mathf.Min(x, xMax - minSize);
                yMax = Mathf.Max(y, yMin + minSize);
                break;
            default:
                xMax = Mathf.Max(x, xMin + minSize);
                yMax = Mathf.Max(y, yMin + minSize);
                break;
        }

        box.cx = (xMin + xMax) * 0.5f;
        box.w = Mathf.Max(minSize, xMax - xMin);
        box.cy = 1f - (yMin + yMax) * 0.5f;
        box.h = Mathf.Max(minSize, yMax - yMin);
        previewBoxes[index] = box;
    }

    public void SavePreviewBoxes()
    {
        DatasetSample sample = FindSampleById(selectedSampleId);
        if (sample == null)
            return;

        if (string.IsNullOrEmpty(sample.labelPath) && !string.IsNullOrEmpty(sample.imagePath))
        {
            string imagePath = sample.imagePath.Replace('\\', '/');
            string labelPath = imagePath
                .Replace("/images/", "/labels/")
                .Replace(".jpg", ".txt")
                .Replace(".jpeg", ".txt")
                .Replace(".png", ".txt");
            string folder = Path.GetDirectoryName(labelPath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            sample.labelPath = labelPath;
        }

        if (string.IsNullOrEmpty(sample.labelPath))
            return;

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < previewBoxes.Count; i++)
        {
            YoloBox box = previewBoxes[i];
            if (builder.Length > 0)
                builder.Append('\n');
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0} {1:0.######} {2:0.######} {3:0.######} {4:0.######}",
                box.classId,
                Mathf.Clamp01(box.cx),
                Mathf.Clamp01(box.cy),
                Mathf.Clamp01(box.w),
                Mathf.Clamp01(box.h));
        }

        File.WriteAllText(sample.labelPath, builder.ToString());
    }

    DatasetSample FindSampleById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        for (int i = 0; i < datasetSamples.Count; i++)
        {
            if (datasetSamples[i] != null && datasetSamples[i].id == id)
                return datasetSamples[i];
        }
        return null;
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

    public void DeleteSample(DatasetSample sample)
    {
        if (sample == null)
            return;
        DeleteSamples(new[] { sample });
    }

    public void DeleteAllSamples()
    {
        DeleteSamples(datasetSamples.ToArray());
    }

    public void DeleteSamples(IList<DatasetSample> samples)
    {
        if (samples == null || samples.Count == 0)
            return;

        bool clearPreview = false;
        for (int i = 0; i < samples.Count; i++)
        {
            DatasetSample sample = samples[i];
            if (sample == null)
                continue;
            DeleteAssetOrFile(sample.imagePath);
            DeleteAssetOrFile(sample.labelPath);
            if (selectedSampleId == sample.id)
                clearPreview = true;
        }

        if (clearPreview)
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

    public void ConfirmNameDialog()
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
        else
        {
            if (createClassTargets.Count == 0)
            {
                nameDialogError = "대상을 하나 이상 추가하세요.";
                return;
            }

            if (!CreateDataset(name))
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

        datasetClasses.Clear();
        datasetClasses.AddRange(createClassTargets);
        includedTargets.Clear();
        includedTargets.AddRange(datasetClasses);

        string previousEditing = editingDataset;
        editingDataset = name;
        SaveCaptureSettings();
        editingDataset = previousEditing;

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

    public void DeleteDataset(string name)
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

    void CopyTrainWeights(string datasetName, TrainResultData result)
    {
        if (string.IsNullOrEmpty(datasetName) || result == null)
            return;

        string destDir = Path.Combine(GetDatasetFolder(datasetName), "weights");
        Directory.CreateDirectory(destDir);
        string srcDir = Path.Combine(PythonRuntime.GetPythonServerDir(), "models", datasetName, "train", "weights");
        if (string.IsNullOrEmpty(result.lastWeights) || !File.Exists(result.lastWeights))
            result.lastWeights = CopyWeightIfExists(Path.Combine(srcDir, "last.pt"), destDir);
        if (string.IsNullOrEmpty(result.bestWeights) || !File.Exists(result.bestWeights))
            result.bestWeights = CopyWeightIfExists(Path.Combine(srcDir, "best.pt"), destDir);
    }

    static string CopyWeightIfExists(string src, string destDir)
    {
        if (string.IsNullOrEmpty(src) || !File.Exists(src))
            return "";
        try
        {
            string dest = Path.Combine(destDir, Path.GetFileName(src));
            File.Copy(src, dest, true);
            return dest;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("가중치 복사 실패: " + src + "\n" + e.Message);
            return src;
        }
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

    public void RefreshDatasetList()
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
            ClearGenerateProgress();
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

        generateTotal = Mathf.Max(0, pendingTrainCount) + Mathf.Max(0, pendingValCount) + Mathf.Max(0, pendingTestCount);
        generateDone = 0;
        generateSplit = "";
        generateSplitIndex = 0;
        generateSplitCount = 0;

        int captureRun = FindNextAutoCaptureRun(root);
        yield return CaptureSplit("train", pendingTrainCount, captureRun);
        yield return CaptureSplit("val", pendingValCount, captureRun);
        yield return CaptureSplit("test", pendingTestCount, captureRun);

        ClearCaptureTargets();
        RefreshSampleList();
        ClearGenerateProgress();
        Debug.Log("Dataset generation complete!");
    }

    IEnumerator CaptureSplit(string split, int count, int captureRun)
    {
        if (count <= 0)
            yield break;

        generateSplit = split;
        generateSplitCount = count;
        generateSplitIndex = 0;
        for (int i = 0; i < count; i++)
        {
            generateSplitIndex = i + 1;
            RandomizeScene();
            RandomizeCaptureAnimations();
            ResolveScreenOverlaps();

            yield return null;
            ResolveScreenOverlaps();
            yield return new WaitForEndOfFrame();

            CaptureAndSave(i, split, captureRun);
            generateDone++;
        }
    }

    void ClearGenerateProgress()
    {
        generateRoutine = null;
        generateSplit = "";
        generateSplitIndex = 0;
        generateSplitCount = 0;
        generateDone = 0;
        generateTotal = 0;
    }

    int FindNextAutoCaptureRun(string datasetFolder)
    {
        int maxRun = 0;
        string[] splits = { "train", "val", "test" };
        for (int s = 0; s < splits.Length; s++)
        {
            string imageDir = Path.Combine(datasetFolder, "images", splits[s]);
            if (!Directory.Exists(imageDir))
                continue;

            string[] files = Directory.GetFiles(imageDir, "sample_*.jpg");
            for (int i = 0; i < files.Length; i++)
            {
                int run;
                if (TryParseAutoSampleRun(Path.GetFileNameWithoutExtension(files[i]), out run) && run > maxRun)
                    maxRun = run;
            }
        }

        return maxRun + 1;
    }

    static bool TryParseAutoSampleRun(string name, out int run)
    {
        run = 0;
        if (string.IsNullOrEmpty(name) || !name.StartsWith("sample_", System.StringComparison.Ordinal))
            return false;

        string rest = name.Substring("sample_".Length);
        int separator = rest.IndexOf('_');
        if (separator < 0)
        {
            int index;
            if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
                return false;
            run = 1;
            return true;
        }

        return int.TryParse(rest.Substring(0, separator), NumberStyles.Integer, CultureInfo.InvariantCulture, out run);
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
        List<Target_Base> source = datasetClasses.Count > 0 ? datasetClasses : includedTargets;
        List<string> labels = new List<string>();
        for (int i = 0; i < source.Count; i++)
        {
            string label = GetClassLabel(source[i]);
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

    public int GetClassIdForTarget(Target_Base source)
    {
        string label = GetClassLabel(source);
        if (string.IsNullOrEmpty(label))
            return 0;

        List<string> labels = GetIncludedClassLabels();
        int index = labels.IndexOf(label);
        return index >= 0 ? index : 0;
    }

    public bool CanGenerateSamples()
    {
        if (generateRoutine != null || waitingForTrain)
            return false;

        return HasIncludedTarget() && HasIncludedTerrain();
    }

    public bool CanTrainModel()
    {
        if (generateRoutine != null || waitingForTrain)
            return false;

        return existingTrainImages > 0 && existingValImages > 0;
    }

    public bool CanManualCapture()
    {
        if (generateRoutine != null || waitingForTrain)
            return false;
        if (datasetCamera == null || renderTexture == null)
            return false;
        if (datasetClasses.Count == 0 && includedTargets.Count == 0)
            return false;
        return terrainCatalog.Count > 0 || includedTerrains.Count > 0;
    }

    public List<Target_Base> GetManualClassPool()
    {
        EnsureManualPools();
        List<Target_Base> pool = new List<Target_Base>();
        AppendUniqueTargets(datasetClasses, pool);
        if (pool.Count == 0)
            AppendUniqueTargets(includedTargets, pool);
        if (pool.Count == 0)
            AppendUniqueTargets(prefabCatalog, pool);
        if (pool.Count == 0 && animalPool != null)
        {
            for (int i = 0; i < animalPool.Length; i++)
            {
                if (animalPool[i] != null)
                    AppendUniqueTarget(animalPool[i].animal, pool);
            }
        }
        return pool;
    }

    public List<GameObject> GetManualTerrainPool()
    {
        EnsureManualPools();
        List<GameObject> pool = new List<GameObject>();
        AppendUniqueTerrains(terrainCatalog, pool);
        AppendUniqueTerrains(includedTerrains, pool);
        return pool;
    }

    void EnsureManualPools()
    {
        StripNullTargets(datasetClasses);
        StripNullTargets(includedTargets);
        if (datasetClasses.Count == 0 && includedTargets.Count == 0)
        {
            RefreshPrefabCatalog();
            LoadCaptureSettings();
            StripNullTargets(datasetClasses);
            StripNullTargets(includedTargets);
        }

        if (terrainCatalog.Count == 0)
            RefreshTerrainCatalog();
        StripNullTerrains(terrainCatalog);
        StripNullTerrains(includedTerrains);
        if (terrainCatalog.Count == 0)
            CollectSceneTerrains(terrainCatalog);
    }

    static void StripNullTargets(List<Target_Base> list)
    {
        if (list == null)
            return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null)
                list.RemoveAt(i);
        }
    }

    static void StripNullTerrains(List<GameObject> list)
    {
        if (list == null)
            return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null)
                list.RemoveAt(i);
        }
    }

    void CollectSceneTerrains(List<GameObject> dest)
    {
        ResolveTerrainsRoot();
        if (terrainsRoot != null)
        {
            for (int i = 0; i < terrainsRoot.childCount; i++)
            {
                Transform child = terrainsRoot.GetChild(i);
                if (child != null)
                    AppendUniqueTerrain(child.gameObject, dest);
            }
        }

        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            if (terrains[i] != null)
                AppendUniqueTerrain(terrains[i].gameObject, dest);
        }
    }

    static void AppendUniqueTerrain(GameObject terrain, List<GameObject> dest)
    {
        if (terrain != null && !dest.Contains(terrain))
            dest.Add(terrain);
    }

    static void AppendUniqueTargets(List<Target_Base> source, List<Target_Base> dest)
    {
        if (source == null)
            return;
        for (int i = 0; i < source.Count; i++)
            AppendUniqueTarget(source[i], dest);
    }

    static void AppendUniqueTarget(Target_Base target, List<Target_Base> dest)
    {
        if (target != null && !dest.Contains(target))
            dest.Add(target);
    }

    static void AppendUniqueTerrains(List<GameObject> source, List<GameObject> dest)
    {
        if (source == null)
            return;
        for (int i = 0; i < source.Count; i++)
        {
            GameObject terrain = source[i];
            if (terrain != null && !dest.Contains(terrain))
                dest.Add(terrain);
        }
    }

    public void OpenAutoGenerate()
    {
        SaveCaptureSettings();
        generateDialogError = "";
        showGenerateDialog = true;
        showManualCaptureDialog = false;
        showTrainDialog = false;
        showPrefabDialog = false;
        showTerrainDialog = false;
    }

    public bool IsManualDragging
    {
        get { return manualGhost != null; }
    }

    public void OpenManualCapture()
    {
        if (!CanManualCapture())
        {
            manualCaptureError = "클래스와 지형, 촬영 카메라가 있어야 합니다.";
            return;
        }

        SaveCaptureSettings();
        RefreshPrefabCatalog();
        RefreshTerrainCatalog();
        EnsureManualPools();
        HidePoolAnimals();
        ClearCaptureTargets();
        ActivateTerrain(FirstAvailableTerrain());
        Vector3 origin = GetCaptureOrigin();
        lastCaptureOrigin = origin;
        manualDistance = Mathf.Clamp(cameraDistanceRange.x + 12f, MinCameraDistance, MaxCameraDistance);
        manualPitch = 35f;
        manualYaw = 40f;
        manualFov = 50f;
        manualPlaceClassIndex = 0;
        manualSplit = "train";
        manualCaptureError = "";
        manualCaptureStatus = "WASD로 이동하고 마우스로 바라보세요. Tab으로 촬영 설정을 엽니다.";
        showManualCaptureDialog = true;
        showManualSettingsUi = true;
        showManualShotReview = false;
        showGenerateDialog = false;
        showTrainDialog = false;
        showPrefabDialog = false;
        showTerrainDialog = false;
        EnsureScreenTexture();
        SaveManualFogState();
        BeginManualView(origin);
        ReadManualSunFromLight();
        RefreshManualMoveBounds();
    }

    public void CloseManualCapture()
    {
        CancelManualGhost();
        DiscardManualShot();
        RestoreManualFogState();
        EndManualView();
        showManualCaptureDialog = false;
        showManualSettingsUi = false;
        showManualShotReview = false;
        ClearCaptureTargets();
        manualCaptureError = "";
        SetCursorLocked(false);
    }

    public void ToggleManualSettingsUi()
    {
        if (!showManualCaptureDialog || showManualShotReview)
            return;
        CancelManualGhost();
        showManualSettingsUi = !showManualSettingsUi;
    }

    public void SetManualTerrain(GameObject terrain)
    {
        if (terrain == null)
            return;
        ActivateTerrain(terrain);
        lastCaptureOrigin = GetCaptureOrigin();
        RefreshManualMoveBounds();
        ClampManualViewPosition();
    }

    public void AddRandomManualFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(
            Random.Range(0.65f, 1f),
            Random.Range(0.65f, 1f),
            Random.Range(0.7f, 1f));
        RenderSettings.fogDensity = Random.Range(0.004f, 0.028f);
        manualCaptureStatus = "안개를 추가했습니다.";
    }

    public void ResetManualFog()
    {
        RestoreManualFogState();
        manualCaptureStatus = "안개를 초기화했습니다.";
    }

    void SaveManualFogState()
    {
        savedFog = RenderSettings.fog;
        savedFogColor = RenderSettings.fogColor;
        savedFogDensity = RenderSettings.fogDensity;
        savedFogMode = RenderSettings.fogMode;
    }

    void RestoreManualFogState()
    {
        RenderSettings.fog = savedFog;
        RenderSettings.fogColor = savedFogColor;
        RenderSettings.fogDensity = savedFogDensity;
        RenderSettings.fogMode = savedFogMode;
    }

    public void SetManualSunYaw(float yaw)
    {
        manualSunYaw = Mathf.Repeat(yaw, 360f);
        if (directionalLight == null)
            return;
        directionalLight.transform.rotation = Quaternion.Euler(manualSunPitch, manualSunYaw, 0f);
    }

    public void BeginManualGhost(int classIndex)
    {
        if (!showManualCaptureDialog || !showManualSettingsUi || showManualShotReview)
            return;

        List<Target_Base> pool = GetManualClassPool();
        if (classIndex < 0 || classIndex >= pool.Count || pool[classIndex] == null)
            return;

        CancelManualGhost();
        manualPlaceClassIndex = classIndex;
        manualGhostSource = pool[classIndex];
        manualGhostYaw = 0f;
        manualGhost = Instantiate(manualGhostSource.gameObject);
        manualGhost.name = manualGhostSource.HudLabel + " (미리보기)";
        manualGhost.SetActive(true);
        Target_Base ghostTarget = manualGhost.GetComponent<Target_Base>();
        if (ghostTarget != null)
        {
            ghostTarget.enabled = false;
            ghostTarget.isSample = true;
        }

        Collider[] colliders = manualGhost.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Rigidbody body = manualGhost.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        UpdateManualGhost();
    }

    public void CancelManualGhost()
    {
        if (manualGhost != null)
            Destroy(manualGhost);
        manualGhost = null;
        manualGhostSource = null;
        manualGhostCanPlace = false;
    }

    public bool TryGetManualViewfinder(out Vector2 min, out Vector2 max)
    {
        min = new Vector2(0.18f, 0.16f);
        max = new Vector2(0.82f, 0.84f);
        Camera view = GetViewCamera();
        Camera capture = manualCaptureCamera;
        if (view == null || capture == null)
            return false;

        min = new Vector2(1f, 1f);
        max = new Vector2(0f, 0f);
        Vector3[] corners =
        {
            new Vector3(0f, 0f, 2f),
            new Vector3(1f, 0f, 2f),
            new Vector3(0f, 1f, 2f),
            new Vector3(1f, 1f, 2f)
        };
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 world = capture.ViewportToWorldPoint(corners[i]);
            Vector3 vp = view.WorldToViewportPoint(world);
            if (vp.z <= 0f)
                continue;
            min.x = Mathf.Min(min.x, vp.x);
            min.y = Mathf.Min(min.y, vp.y);
            max.x = Mathf.Max(max.x, vp.x);
            max.y = Mathf.Max(max.y, vp.y);
        }

        min.x = Mathf.Clamp01(min.x);
        min.y = Mathf.Clamp01(min.y);
        max.x = Mathf.Clamp01(max.x);
        max.y = Mathf.Clamp01(max.y);
        return max.x - min.x > 0.02f && max.y - min.y > 0.02f;
    }

    public void UndoLastManualTarget()
    {
        if (captureTargets.Count == 0)
            return;
        int last = captureTargets.Count - 1;
        if (captureTargets[last] != null)
            Destroy(captureTargets[last].gameObject);
        captureTargets.RemoveAt(last);
        if (last < captureClassIds.Count)
            captureClassIds.RemoveAt(last);
        if (last < captureAnimationStates.Count)
            captureAnimationStates.RemoveAt(last);
        manualCaptureStatus = "마지막 대상을 지웠습니다. (" + captureTargets.Count + ")";
    }

    public void CaptureManualSample()
    {
        if (captureTargets.Count == 0)
        {
            manualCaptureError = "배치된 대상이 없습니다.";
            return;
        }

        if (!RenderManualShotToPreview())
        {
            manualCaptureError = "촬영에 실패했습니다.";
            return;
        }

        FillPreviewBoxesFromCapture();
        pendingManualSplit = string.IsNullOrEmpty(manualSplit) ? "train" : manualSplit;
        pendingManualName = "manual_" + System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        showManualShotReview = true;
        showManualSettingsUi = true;
        CancelManualGhost();
        SetCursorLocked(false);
        manualCaptureError = "";
        manualCaptureStatus = "미리보기에서 박스를 조절한 뒤 저장하세요.";
    }

    public void DiscardManualShot()
    {
        showManualShotReview = false;
        pendingManualName = "";
        if (showManualCaptureDialog)
            ClearPreview();
        if (showManualCaptureDialog)
            manualCaptureStatus = "촬영을 삭제했습니다.";
    }

    public void CommitManualShot()
    {
        if (!showManualShotReview || previewTexture == null)
        {
            manualCaptureError = "저장할 촬영이 없습니다.";
            return;
        }

        string datasetName = string.IsNullOrEmpty(editingDataset) ? selectedDataset : editingDataset;
        if (!string.IsNullOrEmpty(datasetName))
        {
            selectedDataset = datasetName;
            CreateDirectories(GetDatasetFolder(datasetName));
        }

        string split = string.IsNullOrEmpty(pendingManualSplit) ? "train" : pendingManualSplit;
        string imageName = string.IsNullOrEmpty(pendingManualName)
            ? "manual_" + System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)
            : pendingManualName;
        WritePreviewTexture(imageName, split);
        WritePreviewLabels(imageName, split);
        RefreshSampleList();
        RefreshAssets();
        showManualShotReview = false;
        pendingManualName = "";
        ClearPreview();
        manualCaptureError = "";
        manualCaptureStatus = split + "/" + imageName + " 저장";
    }

    bool RenderManualShotToPreview()
    {
        Camera capture = GetLabelCamera();
        if (capture == null || renderTexture == null)
            return false;

        EnsureScreenTexture();
        if (screenTexture == null)
            return false;

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture previousTarget = capture.targetTexture;
        capture.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        capture.Render();
        screenTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenTexture.Apply();
        capture.targetTexture = previousTarget != null ? previousTarget : renderTexture;
        RenderTexture.active = currentRT;

        if (previewTexture != null)
            Destroy(previewTexture);
        previewTexture = new Texture2D(screenTexture.width, screenTexture.height, TextureFormat.RGB24, false);
        previewTexture.SetPixels(screenTexture.GetPixels());
        previewTexture.Apply(false);
        return true;
    }

    void FillPreviewBoxesFromCapture()
    {
        previewBoxes.Clear();
        for (int i = 0; i < captureTargets.Count; i++)
        {
            Rect box;
            if (!TryGetViewBox(captureTargets[i], out box))
                continue;

            float xMin = Mathf.Clamp01(box.xMin);
            float yMin = Mathf.Clamp01(box.yMin);
            float xMax = Mathf.Clamp01(box.xMax);
            float yMax = Mathf.Clamp01(box.yMax);
            float w = xMax - xMin;
            float h = yMax - yMin;
            if (w <= 0.001f || h <= 0.001f)
                continue;

            YoloBox yolo = new YoloBox();
            yolo.classId = i < captureClassIds.Count ? captureClassIds[i] : GetClassIdForTarget(captureTargets[i]);
            yolo.cx = (xMin + xMax) * 0.5f;
            yolo.cy = 1f - (yMin + yMax) * 0.5f;
            yolo.w = w;
            yolo.h = h;
            previewBoxes.Add(yolo);
        }
    }

    void WritePreviewTexture(string imageName, string split)
    {
        if (previewTexture == null)
            return;

        byte[] bytes = previewTexture.EncodeToJPG(jpegQuality);
        string imagePath = GetDatasetRoot() + "/images/" + split + "/" + imageName + ".jpg";
        string folder = Path.GetDirectoryName(imagePath);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        File.WriteAllBytes(imagePath, bytes);
    }

    void WritePreviewLabels(string imageName, string split)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < previewBoxes.Count; i++)
        {
            YoloBox box = previewBoxes[i];
            if (builder.Length > 0)
                builder.Append('\n');
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0} {1:0.######} {2:0.######} {3:0.######} {4:0.######}",
                box.classId,
                Mathf.Clamp01(box.cx),
                Mathf.Clamp01(box.cy),
                Mathf.Clamp01(box.w),
                Mathf.Clamp01(box.h));
        }

        string labelPath = GetDatasetRoot() + "/labels/" + split + "/" + imageName + ".txt";
        string folder = Path.GetDirectoryName(labelPath);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        File.WriteAllText(labelPath, builder.ToString());
    }

    void TickManualCapture()
    {
        if (!showManualCaptureDialog)
            return;

        if (WasTabPressed() && !showManualShotReview)
            ToggleManualSettingsUi();

        if (showManualShotReview)
        {
            SetCursorLocked(false);
            return;
        }

        if (IsManualDragging)
        {
            float scroll = ReadManualScroll();
            if (Mathf.Abs(scroll) > 0.0001f)
                ApplyManualGhostScroll(scroll);
            UpdateManualGhost();
            if (WasCancelPressed())
                CancelManualGhost();
            else if (WasLeftReleased())
                ConfirmManualGhost();
            SetCursorLocked(false);
            return;
        }

        bool blockFly = showManualSettingsUi;
        SetCursorLocked(!blockFly);
        if (blockFly)
            return;

        Camera view = GetViewCamera();
        if (view == null)
            return;

        ReadManualLook(out float mouseX, out float mouseY);
        manualFlyYaw += mouseX * 0.12f;
        manualFlyPitch = Mathf.Clamp(manualFlyPitch - mouseY * 0.12f, -80f, 80f);
        view.transform.rotation = Quaternion.Euler(manualFlyPitch, manualFlyYaw, 0f);

        Vector3 wish = ReadManualMoveWish();
        view.transform.position += wish * 16f * Time.deltaTime;
        ClampManualViewPosition();
    }

    void BeginManualView(Vector3 origin)
    {
        Camera view = GetViewCamera();
        if (view == null)
            return;

        if (datasetCamera != null)
        {
            datasetCameraWasEnabled = datasetCamera.enabled;
            datasetCamera.enabled = false;
        }

        manualHomePos = view.transform.position;
        manualHomeRot = view.transform.rotation;
        view.enabled = true;
        Vector3 offset = Quaternion.Euler(manualPitch, manualYaw, 0f) * new Vector3(0f, 0f, -manualDistance);
        view.transform.position = origin + offset;
        view.transform.LookAt(origin);
        Vector3 euler = view.transform.rotation.eulerAngles;
        manualFlyYaw = euler.y;
        manualFlyPitch = NormalizePitch(euler.x);
        EnsureManualCaptureCamera(view);
        SetCursorLocked(true);
    }

    void EndManualView()
    {
        CancelManualGhost();
        Camera view = GetViewCamera();
        if (view != null)
        {
            view.transform.position = manualHomePos;
            view.transform.rotation = manualHomeRot;
        }

        if (manualCaptureCamera != null)
        {
            Destroy(manualCaptureCamera.gameObject);
            manualCaptureCamera = null;
        }

        if (datasetCamera != null)
            datasetCamera.enabled = datasetCameraWasEnabled;
    }

    void EnsureManualCaptureCamera(Camera view)
    {
        if (view == null)
            return;
        if (manualCaptureCamera == null)
        {
            GameObject go = new GameObject("ManualCaptureCamera");
            go.transform.SetParent(view.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            manualCaptureCamera = go.AddComponent<Camera>();
            AudioListener extra = go.GetComponent<AudioListener>();
            if (extra != null)
                Destroy(extra);
        }

        if (datasetCamera != null)
            manualCaptureCamera.CopyFrom(datasetCamera);

        manualCaptureCamera.transform.SetParent(view.transform, false);
        manualCaptureCamera.transform.localPosition = Vector3.zero;
        manualCaptureCamera.transform.localRotation = Quaternion.identity;
        manualCaptureCamera.transform.localScale = Vector3.one;
        manualCaptureCamera.enabled = false;
        manualCaptureCamera.targetTexture = renderTexture != null ? renderTexture : datasetCamera != null ? datasetCamera.targetTexture : null;
        manualCaptureCamera.depth = view.depth - 2f;
        manualCaptureCamera.stereoTargetEye = StereoTargetEyeMask.None;
        AudioListener copiedListener = manualCaptureCamera.GetComponent<AudioListener>();
        if (copiedListener != null)
            Destroy(copiedListener);

        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData srcData = datasetCamera != null
            ? datasetCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()
            : null;
        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData dstData =
            manualCaptureCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (srcData != null && dstData != null)
        {
            dstData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
            dstData.renderPostProcessing = srcData.renderPostProcessing;
            dstData.antialiasing = srcData.antialiasing;
            dstData.antialiasingQuality = srcData.antialiasingQuality;
            dstData.dithering = srcData.dithering;
            dstData.renderShadows = srcData.renderShadows;
        }
        else if (dstData != null)
        {
            dstData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
            dstData.renderPostProcessing = false;
        }
    }

    void UpdateManualGhost()
    {
        if (manualGhost == null)
            return;

        Camera view = GetViewCamera();
        if (view == null)
        {
            manualGhost.SetActive(false);
            manualGhostCanPlace = false;
            return;
        }

        if (hud != null && hud.IsPointerOverManualUi())
        {
            manualGhost.SetActive(false);
            manualGhostCanPlace = false;
            return;
        }

        Ray ray = view.ScreenPointToRay(GetMouseScreenPosition());
        Vector3 spawnPos;
        if (!TryRaycastGroundAlongRay(ray, out spawnPos))
        {
            manualGhost.SetActive(false);
            manualGhostCanPlace = false;
            return;
        }

        manualGhost.SetActive(true);
        manualGhost.transform.position = spawnPos;
        manualGhost.transform.rotation = Quaternion.Euler(0f, manualGhostYaw, 0f);
        manualGhostCanPlace = true;
    }

    void ConfirmManualGhost()
    {
        if (!manualGhostCanPlace || manualGhost == null || manualGhostSource == null)
        {
            CancelManualGhost();
            return;
        }

        Vector3 pos = manualGhost.transform.position;
        float yaw = manualGhostYaw;
        Target_Base source = manualGhostSource;
        CancelManualGhost();
        SpawnManualTarget(source, pos, yaw);
    }

    bool SpawnManualTarget(Target_Base source, Vector3 spawnPos, float yaw)
    {
        if (source == null)
            return false;

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
        clone.transform.position = spawnPos;
        clone.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        captureTargets.Add(clone);
        captureClassIds.Add(GetClassIdForTarget(source));
        captureAnimationStates.Add(GetAnimationStatesForTarget(source));
        manualCaptureError = "";
        manualCaptureStatus = clone.HudLabel + " 배치 (" + captureTargets.Count + ")";
        return true;
    }

    Camera GetViewCamera()
    {
        if (viewCamera == null)
            viewCamera = Camera.main;
        if (viewCamera == null)
            viewCamera = datasetCamera;
        return viewCamera;
    }

    Camera GetLabelCamera()
    {
        if (showManualCaptureDialog && manualCaptureCamera != null)
            return manualCaptureCamera;
        return datasetCamera;
    }

    void ReadManualSunFromLight()
    {
        if (directionalLight == null)
            return;
        Vector3 euler = directionalLight.transform.rotation.eulerAngles;
        manualSunPitch = NormalizePitch(euler.x);
        if (manualSunPitch < 12f)
            manualSunPitch = 50f;
        manualSunYaw = euler.y;
    }

    void RefreshManualMoveBounds()
    {
        manualMoveLimit = FindFirstObjectByType<CCTVMoveLimit>();
        hasManualMoveBounds = false;
        GameObject terrainGo = null;
        for (int i = 0; i < terrainCatalog.Count; i++)
        {
            if (terrainCatalog[i] != null && terrainCatalog[i].activeInHierarchy)
            {
                terrainGo = terrainCatalog[i];
                break;
            }
        }

        if (terrainGo == null)
            return;

        Terrain terrain = terrainGo.GetComponent<Terrain>();
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 pos = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            Vector3 center = pos + new Vector3(size.x * 0.5f, 14f, size.z * 0.5f);
            manualMoveBounds = new Bounds(center, new Vector3(size.x * 0.82f, 28f, size.z * 0.82f));
            hasManualMoveBounds = true;
            return;
        }

        Renderer[] renderers = terrainGo.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        bounds.Expand(new Vector3(-bounds.size.x * 0.1f, 0f, -bounds.size.z * 0.1f));
        Vector3 c = bounds.center;
        c.y = bounds.min.y + 14f;
        manualMoveBounds = new Bounds(c, new Vector3(bounds.size.x, 28f, bounds.size.z));
        hasManualMoveBounds = true;
    }

    void ClampManualViewPosition()
    {
        Camera view = GetViewCamera();
        if (view == null)
            return;

        Vector3 pos = view.transform.position;
        Collider volume = manualMoveLimit != null ? manualMoveLimit.Volume : null;
        if (volume != null)
        {
            Vector3 closest = volume.ClosestPoint(pos);
            if ((closest - pos).sqrMagnitude > 0.0001f)
                pos = closest;
        }
        else if (hasManualMoveBounds)
        {
            pos.x = Mathf.Clamp(pos.x, manualMoveBounds.min.x, manualMoveBounds.max.x);
            pos.y = Mathf.Clamp(pos.y, manualMoveBounds.min.y, manualMoveBounds.max.y);
            pos.z = Mathf.Clamp(pos.z, manualMoveBounds.min.z, manualMoveBounds.max.z);
        }

        view.transform.position = pos;
    }

    Vector3 ReadManualMoveWish()
    {
        float x = 0f;
        float z = 0f;
        float y = 0f;
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                x -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                z += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                z -= 1f;
            if (keyboard.spaceKey.isPressed)
                y += 1f;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                y -= 1f;
        }
        else
#endif
        {
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                x -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                z -= 1f;
            if (Input.GetKey(KeyCode.Space))
                y += 1f;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                y -= 1f;
        }

        Vector3 planar = Quaternion.Euler(0f, manualFlyYaw, 0f) * new Vector3(x, 0f, z);
        planar.y = 0f;
        Vector3 wish = planar + Vector3.up * y;
        if (wish.sqrMagnitude > 1f)
            wish.Normalize();
        return wish;
    }

    void ReadManualLook(out float mouseX, out float mouseY)
    {
        mouseX = 0f;
        mouseY = 0f;
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            mouseX = delta.x;
            mouseY = delta.y;
            return;
        }
#endif
        mouseX = Input.GetAxisRaw("Mouse X") * 20f;
        mouseY = Input.GetAxisRaw("Mouse Y") * 20f;
    }

    public void ApplyManualGhostScroll(float scroll)
    {
        if (!IsManualDragging || Time.frameCount == lastGhostScrollFrame)
            return;
        float notches = NormalizeScrollNotches(scroll);
        if (Mathf.Abs(notches) < 0.01f)
            return;
        lastGhostScrollFrame = Time.frameCount;
        manualGhostYaw += notches * 18f;
        UpdateManualGhost();
    }

    float ReadManualScroll()
    {
        float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 value = mouse.scroll.ReadValue();
            scroll = Mathf.Abs(value.y) >= Mathf.Abs(value.x) ? value.y : value.x;
        }
        else
#endif
        {
            scroll = Input.mouseScrollDelta.y;
        }

        return scroll;
    }

    static float NormalizeScrollNotches(float scroll)
    {
        if (Mathf.Abs(scroll) >= 20f)
            scroll /= 120f;
        return Mathf.Clamp(scroll, -4f, 4f);
    }

    Vector3 GetMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif
        return Input.mousePosition;
    }

    bool WasTabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
            return keyboard.tabKey.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(KeyCode.Tab);
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

    static float NormalizePitch(float eulerX)
    {
        if (eulerX > 180f)
            eulerX -= 360f;
        return eulerX;
    }

    static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void EnsureScreenTexture()
    {
        if (screenTexture == null && renderTexture != null)
        {
            screenTexture = new Texture2D(
                renderTexture.width,
                renderTexture.height,
                TextureFormat.RGB24,
                false);
        }
    }

    GameObject FirstAvailableTerrain()
    {
        for (int i = 0; i < includedTerrains.Count; i++)
        {
            if (includedTerrains[i] != null)
                return includedTerrains[i];
        }

        for (int i = 0; i < terrainCatalog.Count; i++)
        {
            if (terrainCatalog[i] != null)
                return terrainCatalog[i];
        }

        return null;
    }

    void ActivateTerrain(GameObject chosen)
    {
        if (chosen == null)
            return;
        for (int i = 0; i < terrainCatalog.Count; i++)
        {
            GameObject terrain = terrainCatalog[i];
            if (terrain == null)
                continue;
            bool active = terrain == chosen;
            if (terrain.activeSelf != active)
                terrain.SetActive(active);
        }

        CombatCollision.ApplyGroundLayers();
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
        RefreshAutoCaptureFraming();

        int spawnCount = PickSpawnCount();
        float distance = PickCameraDistance(spawnCount);
        float fov = Random.Range(cameraFovRange.x, cameraFovRange.y);
        float pitch = Random.Range(cameraPitchRange.x, cameraPitchRange.y);
        float yaw = Random.Range(cameraYawRange.x, cameraYawRange.y);
        Vector3 origin = GetCaptureOrigin();
        float spawnRadius = ComputeSpawnRadius(distance, fov);
        lastCaptureOrigin = origin;
        lastSpawnRadius = spawnRadius;

        PlaceCamera(origin, distance, fov, pitch, yaw);
        SpawnCaptureTargets(origin, spawnRadius, spawnCount);
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

        CombatCollision.ApplyGroundLayers();
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
        float fill = Mathf.Clamp(spawnViewFill, 0.55f, 1f);
        return Mathf.Max(minSpacing, distance * Mathf.Tan(halfAngle) * fill);
    }

    void SpawnCaptureTargets(Vector3 origin, float spawnRadius, int count)
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

        int spawnCount = count > 0 ? count : PickSpawnCount();

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
            PlaceTargetWithoutOverlap(clone, origin, spawnRadius, i, spawnCount);

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

    void PlaceTargetWithoutOverlap(Target_Base clone, Vector3 origin, float spawnRadius, int index, int total)
    {
        clone.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Vector3 fallback = origin;
        TryRaycastGround(origin.x, origin.z, origin.y, out fallback);
        Vector3 slotted = FindSlottedSpawn(origin, spawnRadius, index, total);
        if (slotted != fallback)
            fallback = slotted;
        clone.transform.position = fallback;

        int attempts = Mathf.Max(1, maxSpawnAttempts);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector3 candidate = attempt == 0
                ? fallback
                : FindSpawnPosition(origin, spawnRadius);
            clone.transform.position = candidate;
            clone.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Physics.SyncTransforms();
            if (clone.animator != null)
                clone.animator.Update(0f);
            if (!HasViewBoxOverlap(clone) && !HasWorldOverlap(clone))
                return;

            fallback = candidate;
        }

        clone.transform.position = fallback;
        Physics.SyncTransforms();
    }

    bool HasWorldOverlap(Target_Base target)
    {
        if (target == null)
            return false;

        Vector3 pos = target.transform.position;
        float size = MeasureTargetSize(target);
        for (int i = 0; i < captureTargets.Count; i++)
        {
            Target_Base other = captureTargets[i];
            if (other == null || other == target)
                continue;

            Vector3 delta = other.transform.position - pos;
            delta.y = 0f;
            float need = Mathf.Max(minSpacing, 0.7f * (size + MeasureTargetSize(other)));
            if (delta.magnitude < need)
                return true;
        }

        return false;
    }

    bool HasViewBoxOverlap(Target_Base target)
    {
        Rect box;
        if (!TryGetViewBox(target, out box))
            return false;

        box = InflateViewBox(box, 0.03f);
        for (int i = 0; i < captureTargets.Count; i++)
        {
            Target_Base other = captureTargets[i];
            if (other == null || other == target)
                continue;

            Rect otherBox;
            if (!TryGetViewBox(other, out otherBox))
                continue;

            if (box.Overlaps(InflateViewBox(otherBox, 0.03f), true))
                return true;
        }

        return false;
    }

    static Rect InflateViewBox(Rect box, float margin)
    {
        box.xMin -= margin;
        box.yMin -= margin;
        box.xMax += margin;
        box.yMax += margin;
        return box;
    }

    bool TryGetViewBox(Target_Base target, out Rect box)
    {
        box = default;
        Camera capture = GetLabelCamera();
        if (target == null || capture == null)
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
            Vector3 viewPoint = capture.WorldToViewportPoint(corners[i]);
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
        return target != null && target.TryGetRenderBounds(out bounds);
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

        float step = 0.14f + 0.07f * attempt;
        Vector2 newCenter = center + push.normalized * step;
        newCenter.x = Mathf.Clamp(newCenter.x, 0.08f, 0.92f);
        newCenter.y = Mathf.Clamp(newCenter.y, 0.08f, 0.92f);

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

        float worldStep = Mathf.Max(minSpacing, lastSpawnRadius * Mathf.Max(0.12f, step));
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
        float limit = Mathf.Max(minSpacing * 2f, lastSpawnRadius * 1.25f);
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

    Vector3 FindSlottedSpawn(Vector3 origin, float spawnRadius, int index, int total)
    {
        Vector3 fallback = origin;
        TryRaycastGround(origin.x, origin.z, origin.y, out fallback);
        if (total < 1)
            total = 1;

        float ring = spawnRadius * (0.4f + 0.55f * ((index % 2 == 0) ? 0.55f : 1f));
        float angle = (index / (float)total) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
        float x = origin.x + Mathf.Cos(angle) * ring;
        float z = origin.z + Mathf.Sin(angle) * ring;
        Vector3 grounded;
        if (TryRaycastGround(x, z, origin.y, out grounded) && IsFarEnough(grounded, null))
            return grounded;
        return fallback;
    }

    Vector3 FindSpawnPosition(Vector3 origin, float spawnRadius)
    {
        Vector3 fallback = origin;
        TryRaycastGround(origin.x, origin.z, origin.y, out fallback);

        int attempts = Mathf.Max(1, maxSpawnAttempts);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            float radius = spawnRadius * Mathf.Sqrt(Random.value);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float x = origin.x + Mathf.Cos(angle) * radius;
            float z = origin.z + Mathf.Sin(angle) * radius;
            Vector3 candidate;
            if (!TryRaycastGround(x, z, origin.y, out candidate))
                continue;

            if (IsFarEnough(candidate, null))
                return candidate;
        }

        return fallback;
    }

    bool IsFarEnough(Vector3 candidate, Target_Base ignore)
    {
        for (int i = 0; i < captureTargets.Count; i++)
        {
            Target_Base existing = captureTargets[i];
            if (existing == null || existing == ignore)
                continue;

            Vector3 delta = existing.transform.position - candidate;
            delta.y = 0f;
            float need = Mathf.Max(minSpacing, 0.7f * (MeasureTargetSize(existing) + 1.5f));
            if (delta.magnitude < need)
                return false;
        }

        return true;
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
        return CombatCollision.GroundMaskOrOverride(groundMask);
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

    void CaptureAndSave(int index, string split, int captureRun)
    {
        CaptureAndSaveNamed(
            "sample_" + captureRun.ToString(CultureInfo.InvariantCulture) + "_" + index.ToString(CultureInfo.InvariantCulture),
            split);
    }

    void CaptureAndSaveNamed(string imageName, string split)
    {
        Camera capture = GetLabelCamera();
        if (captureTargets.Count == 0 || capture == null || renderTexture == null)
            return;

        EnsureScreenTexture();
        if (screenTexture == null)
            return;

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture previousTarget = capture.targetTexture;
        capture.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;

        capture.Render();

        screenTexture.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );
        screenTexture.Apply();

        capture.targetTexture = previousTarget;
        RenderTexture.active = currentRT;

        byte[] bytes = screenTexture.EncodeToJPG(jpegQuality);
        string imagePath =
            GetDatasetRoot() +
            "/images/" +
            split +
            "/" +
            imageName +
            ".jpg";

        string folder = Path.GetDirectoryName(imagePath);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

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
