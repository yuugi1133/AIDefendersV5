using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    [Tooltip("비우면 자식 Transform 순서를 웨이포인트로 사용합니다.")]
    public Transform[] waypoints;

    [Tooltip("길 옆으로 흩어질 최대 거리. 대상마다 한 차선을 골라 전 구간을 같은 간격으로 따라갑니다.")]
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

    public float RandomLaneOffset()
    {
        float radius = Mathf.Max(0f, waypointRadius);
        if (radius <= 0.01f)
            return 0f;

        return Random.Range(-radius, radius);
    }

    public Vector3[] GetLaneOffsetPoints(float laneOffset)
    {
        Vector3[] centers = GetWorldPoints();
        if (centers.Length == 0)
            return centers;

        if (Mathf.Abs(laneOffset) <= 0.01f)
            return centers;

        Vector3[] offset = new Vector3[centers.Length];
        for (int i = 0; i < centers.Length; i++)
        {
            Vector3 tangent = TangentAt(centers, i);
            Vector3 perp = new Vector3(-tangent.z, 0f, tangent.x);
            offset[i] = centers[i] + perp * laneOffset;
        }

        return offset;
    }

    static Vector3 TangentAt(Vector3[] points, int index)
    {
        Vector3 incoming = Vector3.zero;
        Vector3 outgoing = Vector3.zero;
        if (index > 0)
            incoming = Flatten(points[index] - points[index - 1]);
        if (index < points.Length - 1)
            outgoing = Flatten(points[index + 1] - points[index]);

        Vector3 tangent = incoming.normalized + outgoing.normalized;
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = incoming.sqrMagnitude > 0.0001f ? incoming : outgoing;
        if (tangent.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        tangent.y = 0f;
        return tangent.normalized;
    }

    static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
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
