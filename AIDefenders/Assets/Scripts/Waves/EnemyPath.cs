using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    [Tooltip("비우면 자식 Transform 순서를 웨이포인트로 사용합니다.")]
    public Transform[] waypoints;

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

    public Vector3[] GetWorldPoints(float lateralOffset = 0f)
    {
        Transform[] points = GetWaypointTransforms();
        if (points == null || points.Length == 0)
            return System.Array.Empty<Vector3>();

        Vector3[] world = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            world[i] = points[i].position;

        if (Mathf.Abs(lateralOffset) < 0.01f)
            return world;

        for (int i = 0; i < world.Length; i++)
        {
            Vector3 dir;
            if (i < world.Length - 1)
                dir = world[i + 1] - world[i];
            else
                dir = world[i] - world[i - 1];

            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                continue;

            Vector3 right = Vector3.Cross(Vector3.up, dir.normalized);
            world[i] += right * lateralOffset;
        }

        return world;
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
            Gizmos.DrawSphere(points[i], 0.35f);
            if (i < points.Length - 1)
                Gizmos.DrawLine(points[i], points[i + 1]);
        }
    }
}
