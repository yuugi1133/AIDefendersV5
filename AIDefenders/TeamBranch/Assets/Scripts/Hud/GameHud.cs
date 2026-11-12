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
            gameObject.AddComponent<BattlePauseMenu>().Build(canvasRt, waveDirector);
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
            fundsText.text = funds.ToString("N0");
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
            waveText.text = "ROUND  " + Mathf.Max(1, waveDirector.CurrentWaveIndex + 1) + " / " + waveDirector.WaveCount;
        if (fundsText != null)
            fundsText.text = waveDirector.Funds.ToString("N0");
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
            baseHpText.text = Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(max);

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

        RectTransform root = canvas.transform as RectTransform;
        Image health = UiFont.AddImage(root, "HealthChip", new Color(.60f, .16f, .18f, 1));
        UiSkin.Place(health.rectTransform, new Vector2(0, 1), new Vector2(132, -66), new Vector2(216, 84));
        var heart = new GameObject("HeartIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiHeartIcon));
        heart.transform.SetParent(health.transform, false);
        UiSkin.Place(heart.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(35, -29), new Vector2(46, 42));
        heart.GetComponent<UiHeartIcon>().raycastTarget = false;
        baseHpText = AddLabel(health.rectTransform, "BaseHp", "0 / 0", 24, UiType.Title, 0, 1, -8, 40);
        baseHpText.rectTransform.offsetMin = new Vector2(66, baseHpText.rectTransform.offsetMin.y);
        Image track = UiFont.AddImage(health.transform, "BaseTrack", UiSkin.Navy);
        UiSkin.Place(track.rectTransform, new Vector2(.5f, 0), new Vector2(0, 16), new Vector2(184, 12));
        baseHpFill = UiFont.AddImage(track.transform, "Fill", UiSkin.Green);
        UiFont.Stretch(baseHpFill.rectTransform);
        baseHpFill.type = Image.Type.Filled;
        baseHpFill.fillMethod = Image.FillMethod.Horizontal;
        baseHpFill.raycastTarget = track.raycastTarget = health.raycastTarget = false;
        Image money = UiFont.AddImage(root, "FundsChip", UiSkin.Gold);
        UiSkin.Place(money.rectTransform, new Vector2(0, 1), new Vector2(360, -66), new Vector2(216, 84));
        var coin = new GameObject("CoinIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiCoinIcon));
        coin.transform.SetParent(money.transform, false);
        UiSkin.Place(coin.GetComponent<RectTransform>(), new Vector2(0, .5f), new Vector2(36, 0), new Vector2(52, 52));
        coin.GetComponent<UiCoinIcon>().raycastTarget = false;
        fundsText = AddLabel(money.rectTransform, "Funds", "0", 30, UiType.Title, 0, 1, -12, 58);
        fundsText.rectTransform.offsetMin = new Vector2(72, fundsText.rectTransform.offsetMin.y);
        Image points = UiFont.AddImage(root, "ScoreChip", UiSkin.Panel);
        UiSkin.Place(points.rectTransform, new Vector2(0, 1), new Vector2(588, -66), new Vector2(216, 84));
        scoreText = AddLabel(points.rectTransform, "Score", "SCORE  0", 24, UiType.Title, 0, 1, -12, 58);
        Image phase = UiFont.AddImage(root, "PhaseChip", UiSkin.Panel);
        UiSkin.Place(phase.rectTransform, new Vector2(0, 1), new Vector2(900, -66), new Vector2(360, 84));
        phaseText = AddLabel(phase.rectTransform, "Phase", "", 21, UiType.BodyBold, 0, 1, -6, 30);
        timeText = AddLabel(phase.rectTransform, "Time", "", 24, UiType.Title, 0, 1, -38, 36);
        Image round = UiFont.AddImage(root, "RoundChip", UiSkin.Panel);
        UiSkin.Place(round.rectTransform, new Vector2(1, 1), new Vector2(-294, -66), new Vector2(300, 84));
        waveText = AddLabel(round.rectTransform, "Wave", "ROUND  --", 30, UiType.Title, 0, 1, -12, 58);
        money.raycastTarget = points.raycastTarget = phase.raycastTarget = round.raycastTarget = false;
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
