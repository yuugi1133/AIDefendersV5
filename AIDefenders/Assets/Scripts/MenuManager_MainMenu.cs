using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager_MainMenu : MonoBehaviour
{
    bool showPythonMissingDialog;
    bool showVenvDialog;
    bool showSetupProgress;
    bool showSetupError;
    string setupError = "";
    string pythonExe;
    System.Diagnostics.Process setupProcess;
    PythonRuntime.TorchInstallPlan torchPlan;
    bool gpuTorchUpgrade;

    void Start()
    {
        CheckPythonEnvironment();
    }

    void Update()
    {
        if (!showSetupProgress || setupProcess == null)
            return;

        try
        {
            if (!setupProcess.HasExited)
                return;
        }
        catch (System.Exception)
        {
        }

        setupProcess.Dispose();
        setupProcess = null;
        showSetupProgress = false;
        CheckPythonEnvironment();
        if (showVenvDialog)
        {
            showSetupError = true;
            setupError = "라이브러리 설치가 끝나지 않았거나 실패했습니다. CMD 창의 메시지를 확인하세요.";
        }
    }

    void OnDestroy()
    {
        if (setupProcess == null)
            return;

        try
        {
            if (!setupProcess.HasExited)
                return;
        }
        catch (System.Exception)
        {
        }

        setupProcess.Dispose();
        setupProcess = null;
    }

    void CheckPythonEnvironment()
    {
        showPythonMissingDialog = false;
        showVenvDialog = false;
        showSetupError = false;

        string version;
        bool found = PythonRuntime.TryFindRequiredPython(out pythonExe, out version);
        if (!found)
        {
            showPythonMissingDialog = true;
            Debug.LogWarning("Python " + PythonRuntime.RequiredVersionLabel + "이(가) 설치되어 있지 않습니다.");
            return;
        }

        Debug.Log("Python " + PythonRuntime.RequiredVersionLabel + " 확인: " + pythonExe + " (" + version + ")");
        torchPlan = PythonRuntime.DetectTorchInstall();
        Debug.Log("PyTorch 설치 계획: " + torchPlan.Message);

        if (!PythonRuntime.IsVenvReady())
        {
            showVenvDialog = true;
            gpuTorchUpgrade = false;
            Debug.LogWarning("Python 가상환경 또는 YOLO 라이브러리가 아직 준비되지 않았습니다.");
            return;
        }

        Debug.Log("Python venv 확인: " + PythonRuntime.GetVenvPython());
        if (torchPlan.UseCuda && !PythonRuntime.IsTorchCudaReady())
        {
            showVenvDialog = true;
            gpuTorchUpgrade = true;
            Debug.LogWarning("NVIDIA GPU가 감지되었지만 CUDA PyTorch가 아닙니다. GPU용 torch 설치가 필요합니다.");
        }
    }

    void StartLibrarySetup()
    {
        System.Diagnostics.Process process;
        string error;
        if (!PythonRuntime.TryStartLibrarySetup(pythonExe, out process, out error))
        {
            showSetupError = true;
            setupError = error;
            return;
        }

        setupProcess = process;
        showSetupProgress = true;
        showSetupError = false;
        setupError = "";
    }

    void OnGUI()
    {
        if (showSetupProgress)
        {
            DrawMessageDialog(
                "라이브러리 설치",
                480f,
                160f,
                DrawSetupProgress);
            return;
        }

        if (showPythonMissingDialog)
        {
            DrawMessageDialog(
                "Python " + PythonRuntime.RequiredVersionLabel + " 필요",
                520f,
                260f,
                DrawPythonMissing);
            return;
        }

        if (showVenvDialog)
        {
            DrawMessageDialog(
                gpuTorchUpgrade ? "GPU용 PyTorch 설치" : "라이브러리 설치 필요",
                560f,
                300f,
                DrawVenvMissing);
        }
    }

    void DrawMessageDialog(string title, float width, float height, System.Action drawBody)
    {
        Rect box = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        GUI.Box(box, title);
        GUILayout.BeginArea(new Rect(box.x + 16f, box.y + 28f, box.width - 32f, box.height - 36f));
        drawBody();
        GUILayout.EndArea();
    }

    void DrawPythonMissing()
    {
        GUILayout.Label("이 게임의 YOLO 추론/학습은 Python " + PythonRuntime.RequiredVersionLabel + "이 필요합니다.");
        GUILayout.Space(8f);
        GUILayout.Label("공식 사이트에서 Windows 설치 파일을 받아 설치하세요.");
        GUILayout.Label("설치 첫 화면에서 \"Add python.exe to PATH\" 를 반드시 체크하세요.");
        GUILayout.Label("(환경 변수 / PATH에 추가)");
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("공식 설치 페이지 열기", GUILayout.Height(32f)))
            PythonRuntime.OpenOfficialDownloadPage();

        if (GUILayout.Button("다시 확인", GUILayout.Height(32f)))
            CheckPythonEnvironment();
        GUILayout.EndHorizontal();

        if (GUILayout.Button("닫기", GUILayout.Height(26f)))
            showPythonMissingDialog = false;
    }

    void DrawVenvMissing()
    {
        GUILayout.Label("Python " + PythonRuntime.RequiredVersionLabel + "은 확인되었습니다.");
        if (gpuTorchUpgrade)
        {
            GUILayout.Label("가상환경은 있지만 CPU용 PyTorch가 설치되어 있습니다.");
            GUILayout.Label("NVIDIA GPU에 맞는 CUDA PyTorch를 설치합니다. 설치에 실패하면 CPU용으로 되돌립니다.");
        }
        else
        {
            GUILayout.Label("YOLO 서버에 필요한 가상환경(.venv)과 라이브러리가 아직 없습니다.");
            GUILayout.Label("자동 설치를 누르면 가상환경을 만들고 GPU에 맞는 PyTorch와 나머지 패키지를 설치합니다.");
        }

        if (torchPlan != null && !string.IsNullOrEmpty(torchPlan.Message))
            GUILayout.Label(torchPlan.Message);

        GUILayout.Label("CMD 창에서 진행 상황을 볼 수 있습니다. GPU용 torch는 용량이 커서 몇 분 걸릴 수 있습니다.");

        if (showSetupError && !string.IsNullOrEmpty(setupError))
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.4f);
            GUILayout.Label(setupError);
            GUI.color = previous;
        }

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(gpuTorchUpgrade ? "GPU용 Torch 설치" : "자동 설치", GUILayout.Height(32f)))
            StartLibrarySetup();

        if (GUILayout.Button("다시 확인", GUILayout.Height(32f)))
            CheckPythonEnvironment();
        GUILayout.EndHorizontal();

        if (GUILayout.Button("닫기", GUILayout.Height(26f)))
            showVenvDialog = false;
    }

    void DrawSetupProgress()
    {
        GUILayout.Label("가상환경 생성 및 라이브러리 설치를 진행 중입니다.");
        GUILayout.Label("CMD 창이 닫히면 자동으로 다시 확인합니다.");
        GUILayout.FlexibleSpace();
        GUILayout.Label("설치가 실패하면 CMD 창이 멈추고 에러가 표시됩니다.");
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
