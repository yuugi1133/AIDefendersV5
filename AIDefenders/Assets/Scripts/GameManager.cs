using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public WaveDirector waveDirector;
    public float serverWaitTimeout = 180f;

    readonly List<PythonRuntime.TrainedModelInfo> trainedModels = new List<PythonRuntime.TrainedModelInfo>();
    Vector2 modelListScroll;
    int selectedModelIndex = -1;
    bool selectingModel = true;
    bool waitingForServer;
    string statusMessage = "";
    System.Diagnostics.Process serverProcess;
    AIInputCapture[] captures;
    TurretPlatformPlacer turretPlacer;
    bool previousDebugHud;

    void Awake()
    {
        if (waveDirector == null)
            waveDirector = GetComponent<WaveDirector>();
        if (waveDirector != null)
        {
            previousDebugHud = waveDirector.showDebugHud;
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

    void RefreshTrainedModels()
    {
        PythonRuntime.ListTrainedModels(trainedModels);
        if (selectedModelIndex >= trainedModels.Count)
            selectedModelIndex = trainedModels.Count - 1;
    }

    void OnGUI()
    {
        if (!selectingModel && !waitingForServer)
            return;

        Rect box = new Rect(
            (Screen.width - 520f) * 0.5f,
            (Screen.height - 360f) * 0.5f,
            520f,
            360f
        );

        GUI.Box(box, waitingForServer ? "YOLO 서버 시작" : "YOLO 모델 선택");
        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 28f, box.width - 32f, box.height - 36f));

        if (waitingForServer)
        {
            GUILayout.Label("선택한 모델로 로컬 YOLO 서버를 실행하는 중입니다.");
            GUILayout.Label("CMD 창에서 모델 로딩 로그를 확인할 수 있습니다.");
            GUILayout.Space(8f);
            GUILayout.Label(string.IsNullOrEmpty(statusMessage) ? "서버 응답을 기다리는 중..." : statusMessage);
            GUILayout.FlexibleSpace();
            GUILayout.Label("서버가 준비되면 게임이 시작됩니다.");
            GUILayout.EndArea();
            EatClicksOutside(box);
            return;
        }

        GUILayout.Label("이 스테이지에서 사용할 학습 모델을 고르세요.");
        GUILayout.Label("학습이 끝난 데이터셋(weights/best.pt 또는 last.pt)만 표시됩니다.");
        GUILayout.Space(6f);

        modelListScroll = GUILayout.BeginScrollView(modelListScroll, GUILayout.ExpandHeight(true));
        if (trainedModels.Count == 0)
            GUILayout.Label("학습된 모델이 없습니다. DatasetGeneratorScene에서 먼저 학습하세요.");

        for (int i = 0; i < trainedModels.Count; i++)
        {
            PythonRuntime.TrainedModelInfo model = trainedModels[i];
            string label = model.DatasetName + "  (" + model.WeightsFile + ")";
            Color previous = GUI.backgroundColor;
            if (i == selectedModelIndex)
                GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);
            if (GUILayout.Button(label, GUILayout.Height(28f)))
                selectedModelIndex = i;
            GUI.backgroundColor = previous;
        }
        GUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(statusMessage))
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.4f);
            GUILayout.Label(statusMessage);
            GUI.color = previous;
        }

        GUILayout.BeginHorizontal();
        bool canApply = selectedModelIndex >= 0 && selectedModelIndex < trainedModels.Count;
        GUI.enabled = canApply;
        if (GUILayout.Button("적용하고 서버 시작", GUILayout.Height(32f)))
            ApplySelectedModel();
        GUI.enabled = true;
        if (GUILayout.Button("목록 새로고침", GUILayout.Height(32f)))
            RefreshTrainedModels();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        EatClicksOutside(box);
    }

    static void EatClicksOutside(Rect box)
    {
        Event current = Event.current;
        if (current == null || !current.isMouse)
            return;
        if (!box.Contains(current.mousePosition))
            current.Use();
    }

    void ApplySelectedModel()
    {
        if (selectedModelIndex < 0 || selectedModelIndex >= trainedModels.Count)
        {
            statusMessage = "데이터셋을 선택하세요.";
            return;
        }

        PythonRuntime.TrainedModelInfo model = trainedModels[selectedModelIndex];
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

        for (int i = 0; i < captures.Length; i++)
        {
            if (captures[i] != null)
                captures[i].enabled = true;
        }

        if (turretPlacer != null)
            turretPlacer.enabled = true;

        if (waveDirector != null)
        {
            waveDirector.showDebugHud = previousDebugHud;
            waveDirector.BeginStage();
        }
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
