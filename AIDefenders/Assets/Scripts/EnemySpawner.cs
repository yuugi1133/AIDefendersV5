using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] spawnList;

    public float spawnRange = 20;
    public float spawnInterval = 2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating("SpawnTarget", 1, spawnInterval);
    }

    void SpawnTarget()
    {
        if (spawnList.Length <= 0)
            return;

        Vector3 pos = new Vector3(
            Random.Range(-spawnRange, spawnRange),
            0,    //캡슐 테스터일 경우 1을 쓸 것
            Random.Range(-spawnRange, spawnRange)
        );

        Instantiate(spawnList[Random.Range(0, spawnList.Length)], pos, Quaternion.identity);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
