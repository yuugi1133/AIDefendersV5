using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnTargetEntry
{
    public GameObject prefab;
    public int count = 1;
}

[System.Serializable]
public class SpawnGroup
{
    public float spawnTime;

    [Tooltip("비우면 WaveDirector 기본 스포너. EnemySpawner의 Spawner Id 또는 오브젝트 이름과 같게 넣습니다.")]
    public string spawnerId;

    public SpawnTargetEntry[] targets;
}

[System.Serializable]
public class WaveData
{
    public string waveName = "Wave";
    public float duration = 30f;
    public float nextWavePrepTime = 10f;
    public SpawnGroup[] groups;
}

[CreateAssetMenu(fileName = "StageData", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    public float firstPrepTime = 10f;
    public int startingFunds = 300;
    public float startingBaseHP = 20f;
    public WaveData[] waves;

    public void CollectSpawnTargets(List<Target_Base> results)
    {
        if (results == null || waves == null)
            return;

        for (int w = 0; w < waves.Length; w++)
        {
            WaveData wave = waves[w];
            if (wave == null || wave.groups == null)
                continue;

            for (int g = 0; g < wave.groups.Length; g++)
            {
                SpawnGroup group = wave.groups[g];
                if (group == null || group.targets == null)
                    continue;

                for (int t = 0; t < group.targets.Length; t++)
                {
                    SpawnTargetEntry entry = group.targets[t];
                    if (entry == null || entry.prefab == null)
                        continue;

                    Target_Base target = entry.prefab.GetComponent<Target_Base>();
                    if (target == null)
                        target = entry.prefab.GetComponentInChildren<Target_Base>(true);
                    if (target != null && !results.Contains(target))
                        results.Add(target);
                }
            }
        }
    }
}
