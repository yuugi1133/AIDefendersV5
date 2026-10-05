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
    public EnemySpawner[] spawners;

    [Header("Player Cameras")]
    public Camera preparationCamera;
    public Camera waveCamera;

    public bool autoStart = true;
    public bool showDebugHud = true;

    public WavePhase Phase { get; private set; } = WavePhase.Idle;
    public int CurrentWaveIndex { get; private set; } = -1;
    public float PhaseTimeRemaining { get; private set; }
    public int Funds { get; private set; }

    public event Action<int, float> OnPrepStarted;
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveEnded;
    public event Action OnStageComplete;
    public event Action<int> OnFundsChanged;

    bool[] groupSpawned;
    WaveData currentWave;

    void Awake()
    {
        ResolveCameras();
        SetPlayerView(false);
    }

    void Start()
    {
        ResolveSpawners();

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
        Funds = Mathf.Max(0, stage.startingFunds);
        OnFundsChanged?.Invoke(Funds);
        EnterPrep(stage.firstPrepTime);
    }

    public bool CanAfford(int cost)
    {
        if (cost <= 0)
            return true;

        return Funds >= cost;
    }

    public bool TrySpend(int cost)
    {
        if (cost <= 0)
            return true;

        if (Funds < cost)
            return false;

        Funds -= cost;
        OnFundsChanged?.Invoke(Funds);
        return true;
    }

    public void AddFunds(int amount)
    {
        Funds = Mathf.Max(0, Funds + amount);
        OnFundsChanged?.Invoke(Funds);
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
        if (currentWave == null || currentWave.groups == null)
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

            EnemySpawner chosen = ResolveSpawner(group);
            if (chosen == null)
            {
                Debug.LogWarning(
                    "WaveDirector: 스포너를 찾지 못했습니다. " +
                    (string.IsNullOrWhiteSpace(group.spawnerId) ? "(기본)" : group.spawnerId)
                );
                continue;
            }

            chosen.SpawnGroup(group);
        }
    }

    void ResolveSpawners()
    {
        if (spawners == null || spawners.Length == 0)
        {
            spawners = FindObjectsByType<EnemySpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
        }

        if (spawner == null)
            spawner = GetComponent<EnemySpawner>();

        if (spawner == null && spawners != null && spawners.Length > 0)
            spawner = spawners[0];
    }

    EnemySpawner ResolveSpawner(SpawnGroup group)
    {
        if (group != null && !string.IsNullOrWhiteSpace(group.spawnerId))
        {
            EnemySpawner found = FindSpawner(group.spawnerId);
            if (found != null)
                return found;
        }

        return spawner;
    }

    EnemySpawner FindSpawner(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        string key = id.Trim();

        if (spawners != null)
        {
            for (int i = 0; i < spawners.Length; i++)
            {
                EnemySpawner candidate = spawners[i];
                if (candidate != null && MatchesSpawner(candidate, key))
                    return candidate;
            }
        }

        if (spawner != null && MatchesSpawner(spawner, key))
            return spawner;

        EnemySpawner[] found = FindObjectsByType<EnemySpawner>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && MatchesSpawner(found[i], key))
                return found[i];
        }

        return null;
    }

    static bool MatchesSpawner(EnemySpawner candidate, string key)
    {
        return string.Equals(candidate.Id, key, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate.gameObject.name, key, System.StringComparison.OrdinalIgnoreCase);
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
                text = $"준비 시간\n다음 Wave {CurrentWaveIndex + 1}\n{PhaseTimeRemaining:0.0}s\n자금 {Funds}";
                break;
            case WavePhase.Combat:
                string waveName = currentWave != null && !string.IsNullOrEmpty(currentWave.waveName)
                    ? currentWave.waveName
                    : $"Wave {CurrentWaveIndex + 1}";
                text = $"{waveName}\n남은 시간 {PhaseTimeRemaining:0.0}s\n자금 {Funds}";
                break;
            case WavePhase.Complete:
                text = $"스테이지 완료\n자금 {Funds}";
                break;
            default:
                text = $"대기\n자금 {Funds}";
                break;
        }

        GUI.Label(new Rect(20f, 20f, 320f, 100f), text);
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
