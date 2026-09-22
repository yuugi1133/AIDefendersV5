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
    public WaveData[] waves;
}
