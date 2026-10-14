using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public WaveDirector waveDirector;
    public float serverWaitTimeout = 180f;

    readonly List<PythonRuntime.TrainedModelInfo> trainedModels = new List<PythonRuntime.TrainedModelInfo>();
    int selectedModelIndex = -1;
    bool selectingModel = true;
    bool waitingForServer;
    string statusMessage = "";
    System.Diagnostics.Process serverProcess;
    PythonRuntime.TrainedModelInfo selectedModel;
    AIInputCapture[] captures;
    TurretPlatformPlacer turretPlacer;

    public bool IsModelPromptOpen
    {
        get { return selectingModel || waitingForServer; }
    }

    public bool IsWaitingForServer
    {
        get { return waitingForServer; }
    }

    public int SelectedModelIndex
    {
        get { return selectedModelIndex; }
    }

    public string StatusMessage
    {
        get { return statusMessage; }
    }

    public IReadOnlyList<PythonRuntime.TrainedModelInfo> TrainedModels
    {
        get { return trainedModels; }
    }

    void Awake()
    {
        if (waveDirector == null)
            waveDirector = GetComponent<WaveDirector>();
        if (waveDirector != null)
        {
            waveDirector.autoStart = false;
            waveDirector.showDebugHud = false;
        }

        turretPlacer = GetComponent<TurretPlatformPlacer>();
        if (turretPlacer != null)
            turretPlacer.enabled = false;

        captures = FindObjectsByType<AIInputCapture>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < captures.Length; i++)
        {
            if (captures[i] != null)
                captures[i].enabled = false;
        }
    }

    void Start()
    {
        RefreshTrainedModels();
        if (trainedModels.Count == 1)
            selectedModelIndex = 0;
    }

    void OnDestroy()
    {
        StopServerProcess();
    }

    public void RefreshTrainedModels()
    {
        PythonRuntime.ListTrainedModels(trainedModels);
        if (selectedModelIndex >= trainedModels.Count)
            selectedModelIndex = trainedModels.Count - 1;
    }

    public void SelectModel(int index)
    {
        if (index < 0 || index >= trainedModels.Count)
            return;
        selectedModelIndex = index;
    }

    public void ApplySelectedModel()
    {
        if (selectedModelIndex < 0 || selectedModelIndex >= trainedModels.Count)
        {
            statusMessage = "데이터셋을 선택하세요.";
            return;
        }

        PythonRuntime.TrainedModelInfo model = trainedModels[selectedModelIndex];
        selectedModel = model;
        System.Diagnostics.Process process;
        string error;
        if (!PythonRuntime.TryStartYoloServer(model.WeightsPath, out process, out error))
        {
            statusMessage = error;
            return;
        }

        serverProcess = process;
        waitingForServer = true;
        statusMessage = "모델: " + model.DatasetName + " / " + model.WeightsFile;
        StartCoroutine(WaitForServerThenStart());
    }

    IEnumerator WaitForServerThenStart()
    {
        string healthUrl = PythonRuntime.GetServerHealthUrl();
        float deadline = Time.realtimeSinceStartup + Mathf.Max(10f, serverWaitTimeout);

        while (Time.realtimeSinceStartup < deadline)
        {
            if (serverProcess != null)
            {
                bool exited = false;
                try { exited = serverProcess.HasExited; }
                catch (System.Exception) { exited = true; }
                if (exited)
                {
                    waitingForServer = false;
                    statusMessage = "YOLO 서버 프로세스가 종료되었습니다. CMD 창의 에러를 확인하세요.";
                    yield break;
                }
            }

            UnityWebRequest request = UnityWebRequest.Get(healthUrl);
            request.timeout = 2;
            yield return request.SendWebRequest();
            bool ok = request.result == UnityWebRequest.Result.Success && request.responseCode == 200;
            request.Dispose();

            if (ok)
            {
                BeginGameplay();
                yield break;
            }

            statusMessage = "서버 준비 대기 중... " + healthUrl;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        waitingForServer = false;
        statusMessage = "서버가 제한 시간 안에 준비되지 않았습니다. CMD 창을 확인하세요.";
    }

    void BeginGameplay()
    {
        selectingModel = false;
        waitingForServer = false;
        statusMessage = "";

        ApplyClassMapFromSelectedModel();

        for (int i = 0; i < captures.Length; i++)
        {
            if (captures[i] != null)
                captures[i].enabled = true;
        }

        if (turretPlacer != null)
            turretPlacer.enabled = true;

        if (waveDirector != null)
            waveDirector.BeginStage();
    }

    void ApplyClassMapFromSelectedModel()
    {
        if (selectedModel == null)
            return;

        PythonRuntime.YoloClassMap map;
        string error;
        if (!PythonRuntime.TryLoadYoloClassMap(selectedModel.DatasetFolder, out map, out error))
        {
            Debug.LogWarning("YOLO 피아식별 맵을 읽지 못했습니다. " + error);
            return;
        }

        CCTVController[] controllers = FindObjectsByType<CCTVController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
                controllers[i].ApplyYoloClassMap(map);
        }

        Debug.Log(
            "YOLO 피아식별: 적 " + map.EnemyClassIds.Count +
            " / 아군 " + map.FriendlyClassIds.Count +
            " (" + selectedModel.DatasetName + ")"
        );
    }

    void StopServerProcess()
    {
        if (serverProcess == null)
            return;

        try
        {
            if (!serverProcess.HasExited)
            {
                var kill = new System.Diagnostics.ProcessStartInfo();
                kill.FileName = "taskkill";
                kill.Arguments = "/PID " + serverProcess.Id.ToString() + " /T /F";
                kill.CreateNoWindow = true;
                kill.UseShellExecute = false;
                System.Diagnostics.Process killer = System.Diagnostics.Process.Start(kill);
                if (killer != null)
                {
                    killer.WaitForExit(3000);
                    killer.Dispose();
                }
            }
        }
        catch (System.Exception)
        {
        }

        try { serverProcess.Dispose(); }
        catch (System.Exception) { }
        serverProcess = null;
    }
}
