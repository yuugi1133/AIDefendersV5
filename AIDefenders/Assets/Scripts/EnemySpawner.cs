using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Placement")]
    public float spawnRange = 20;
    public LayerMask groundMask;
    public float raycastHeight = 80f;
    public float spawnHeightOffset = 0.05f;
    public int maxPlaceAttempts = 8;

    public void SpawnGroup(SpawnGroup group)
    {
        if (group == null || group.targets == null)
            return;

        foreach (SpawnTargetEntry entry in group.targets)
            Spawn(entry);
    }

    public void Spawn(SpawnTargetEntry entry)
    {
        if (entry == null || entry.prefab == null || entry.count <= 0)
            return;

        for (int i = 0; i < entry.count; i++)
            SpawnOne(entry.prefab);
    }

    public void SpawnOne(GameObject prefab)
    {
        if (prefab == null)
            return;

        if (!TryGetGroundSpawnPoint(out Vector3 pos))
        {
            Debug.LogWarning("EnemySpawner: 지형 스폰 위치를 찾지 못했습니다.");
            return;
        }

        Instantiate(prefab, pos, Quaternion.identity);
    }

    bool TryGetGroundSpawnPoint(out Vector3 spawnPos)
    {
        spawnPos = transform.position;

        int mask = GetGroundMask();

        for (int i = 0; i < maxPlaceAttempts; i++)
        {
            float x = transform.position.x + Random.Range(-spawnRange, spawnRange);
            float z = transform.position.z + Random.Range(-spawnRange, spawnRange);
            Vector3 origin = new Vector3(x, transform.position.y + raycastHeight, z);

            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    raycastHeight * 2f,
                    mask,
                    QueryTriggerInteraction.Ignore))
            {
                spawnPos = hit.point + Vector3.up * spawnHeightOffset;
                return true;
            }
        }

        return false;
    }

    int GetGroundMask()
    {
        if (groundMask.value != 0)
            return groundMask.value;

        int mask = Physics.DefaultRaycastLayers;
        mask &= ~LayerMask.GetMask("Enemy", "Friendly", "Turret", "Bullet", "Ignore Raycast");
        return mask;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.25f);
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(spawnRange * 2f, 0.2f, spawnRange * 2f)
        );
    }
}
