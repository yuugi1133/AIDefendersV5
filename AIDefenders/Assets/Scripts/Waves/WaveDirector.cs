using UnityEngine;
using System;
using System.Collections.Generic;

public class SpawnerPreviewLine
{
    public string label;
    public int count;
}

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
    public int Score { get; private set; }
    public float BaseHP { get; private set; }
    public float MaxBaseHP { get; private set; }

    public int WaveCount
    {
        get { return stage != null && stage.waves != null ? stage.waves.Length : 0; }
    }

    public string HudPhaseLabel
    {
        get
        {
            switch (Phase)
            {
                case WavePhase.Prep:
                    return "준비";
                case WavePhase.Combat:
                    return "전투";
                case WavePhase.Complete:
                    return "스테이지 완료";
                default:
                    return "대기";
            }
        }
    }

    public string HudWaveTitle
    {
        get
        {
            if (Phase == WavePhase.Prep)
            {
                int number = Mathf.Max(1, CurrentWaveIndex + 1);
                return WaveCount > 0
                    ? "다음 웨이브  " + number + " / " + WaveCount
                    : "다음 웨이브  " + number;
            }

            if (currentWave != null && !string.IsNullOrWhiteSpace(currentWave.waveName))
                return currentWave.waveName;

            if (CurrentWaveIndex >= 0)
            {
                int number = CurrentWaveIndex + 1;
                return WaveCount > 0
                    ? "웨이브  " + number + " / " + WaveCount
                    : "웨이브  " + number;
            }

            return "대기";
        }
    }

    public event Action<int, float> OnPrepStarted;
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveEnded;
    public event Action OnStageComplete;
    public event Action<int> OnFundsChanged;
    public event Action<int> OnScoreChanged;
    public event Action<float, float> OnBaseHpChanged;

    bool[] groupSpawned;
    WaveData currentWave;

    void Awake()
    {
        ResolveCameras();
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
        Score = 0;
        MaxBaseHP = stage.startingBaseHP > 0f ? stage.startingBaseHP : 20f;
        BaseHP = MaxBaseHP;
        OnFundsChanged?.Invoke(Funds);
        OnScoreChanged?.Invoke(Score);
        OnBaseHpChanged?.Invoke(BaseHP, MaxBaseHP);
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

    public void AddScore(int amount)
    {
        Score = Mathf.Max(0, Score + amount);
        OnScoreChanged?.Invoke(Score);
    }

    public void DamageBase(float amount)
    {
        if (amount <= 0f || BaseHP <= 0f)
            return;

        BaseHP = Mathf.Max(0f, BaseHP - amount);
        OnBaseHpChanged?.Invoke(BaseHP, MaxBaseHP);
        if (BaseHP > 0f)
            return;

        Phase = WavePhase.Complete;
        PhaseTimeRemaining = 0f;
        Debug.Log("기지 파괴");
        OnStageComplete?.Invoke();
    }

    void Update()
    {
        if (Phase != WavePhase.Prep && Phase != WavePhase.Combat)
            return;

        PhaseTimeRemaining -= Time.deltaTime;

        if (Phase == WavePhase.Combat)
        {
            SpawnDueGroups();
            if (AllGroupsSpawned() && FieldIsClear())
            {
                EndCurrentWave();
                return;
            }
        }

        if (PhaseTimeRemaining > 0f)
            return;

        if (Phase == WavePhase.Prep)
        {
            EnterCombat();
            return;
        }

        EndCurrentWave();
    }

    public bool TryGetSpawnerPreview(EnemySpawner enemySpawner, List<SpawnerPreviewLine> results)
    {
        if (results == null)
            return false;

        results.Clear();
        if (Phase != WavePhase.Prep || currentWave == null || currentWave.groups == null || enemySpawner == null)
            return false;

        Dictionary<string, int> counts = new Dictionary<string, int>();
        List<string> order = new List<string>();

        for (int i = 0; i < currentWave.groups.Length; i++)
        {
            SpawnGroup group = currentWave.groups[i];
            if (group == null || ResolveSpawner(group) != enemySpawner)
                continue;
            if (group.targets == null)
                continue;

            for (int t = 0; t < group.targets.Length; t++)
            {
                SpawnTargetEntry entry = group.targets[t];
                if (entry == null || entry.prefab == null || entry.count <= 0)
                    continue;

                string label = TargetHudLabel(entry.prefab);
                int next;
                if (counts.TryGetValue(label, out next))
                    counts[label] = next + entry.count;
                else
                {
                    counts[label] = entry.count;
                    order.Add(label);
                }
            }
        }

        for (int i = 0; i < order.Count; i++)
        {
            string label = order[i];
            SpawnerPreviewLine line = new SpawnerPreviewLine();
            line.label = label;
            line.count = counts[label];
            results.Add(line);
        }

        return results.Count > 0;
    }

    static string TargetHudLabel(GameObject prefab)
    {
        Target_Base target = prefab.GetComponent<Target_Base>();
        if (target != null)
            return target.HudLabel;
        return prefab.name;
    }

    public void SkipPrep()
    {
        if (Phase != WavePhase.Prep)
            return;

        EnterCombat();
    }

    void EnterPrep(float duration)
    {
        Phase = WavePhase.Prep;
        PhaseTimeRemaining = Mathf.Max(0f, duration);
        currentWave = stage.waves[CurrentWaveIndex];

        Debug.Log($"준비 시간 시작: Wave {CurrentWaveIndex + 1} / {duration:0.0}s");
        OnPrepStarted?.Invoke(CurrentWaveIndex, duration);

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

    public EnemySpawner ResolveSpawner(SpawnGroup group)
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

    bool AllGroupsSpawned()
    {
        if (currentWave == null || currentWave.groups == null || currentWave.groups.Length == 0)
            return true;
        if (groupSpawned == null)
            return false;

        for (int i = 0; i < groupSpawned.Length; i++)
        {
            if (!groupSpawned[i])
                return false;
        }

        return true;
    }

    static bool FieldIsClear()
    {
        Target_Base[] targets = FindObjectsByType<Target_Base>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < targets.Length; i++)
        {
            Target_Base target = targets[i];
            if (target != null && target.IsActiveInPlay)
                return false;
        }

        return true;
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
                text = $"준비 시간\n다음 Wave {CurrentWaveIndex + 1}\n{PhaseTimeRemaining:0.0}s\n자금 {Funds}  점수 {Score}\n기지 {BaseHP:0}/{MaxBaseHP:0}";
                break;
            case WavePhase.Combat:
                string waveName = currentWave != null && !string.IsNullOrEmpty(currentWave.waveName)
                    ? currentWave.waveName
                    : $"Wave {CurrentWaveIndex + 1}";
                text = $"{waveName}\n남은 시간 {PhaseTimeRemaining:0.0}s\n자금 {Funds}  점수 {Score}\n기지 {BaseHP:0}/{MaxBaseHP:0}";
                break;
            case WavePhase.Complete:
                text = $"스테이지 완료\n자금 {Funds}  점수 {Score}\n기지 {BaseHP:0}/{MaxBaseHP:0}";
                break;
            default:
                text = $"대기\n자금 {Funds}";
                break;
        }

        GUI.Label(new Rect(20f, 20f, 320f, 130f), text);
    }

    void ResolveCameras()
    {
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
}
