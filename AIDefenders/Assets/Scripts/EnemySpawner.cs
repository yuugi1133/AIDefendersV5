using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("비우면 오브젝트 이름을 사용합니다. StageData 그룹의 Spawner Id와 맞춥니다.")]
    public string spawnerId;

    public string Id
    {
        get
        {
            if (string.IsNullOrWhiteSpace(spawnerId))
                return gameObject.name;

            return spawnerId.Trim();
        }
    }

    [Header("Placement")]
    public float spawnRange = 20;
    public LayerMask groundMask;
    public float raycastHeight = 80f;
    public float spawnHeightOffset = 0.05f;
    public int maxPlaceAttempts = 8;

    [Header("Route")]
    public Transform destination;
    public EnemyPath[] paths;
    public bool pickNearestPath;
    [Tooltip("목적지 Transform 주변에서 목표를 흩뿌릴 반경. Path의 waypointRadius와 같은 역할.")]
    public float destinationRadius = 4f;
    public float navMeshSampleDistance = 6f;

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

        GameObject instance = Instantiate(prefab, pos, Quaternion.identity);
        Target_Base target = instance.GetComponent<Target_Base>();
        if (target == null || target.isSample)
            return;

        target.SetMoveRoute(BuildRoute(pos), destination);
    }

    Vector3[] BuildRoute(Vector3 spawnPos)
    {
        EnemyPath path = PickPath(spawnPos);
        if (path != null && path.PointCount > 0)
            return AppendDestination(path.GetScatteredWorldPoints());

        if (destination != null &&
            TryBuildNavMeshRoute(spawnPos, destination.position, out Vector3[] navCorners))
        {
            return navCorners;
        }

        if (destination != null)
            return new[] { destination.position };

        return null;
    }

    EnemyPath PickPath(Vector3 spawnPos)
    {
        if (paths == null || paths.Length == 0)
            return null;

        EnemyPath nearest = null;
        float nearestDist = float.MaxValue;
        int validCount = 0;
        EnemyPath lastValid = null;

        for (int i = 0; i < paths.Length; i++)
        {
            EnemyPath path = paths[i];
            if (path == null || path.PointCount == 0)
                continue;

            validCount++;
            lastValid = path;

            float dist = Flatten(path.GetStartPosition() - spawnPos).sqrMagnitude;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = path;
            }
        }

        if (validCount == 0)
            return null;

        if (pickNearestPath)
            return nearest;

        int pick = Random.Range(0, validCount);
        int seen = 0;
        for (int i = 0; i < paths.Length; i++)
        {
            EnemyPath path = paths[i];
            if (path == null || path.PointCount == 0)
                continue;

            if (seen == pick)
                return path;
            seen++;
        }

        return lastValid;
    }

    Vector3[] AppendDestination(Vector3[] corners)
    {
        if (destination == null)
            return corners;

        Vector3 goal = EnemyPath.ScatterPoint(destination.position, destinationRadius);
        if (corners == null || corners.Length == 0)
            return new[] { goal };

        if (Flatten(corners[corners.Length - 1] - goal).sqrMagnitude < 0.25f)
            return corners;

        Vector3[] withGoal = new Vector3[corners.Length + 1];
        System.Array.Copy(corners, withGoal, corners.Length);
        withGoal[corners.Length] = goal;
        return withGoal;
    }

    bool TryBuildNavMeshRoute(Vector3 from, Vector3 to, out Vector3[] corners)
    {
        corners = null;

        if (!NavMesh.SamplePosition(from, out NavMeshHit startHit, navMeshSampleDistance, NavMesh.AllAreas))
            return false;

        if (!NavMesh.SamplePosition(to, out NavMeshHit endHit, navMeshSampleDistance, NavMesh.AllAreas))
            return false;

        NavMeshPath navPath = new NavMeshPath();
        if (!NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, navPath))
            return false;

        if (navPath.status == NavMeshPathStatus.PathInvalid)
            return false;

        if (navPath.corners == null || navPath.corners.Length == 0)
            return false;

        corners = navPath.corners;
        return true;
    }

    static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
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

        if (destination != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destination.position);
            Gizmos.DrawWireSphere(destination.position, 0.8f);
        }
    }
}
