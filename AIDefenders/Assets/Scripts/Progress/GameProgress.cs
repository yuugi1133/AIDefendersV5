using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class GameProgressData
{
    public int researchPoints;
    public List<string> unlockedStages = new List<string>();
    public List<string> completedResearch = new List<string>();
}

public static class GameProgress
{
    public const string FirstStage = "Grassfield_1";
    public static readonly string[] CampaignStages =
    {
        "Grassfield_1",
        "Grassfield_2",
        "Grassfield_3"
    };

    const string FileName = "progress.json";

    static GameProgressData data;

    public static int ResearchPoints
    {
        get { return Mathf.Max(0, Loaded.researchPoints); }
    }

    static GameProgressData Loaded
    {
        get
        {
            if (data == null)
                Load();
            return data;
        }
    }

    public static void Load()
    {
        string path = Path();
        if (File.Exists(path))
        {
            try
            {
                data = JsonUtility.FromJson<GameProgressData>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("진행 저장 파일을 읽지 못했습니다: " + exception.Message);
                data = null;
            }
        }

        if (data == null)
            data = new GameProgressData();
        if (data.unlockedStages == null)
            data.unlockedStages = new List<string>();
        if (data.completedResearch == null)
            data.completedResearch = new List<string>();
        EnsureUnlocked(FirstStage);
    }

    public static void Save()
    {
        if (data == null)
            Load();

        EnsureUnlocked(FirstStage);
        try
        {
            File.WriteAllText(Path(), JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("진행 저장에 실패했습니다: " + exception.Message);
        }
    }

    public static bool IsStageUnlocked(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;
        if (string.Equals(sceneName, FirstStage, StringComparison.Ordinal))
            return true;

        List<string> stages = Loaded.unlockedStages;
        for (int i = 0; i < stages.Count; i++)
        {
            if (string.Equals(stages[i], sceneName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static bool HasResearch(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        List<string> completed = Loaded.completedResearch;
        for (int i = 0; i < completed.Count; i++)
        {
            if (string.Equals(completed[i], id, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static bool TrySpendResearchPoints(int cost)
    {
        if (cost < 0)
            return false;
        if (ResearchPoints < cost)
            return false;

        Loaded.researchPoints = ResearchPoints - cost;
        Save();
        return true;
    }

    public static void UnlockResearch(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || HasResearch(id))
            return;

        Loaded.completedResearch.Add(id);
        Save();
    }

    public static int ApplyStageResult(string sceneName, int score, bool cleared)
    {
        int gained = Mathf.Max(0, score);
        Loaded.researchPoints = ResearchPoints + gained;
        if (cleared)
            UnlockAfterClear(sceneName);
        Save();
        return gained;
    }

    static void UnlockAfterClear(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
            EnsureUnlocked(sceneName);

        for (int i = 0; i < CampaignStages.Length - 1; i++)
        {
            if (!string.Equals(CampaignStages[i], sceneName, StringComparison.Ordinal))
                continue;
            EnsureUnlocked(CampaignStages[i + 1]);
            return;
        }
    }

    static void EnsureUnlocked(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;
        if (IsStageUnlocked(sceneName) && !string.Equals(sceneName, FirstStage, StringComparison.Ordinal))
            return;

        List<string> stages = Loaded.unlockedStages;
        for (int i = 0; i < stages.Count; i++)
        {
            if (string.Equals(stages[i], sceneName, StringComparison.Ordinal))
                return;
        }

        stages.Add(sceneName);
    }

    static string Path()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, FileName);
    }
}
