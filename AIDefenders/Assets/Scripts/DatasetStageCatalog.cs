using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데이터셋 생성 UI의 게임 스테이지 목록입니다.
/// 스테이지를 추가할 때는 여기에 한 줄을 넣고, 등장 대상은 해당 StageData의 웨이브 spawn prefab에서 읽습니다.
/// </summary>
public static class DatasetStageCatalog
{
    public class Entry
    {
        public string displayName;
        public string stageDataPath;
    }

    public static readonly Entry[] Stages =
    {
        new Entry
        {
            displayName = "Grassfield 1",
            stageDataPath = "Assets/Waves/StageData_GrassField_1.asset"
        },
        new Entry
        {
            displayName = "Grassfield 2",
            stageDataPath = "Assets/Waves/StageData_GrassField_2.asset"
        },
        new Entry
        {
            displayName = "Grassfield 3",
            stageDataPath = "Assets/Waves/StageData_GrassField_3.asset"
        }
    };

    public static StageData LoadStageData(Entry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.stageDataPath))
            return null;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<StageData>(entry.stageDataPath);
#else
        return null;
#endif
    }

    public static void CollectTargets(Entry entry, List<Target_Base> results)
    {
        StageData data = LoadStageData(entry);
        if (data != null)
            data.CollectSpawnTargets(results);
    }
}
