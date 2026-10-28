using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    public WaveDirector waveDirector;
    public TurretPlatformPlacer turretPlacer;
    public GameManager gameManager;
    public CCTVSkillSystem skillSystem;

    Canvas canvas;
    HudBuildUi buildUi;
    HudModelSelectUi modelUi;
    HudSkillUi skillUi;
    HudWorldOverlay worldOverlay;
    HudWaveBanner waveBanner;
    HudSpawnerPreview spawnerPreview;
    HudStageResult stageResult;
    Text phaseText;
    Text waveText;
    Text timeText;
    Text fundsText;
    Text scoreText;
    Text baseHpText;
    Image baseHpFill;

    void Awake()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();
        if (waveDirector != null)
            waveDirector.showDebugHud = false;
        if (turretPlacer == null)
            turretPlacer = FindFirstObjectByType<TurretPlatformPlacer>();
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (skillSystem == null)
            skillSystem = FindFirstObjectByType<CCTVSkillSystem>();

        EnsureEventSystem();
        BuildCanvas();
        RectTransform canvasRt = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRt != null)
        {
            buildUi = new HudBuildUi(canvasRt, turretPlacer, waveDirector);
            if (gameManager != null)
                modelUi = new HudModelSelectUi(canvasRt, gameManager);
            if (skillSystem != null)
                skillUi = new HudSkillUi(canvasRt, skillSystem);
            worldOverlay = new HudWorldOverlay(canvasRt);
            waveBanner = new HudWaveBanner(canvasRt);
            spawnerPreview = new HudSpawnerPreview(canvasRt, waveDirector);
            stageResult = new HudStageResult(canvasRt);
        }
        Refresh(true);
    }

    void OnEnable()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();
        if (waveDirector == null)
            return;

        waveDirector.OnFundsChanged += HandleFundsChanged;
        waveDirector.OnScoreChanged += HandleScoreChanged;
        waveDirector.OnBaseHpChanged += HandleBaseHpChanged;
        waveDirector.OnPrepStarted += HandlePrepStarted;
        waveDirector.OnWaveStarted += HandleWaveStarted;
        waveDirector.OnWaveEnded += HandleWaveEnded;
        waveDirector.OnStageComplete += HandleStageComplete;
    }

    void OnDisable()
    {
        if (waveDirector == null)
            return;

        waveDirector.OnFundsChanged -= HandleFundsChanged;
        waveDirector.OnScoreChanged -= HandleScoreChanged;
        waveDirector.OnBaseHpChanged -= HandleBaseHpChanged;
        waveDirector.OnPrepStarted -= HandlePrepStarted;
        waveDirector.OnWaveStarted -= HandleWaveStarted;
        waveDirector.OnWaveEnded -= HandleWaveEnded;
        waveDirector.OnStageComplete -= HandleStageComplete;
    }

    void Update()
    {
        RefreshTime();
        if (buildUi != null)
            buildUi.Tick();
        if (modelUi != null)
            modelUi.Tick();
        if (skillUi != null)
            skillUi.Tick();
        if (waveBanner != null)
            waveBanner.Tick();
        if (stageResult != null)
            stageResult.Tick();
    }

    void LateUpdate()
    {
        if (worldOverlay != null)
            worldOverlay.Tick();
        if (spawnerPreview != null)
            spawnerPreview.Tick();
    }

    void OnDestroy()
    {
        if (worldOverlay != null)
            worldOverlay.Dispose();
    }

    void HandleFundsChanged(int funds)
    {
        if (fundsText != null)
            fundsText.text = "자금  $" + funds;
    }

    void HandleScoreChanged(int score)
    {
        if (scoreText != null)
            scoreText.text = "점수  " + score;
    }

    void HandleBaseHpChanged(float hp, float max)
    {
        RefreshBaseHp(hp, max);
    }

    void HandlePrepStarted(int waveIndex, float duration)
    {
        Refresh(true);
    }

    void HandleWaveStarted(int waveIndex)
    {
        Refresh(true);
        if (waveBanner != null && waveDirector != null)
            waveBanner.Play(waveDirector.HudWaveTitle, waveIndex + 1, waveDirector.WaveCount);
    }

    void HandleWaveEnded(int waveIndex)
    {
        Refresh(true);
    }

    void HandleStageComplete()
    {
        Refresh(true);
        if (stageResult != null && waveDirector != null)
            stageResult.Show(waveDirector.LastReport);
    }

    void Refresh(bool includeTime)
    {
        if (waveDirector == null)
            return;

        if (phaseText != null)
            phaseText.text = waveDirector.HudPhaseLabel;
        if (waveText != null)
            waveText.text = waveDirector.HudWaveTitle;
        if (fundsText != null)
            fundsText.text = "자금  $" + waveDirector.Funds;
        if (scoreText != null)
            scoreText.text = "점수  " + waveDirector.Score;
        RefreshBaseHp(waveDirector.BaseHP, waveDirector.MaxBaseHP);
        if (includeTime)
            RefreshTime();
    }

    void RefreshTime()
    {
        if (timeText == null || waveDirector == null)
            return;

        if (waveDirector.Phase == WavePhase.Idle)
        {
            timeText.text = "시간  --";
            return;
        }

        if (waveDirector.Phase == WavePhase.Complete)
        {
            timeText.text = "시간  완료";
            return;
        }

        string prefix = waveDirector.Phase == WavePhase.Prep ? "시작까지  " : "남은 시간  ";
        timeText.text = prefix + FormatTime(waveDirector.PhaseTimeRemaining);
    }

    void RefreshBaseHp(float hp, float max)
    {
        if (baseHpText != null)
            baseHpText.text = "기지  " + Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(max);

        if (baseHpFill == null)
            return;

        float t = max > 0.01f ? Mathf.Clamp01(hp / max) : 0f;
        baseHpFill.fillAmount = t;
        if (t > 0.45f)
            baseHpFill.color = new Color(0.32f, 0.78f, 0.48f, 1f);
        else if (t > 0.2f)
            baseHpFill.color = new Color(0.92f, 0.72f, 0.28f, 1f);
        else
            baseHpFill.color = new Color(0.9f, 0.28f, 0.24f, 1f);
    }

    static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int whole = Mathf.FloorToInt(seconds);
        int minutes = whole / 60;
        int rest = whole % 60;
        if (minutes > 0)
            return minutes + ":" + rest.ToString("00");
        return seconds.ToString("0.0") + "초";
    }

    void BuildCanvas()
    {
        Transform existing = transform.Find("Canvas");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.layer = 5;
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        Image panel = UiFont.AddImage(canvasGo.transform, "StatusPanel", new Color(0.08f, 0.1f, 0.14f, 0.82f));
        panel.raycastTarget = false;
        RectTransform panelRt = panel.rectTransform;
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(24f, -24f);
        panelRt.sizeDelta = new Vector2(420f, 228f);

        phaseText = AddLabel(panelRt, "Phase", "대기", 28, UiType.Title, 0f, 1f, -16f, 40f);
        waveText = AddLabel(panelRt, "Wave", "대기", 20, UiType.BodyMedium, 0f, 1f, -58f, 30f);
        timeText = AddLabel(panelRt, "Time", "시간  --", 22, UiType.BodyMedium, 0f, 1f, -92f, 32f);
        fundsText = AddLabel(panelRt, "Funds", "자금  $0", 22, UiType.BodyMedium, 0f, 0.55f, -128f, 32f);
        scoreText = AddLabel(panelRt, "Score", "점수  0", 22, UiType.BodyMedium, 0.55f, 1f, -128f, 32f);

        Image track = UiFont.AddImage(panelRt, "BaseTrack", new Color(0.16f, 0.18f, 0.22f, 1f));
        track.raycastTarget = false;
        RectTransform trackRt = track.rectTransform;
        trackRt.anchorMin = new Vector2(0f, 1f);
        trackRt.anchorMax = new Vector2(1f, 1f);
        trackRt.pivot = new Vector2(0.5f, 1f);
        trackRt.anchoredPosition = new Vector2(0f, -168f);
        trackRt.sizeDelta = new Vector2(-40f, 18f);

        baseHpFill = UiFont.AddImage(trackRt, "Fill", new Color(0.32f, 0.78f, 0.48f, 1f));
        baseHpFill.raycastTarget = false;
        baseHpFill.type = Image.Type.Filled;
        baseHpFill.fillMethod = Image.FillMethod.Horizontal;
        baseHpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        baseHpFill.fillAmount = 1f;
        UiFont.Stretch(baseHpFill.rectTransform);

        baseHpText = AddLabel(panelRt, "BaseHp", "기지  0 / 0", 16, UiType.Body, 0f, 1f, -190f, 24f);
        baseHpText.color = new Color(0.86f, 0.88f, 0.9f, 1f);
    }

    static Text AddLabel(
        RectTransform parent,
        string name,
        string content,
        int size,
        UiType type,
        float anchorMinX,
        float anchorMaxX,
        float top,
        float height)
    {
        Text text = UiFont.AddText(parent, name, content, size, TextAnchor.MiddleLeft, Color.white, type);
        text.raycastTarget = false;
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(anchorMinX, 1f);
        rt.anchorMax = new Vector2(anchorMaxX, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, top);
        rt.sizeDelta = new Vector2(-40f, height);
        return text;
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        EventSystem eventSystem = go.AddComponent<EventSystem>();
        eventSystem.sendNavigationEvents = false;
        go.AddComponent<InputSystemUIInputModule>();
    }
}
