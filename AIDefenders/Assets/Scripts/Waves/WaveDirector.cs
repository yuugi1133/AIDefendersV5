using UnityEngine;
using System;

public enum WavePhase
{
    Idle,
    Prep,
    Combat,
    Complete
}

public class WaveDirector : MonoBehaviour
{
    public StageData stage;
    public EnemySpawner spawner;

    [Header("Player Cameras")]
    public Camera preparationCamera;
    public Camera waveCamera;

    public bool autoStart = true;
    public bool showDebugHud = true;

    public WavePhase Phase { get; private set; } = WavePhase.Idle;
    public int CurrentWaveIndex { get; private set; } = -1;
    public float PhaseTimeRemaining { get; private set; }

    public event Action<int, float> OnPrepStarted;
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveEnded;
    public event Action OnStageComplete;

    bool[] groupSpawned;
    WaveData currentWave;

    void Awake()
    {
        ResolveCameras();
        SetPlayerView(false);
    }

    void Start()
    {
        if (spawner == null)
            spawner = GetComponent<EnemySpawner>();

        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawner>();

        if (autoStart)
            BeginStage();
    }

    public void BeginStage()
    {
        if (stage == null || stage.waves == null || stage.waves.Length == 0)
        {
            Debug.LogWarning("WaveDirector: StageData가 없습니다.");
            Phase = WavePhase.Complete;
            return;
        }

        CurrentWaveIndex = 0;
        EnterPrep(stage.firstPrepTime);
    }

    void Update()
    {
        if (Phase != WavePhase.Prep && Phase != WavePhase.Combat)
            return;

        PhaseTimeRemaining -= Time.deltaTime;

        if (Phase == WavePhase.Combat)
            SpawnDueGroups();

        if (PhaseTimeRemaining > 0f)
            return;

        if (Phase == WavePhase.Prep)
        {
            EnterCombat();
            return;
        }

        EndCurrentWave();
    }

    void EnterPrep(float duration)
    {
        Phase = WavePhase.Prep;
        PhaseTimeRemaining = Mathf.Max(0f, duration);
        currentWave = stage.waves[CurrentWaveIndex];

        Debug.Log($"준비 시간 시작: Wave {CurrentWaveIndex + 1} / {duration:0.0}s");
        OnPrepStarted?.Invoke(CurrentWaveIndex, duration);
        SetPlayerView(false);

        if (PhaseTimeRemaining <= 0f)
            EnterCombat();
    }

    void EnterCombat()
    {
        currentWave = stage.waves[CurrentWaveIndex];
        Phase = WavePhase.Combat;
        PhaseTimeRemaining = Mathf.Max(0.01f, currentWave.duration);

        int groupCount = currentWave.groups != null ? currentWave.groups.Length : 0;
        groupSpawned = new bool[groupCount];

        string name = string.IsNullOrEmpty(currentWave.waveName)
            ? $"Wave {CurrentWaveIndex + 1}"
            : currentWave.waveName;

        Debug.Log($"{name} 시작 ({currentWave.duration:0.0}s)");
        OnWaveStarted?.Invoke(CurrentWaveIndex);
        SetPlayerView(true);

        SpawnDueGroups();
    }

    void SpawnDueGroups()
    {
        if (currentWave == null || currentWave.groups == null || spawner == null)
            return;

        float elapsed = currentWave.duration - PhaseTimeRemaining;

        for (int i = 0; i < currentWave.groups.Length; i++)
        {
            if (groupSpawned[i])
                continue;

            SpawnGroup group = currentWave.groups[i];
            if (group == null || elapsed < group.spawnTime)
                continue;

            groupSpawned[i] = true;
            spawner.SpawnGroup(group);
        }
    }

    void EndCurrentWave()
    {
        OnWaveEnded?.Invoke(CurrentWaveIndex);

        bool lastWave = CurrentWaveIndex >= stage.waves.Length - 1;
        if (lastWave)
        {
            Phase = WavePhase.Complete;
            PhaseTimeRemaining = 0f;
            Debug.Log("스테이지 완료");
            OnStageComplete?.Invoke();
            SetPlayerView(false);
            return;
        }

        float prep = currentWave != null ? currentWave.nextWavePrepTime : 0f;
        CurrentWaveIndex++;
        EnterPrep(prep);
    }

    void OnGUI()
    {
        if (!showDebugHud)
            return;

        string text;
        switch (Phase)
        {
            case WavePhase.Prep:
                text = $"준비 시간\n다음 Wave {CurrentWaveIndex + 1}\n{PhaseTimeRemaining:0.0}s";
                break;
            case WavePhase.Combat:
                string waveName = currentWave != null && !string.IsNullOrEmpty(currentWave.waveName)
                    ? currentWave.waveName
                    : $"Wave {CurrentWaveIndex + 1}";
                text = $"{waveName}\n남은 시간 {PhaseTimeRemaining:0.0}s";
                break;
            case WavePhase.Complete:
                text = "스테이지 완료";
                break;
            default:
                text = "대기";
                break;
        }

        GUI.Label(new Rect(20f, 20f, 320f, 80f), text);
    }

    void ResolveCameras()
    {
        if (preparationCamera == null)
            preparationCamera = FindCameraByName("Camera_GamePreparation");

        if (waveCamera == null)
            waveCamera = FindCameraByName("Camera_GameOnWave");
    }

    Camera FindCameraByName(string objectName)
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.gameObject.name == objectName)
                return camera;
        }

        return null;
    }

    void SetPlayerView(bool waveView)
    {
        SetPlayerCameraEnabled(preparationCamera, !waveView);
        SetPlayerCameraEnabled(waveCamera, waveView);
    }

    void SetPlayerCameraEnabled(Camera camera, bool enabled)
    {
        if (camera == null)
            return;

        // CCTVSystem / CCTVCamera 계층은 끄지 않고, 플레이어 Camera 컴포넌트만 전환한다.
        camera.enabled = enabled;
        camera.tag = enabled ? "MainCamera" : "Untagged";

        AudioListener listener = camera.GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = enabled;
    }
}
