using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CampaignStageEntry
{
    public string displayName = "Stage";
    public string sceneName;
    public string description;
}

public class MenuManager_MainMenu : MonoBehaviour
{
    public CampaignStageEntry[] campaignStages;

    GameObject mainMenuPanel;
    GameObject campaignPanel;
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
        EnsureDefaultStages();
        ApplyMainMenuFonts();
        BuildCampaignPanel();
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

    public void OpenCampaign()
    {
        if (campaignPanel == null)
            BuildCampaignPanel();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (campaignPanel != null)
        {
            campaignPanel.transform.SetAsLastSibling();
            campaignPanel.SetActive(true);
        }
    }

    public void CloseCampaign()
    {
        if (campaignPanel != null)
            campaignPanel.SetActive(false);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void LoadScene(string sceneName)
    {
        SceneLoader.Load(sceneName);
    }

    void EnsureDefaultStages()
    {
        if (campaignStages != null && campaignStages.Length > 0)
            return;

        campaignStages = new[]
        {
            new CampaignStageEntry
            {
                displayName = "Grassfield 1",
                sceneName = "Grassfield_1",
                description = "초원 지대"
            }
        };
    }

    void BuildCampaignPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        Transform canvasRoot = canvas.transform;
        Transform existingMain = canvasRoot.Find("Panel");
        if (existingMain != null)
            mainMenuPanel = existingMain.gameObject;

        Transform existingCampaign = canvasRoot.Find("Panel_Campaign");
        if (existingCampaign != null)
            Destroy(existingCampaign.gameObject);

        campaignPanel = new GameObject("Panel_Campaign");
        RectTransform panelRt = campaignPanel.AddComponent<RectTransform>();
        panelRt.SetParent(canvasRoot, false);
        UiFont.Stretch(panelRt);
        campaignPanel.SetActive(false);

        Image dim = UiFont.AddImage(panelRt, "Dim", new Color(0.05f, 0.06f, 0.08f, 0.72f));
        UiFont.Stretch(dim.rectTransform);

        Image card = UiFont.AddImage(panelRt, "Card", new Color(0.16f, 0.18f, 0.22f, 0.96f));
        RectTransform cardRt = card.rectTransform;
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(720f, 540f);
        cardRt.anchoredPosition = Vector2.zero;

        Text title = UiFont.AddText(cardRt, "Title", "스테이지 선택", 36, TextAnchor.MiddleLeft, Color.white, UiType.Title);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        title.rectTransform.sizeDelta = new Vector2(-48f, 56f);

        GameObject list = new GameObject("StageList");
        RectTransform listRt = list.AddComponent<RectTransform>();
        listRt.SetParent(cardRt, false);
        listRt.anchorMin = new Vector2(0f, 0f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.offsetMin = new Vector2(28f, 96f);
        listRt.offsetMax = new Vector2(-28f, -84f);

        if (campaignStages != null)
        {
            for (int i = 0; i < campaignStages.Length; i++)
                CreateStageButton(listRt, campaignStages[i], i);
        }

        Button back = CreateTextButton(cardRt, "Button_Back", "뒤로", new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(180f, 48f));
        back.onClick.AddListener(CloseCampaign);
    }

    void CreateStageButton(Transform parent, CampaignStageEntry stage, int index)
    {
        if (stage == null)
            return;

        Image image = UiFont.AddImage(parent, string.IsNullOrEmpty(stage.displayName) ? "Stage" : stage.displayName, Color.white);
        RectTransform rt = image.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 96f);
        rt.anchoredPosition = new Vector2(0f, -index * 112f);

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        string sceneName = stage.sceneName;
        button.onClick.AddListener(() => LoadScene(sceneName));

        Text name = UiFont.AddText(rt, "Name", stage.displayName, 28, TextAnchor.MiddleLeft, new Color(0.12f, 0.13f, 0.15f, 1f), UiType.TitleMedium);
        name.rectTransform.anchorMin = new Vector2(0f, 0.45f);
        name.rectTransform.anchorMax = new Vector2(1f, 1f);
        name.rectTransform.offsetMin = new Vector2(24f, 0f);
        name.rectTransform.offsetMax = new Vector2(-24f, -8f);

        string desc = string.IsNullOrWhiteSpace(stage.description) ? stage.sceneName : stage.description;
        Text detail = UiFont.AddText(rt, "Detail", desc, 20, TextAnchor.UpperLeft, new Color(0.32f, 0.34f, 0.38f, 1f), UiType.Body);
        detail.rectTransform.anchorMin = new Vector2(0f, 0f);
        detail.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        detail.rectTransform.offsetMin = new Vector2(24f, 12f);
        detail.rectTransform.offsetMax = new Vector2(-24f, 0f);
    }

    static Button CreateTextButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        Image image = UiFont.AddImage(parent, name, Color.white);
        RectTransform rt = image.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        Text text = UiFont.AddText(rt, "Text", label, 24, TextAnchor.MiddleCenter, new Color(0.12f, 0.13f, 0.15f, 1f), UiType.BodyMedium);
        UiFont.Stretch(text.rectTransform);
        return button;
    }

    void ApplyMainMenuFonts()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            string value = text.text;
            bool title = !string.IsNullOrEmpty(value) &&
                (value.IndexOf("Campaign", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("AI Den", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("AI Def", System.StringComparison.OrdinalIgnoreCase) >= 0);
            UiFont.Apply(text, title ? UiType.Title : UiType.BodyMedium);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
