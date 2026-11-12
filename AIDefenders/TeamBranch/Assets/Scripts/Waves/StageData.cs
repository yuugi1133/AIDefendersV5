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
}
