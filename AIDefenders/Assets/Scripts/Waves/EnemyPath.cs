using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    [Tooltip("비우면 자식 Transform 순서를 웨이포인트로 사용합니다.")]
    public Transform[] waypoints;

    [Tooltip("각 웨이포인트에서 대상이 고를 무작위 목표의 반경.")]
    public float waypointRadius = 4f;

    public Color gizmoColor = new Color(0.2f, 0.85f, 1f, 0.9f);

    public int PointCount
    {
        get
        {
            Transform[] points = GetWaypointTransforms();
            return points != null ? points.Length : 0;
        }
    }

    public Vector3 GetStartPosition()
    {
        Transform[] points = GetWaypointTransforms();
        if (points == null || points.Length == 0)
            return transform.position;

        return points[0].position;
    }

    public Vector3[] GetWorldPoints()
    {
        Transform[] points = GetWaypointTransforms();
        if (points == null || points.Length == 0)
            return System.Array.Empty<Vector3>();

        Vector3[] world = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            world[i] = points[i].position;
        return world;
    }

    public Vector3[] GetScatteredWorldPoints()
    {
        Vector3[] centers = GetWorldPoints();
        if (centers.Length == 0)
            return centers;

        float radius = Mathf.Max(0f, waypointRadius);
        Vector3[] scattered = new Vector3[centers.Length];
        for (int i = 0; i < centers.Length; i++)
            scattered[i] = ScatterPoint(centers[i], radius);
        return scattered;
    }

    public static Vector3 ScatterPoint(Vector3 center, float radius)
    {
        if (radius <= 0.01f)
            return center;

        Vector2 offset = Random.insideUnitCircle * radius;
        return center + new Vector3(offset.x, 0f, offset.y);
    }

    Transform[] GetWaypointTransforms()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            int count = 0;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                    count++;
            }

            if (count == 0)
                return System.Array.Empty<Transform>();

            Transform[] filtered = new Transform[count];
            int write = 0;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                    filtered[write++] = waypoints[i];
            }

            return filtered;
        }

        int childCount = transform.childCount;
        if (childCount == 0)
            return System.Array.Empty<Transform>();

        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            children[i] = transform.GetChild(i);

        return children;
    }

    void OnDrawGizmos()
    {
        Vector3[] points = GetWorldPoints();
        if (points.Length == 0)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            return;
        }

        Gizmos.color = gizmoColor;
        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.DrawSphere(points[i], 0.25f);
            DrawRadius(points[i], waypointRadius);
            if (i < points.Length - 1)
                Gizmos.DrawLine(points[i], points[i + 1]);
        }
    }

    void DrawRadius(Vector3 center, float radius)
    {
        if (radius <= 0.01f)
            return;

        const int segments = 24;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
