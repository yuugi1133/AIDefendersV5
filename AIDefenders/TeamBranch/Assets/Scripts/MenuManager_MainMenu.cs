using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CampaignStageEntry
{
    public int chapterId = 1;
    public string chapterName = "초원";
    public string displayName = "Stage";
    public string sceneName;
    public string description;
}

public class MenuManager_MainMenu : MonoBehaviour
{
    public CampaignStageEntry[] campaignStages;
    [Tooltip("켜면 이전 스테이지를 클리어해야 다음 스테이지를 선택할 수 있습니다.")]
    public bool requireStageUnlocks;

    GameObject mainMenuPanel;
    static bool titleEntered;
    GameObject titlePanel;
    AudioSettingsUi settingsPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetTitle() { titleEntered = false; }
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
        BuildStyledMainMenu();
        if (titleEntered) CheckPythonEnvironment();
        else
        {
            mainMenuPanel.SetActive(false);
            titlePanel = TitleScreenUi.Create(UiFont.FindSceneCanvas(this).transform, EnterMainMenu);
        }
    }

    void EnterMainMenu()
    {
        if (titleEntered) return;
        titleEntered = true;
        if (titlePanel != null) Destroy(titlePanel);
        mainMenuPanel.SetActive(true);
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
            Debug.LogWarning("NVIDIA GPU에서 PyTorch 연산을 실행할 수 없습니다. GPU와 호환되는 torch 설치가 필요합니다.");
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
        if (!titleEntered) return;
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
            GUILayout.Label("가상환경은 있지만 현재 PyTorch로 GPU 연산을 실행할 수 없습니다.");
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
        ShowChapters();

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
                chapterId = 1,
                chapterName = "초원",
                displayName = "1-1 초원 · 낮",
                sceneName = "Grassfield_1",
                description = "챕터 1 · 초원 지대 / 낮"
            },
            new CampaignStageEntry
            {
                chapterId = 1,
                chapterName = "초원",
                displayName = "1-2 초원 · 저녁",
                sceneName = "Grassfield_2",
                description = "챕터 1 · 초원 지대 / 저녁"
            },
            new CampaignStageEntry
            {
                chapterId = 1,
                chapterName = "초원",
                displayName = "1-3 초원 · 밤",
                sceneName = "Grassfield_3",
                description = "챕터 1 · 초원 지대 / 밤"
            },
            new CampaignStageEntry
            {
                chapterId = 2,
                chapterName = "사막",
                displayName = "2-1 사막 · 낮",
                sceneName = "Desert_1",
                description = "챕터 2 · 사막 지대 / 낮"
            },
            new CampaignStageEntry
            {
                chapterId = 3,
                chapterName = "도시",
                displayName = "3-1 도시 · 낮",
                sceneName = "City_1",
                description = "챕터 3 · 도심 방어선 / 낮"
            }
        };
    }

    CampaignSelectionUi campaignUi;

    void BuildCampaignPanel()
    {
        Canvas canvas = UiFont.FindSceneCanvas(this);
        if (canvas == null) return;
        Transform existingMain = canvas.transform.Find("Panel");
        if (existingMain != null) mainMenuPanel = existingMain.gameObject;
        Transform existingCampaign = canvas.transform.Find("Panel_Campaign");
        if (existingCampaign != null) Destroy(existingCampaign.gameObject);
        campaignPanel = new GameObject("Panel_Campaign", typeof(RectTransform));
        campaignPanel.transform.SetParent(canvas.transform, false);
        UiFont.Stretch(campaignPanel.GetComponent<RectTransform>());
        campaignUi = new CampaignSelectionUi(campaignPanel.transform, campaignStages, requireStageUnlocks, CloseCampaign, LoadScene);
        campaignPanel.SetActive(false);
    }

    public void ShowChapters()
    {
        if (campaignUi != null) campaignUi.ShowChapters();
    }

    public void ShowChapterStages(int chapterId)
    {
        if (campaignUi != null) campaignUi.ShowStages(chapterId);
    }

    void BuildStyledMainMenu()
    {
        Canvas canvas = UiFont.FindSceneCanvas(this);
        if (canvas == null) return;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        Image background = UiFont.AddImage(canvas.transform, "MainMenu_Styled", UiSkin.Panel);
        UiFont.Stretch(background.rectTransform);
        mainMenuPanel = background.gameObject;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f; }
        var rays = new GameObject("MenuRadiance", typeof(RectTransform), typeof(CanvasRenderer), typeof(TitleScreenRays));
        rays.transform.SetParent(background.transform, false);
        UiFont.Stretch(rays.GetComponent<RectTransform>());
        rays.GetComponent<TitleScreenRays>().raycastTarget = false;
        var logo = new GameObject("MenuLogo", typeof(RectTransform), typeof(RawImage));
        logo.transform.SetParent(background.transform, false);
        var logoImage = logo.GetComponent<RawImage>();
        var logoTexture = Resources.Load<Texture2D>("UI/Title/LogoOneline");
        logoImage.texture = logoTexture;
        logoImage.raycastTarget = false;
        UiSkin.Place(logoImage.rectTransform, new Vector2(.5f, .85f), Vector2.zero,
            new Vector2(1000, logoTexture != null ? 1000f * logoTexture.height / logoTexture.width : 234));
        UiLogoDepth.Apply(logoImage);
        Text subtitle = UiFont.AddText(background.transform, "Subtitle", "인식하고, 연구하고, 방어하라", 28, TextAnchor.MiddleCenter, Color.white, UiType.BodyBold);
        UiSkin.Place(subtitle.rectTransform, new Vector2(.5f, .70f), Vector2.zero, new Vector2(1000, 60));
        MenuTurret(background.transform, "Build_Turret_Test Missile", new Vector2(.20f, .59f), -12, true);
        MenuTurret(background.transform, "Build_Turret_Test Laser", new Vector2(.80f, .59f), 12, false);
        MenuTurret(background.transform, "Build_Turret_MG", new Vector2(.20f, .37f), 8, true);
        MenuTurret(background.transform, "Build_Turret_PrecisionShot", new Vector2(.80f, .37f), -8, false);
        UiSkin.Preview(background.transform, "Build_Turret_MG", new Vector2(.5f, .49f), new Vector2(580, 435));
        UiSkin.Button(background.transform, "Settings", "설정", UiSkin.Sky, new Vector2(1, 1), new Vector2(-120, -65), new Vector2(180, 76)).onClick.AddListener(() =>
        {
            if (!AudioSettingsUi.IsOpen) settingsPanel = AudioSettingsUi.Create(canvas.transform);
        });
        UiSkin.Button(background.transform, "ControlsHelp", "조작키 안내", UiSkin.Blue, new Vector2(0, 1), new Vector2(150, -65), new Vector2(240, 76)).onClick.AddListener(() =>
        {
            if (!AudioSettingsUi.IsOpen) AudioSettingsUi.CreateHelp(canvas.transform);
        });
        UiSkin.Button(background.transform, "Campaign", "캠페인 시작  ›", UiSkin.Gold, new Vector2(.5f, .28f), Vector2.zero, new Vector2(600, 110)).onClick.AddListener(OpenCampaign);
        UiSkin.Button(background.transform, "TurretLab", "포탑 연구소", UiSkin.Blue, new Vector2(.32f, .15f), Vector2.zero, new Vector2(360, 100)).onClick.AddListener(() => LoadScene("TurretLab"));
        UiSkin.Button(background.transform, "DatasetFactory", "데이터셋 팩토리", UiSkin.Blue, new Vector2(.55f, .15f), Vector2.zero, new Vector2(420, 100)).onClick.AddListener(() => LoadScene("DatasetGeneratorScene"));
        UiSkin.Button(background.transform, "Quit", "종료", UiSkin.Navy, new Vector2(.77f, .15f), Vector2.zero, new Vector2(280, 100)).onClick.AddListener(QuitGame);
    }

    static void MenuTurret(Transform parent, string resource, Vector2 anchor, float angle, bool mirror)
    {
        var preview = UiSkin.Preview(parent, resource, anchor, new Vector2(360, 270));
        if (preview == null) return;
        preview.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        if (mirror) preview.uvRect = new Rect(1, 0, -1, 1);
    }

    void ApplyMainMenuFonts()
    {
        Canvas canvas = UiFont.FindSceneCanvas(this);
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
