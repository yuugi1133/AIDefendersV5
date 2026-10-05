using UnityEngine;

public class CCTVRadar : MonoBehaviour
{
    public float range = 45f;
    public float size = 168f;
    public float margin = 16f;
    public float blipSize = 7f;

    public Color backgroundColor = new Color(0.05f, 0.08f, 0.08f, 0.72f);
    public Color borderColor = new Color(0.55f, 0.85f, 0.7f, 0.9f);
    public Color playerColor = new Color(1f, 1f, 1f, 1f);
    public Color unknownColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    public Color enemyColor = new Color(0.95f, 0.18f, 0.14f, 1f);
    public Color friendlyColor = new Color(0.2f, 0.45f, 1f, 1f);

    WaveDirector waveDirector;
    CCTVController cctv;

    void Start()
    {
        waveDirector = FindFirstObjectByType<WaveDirector>();
        cctv = GetComponent<CCTVController>();
        if (cctv == null)
            cctv = FindFirstObjectByType<CCTVController>();
    }

    void OnGUI()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();

        if (waveDirector == null || waveDirector.Phase != WavePhase.Combat)
            return;

        Rect radar = new Rect(
            Screen.width - size - margin,
            margin,
            size,
            size
        );

        DrawQuad(radar, backgroundColor);
        DrawBorder(radar, 2f, borderColor);

        Vector2 center = new Vector2(radar.x + radar.width * 0.5f, radar.y + radar.height * 0.5f);
        float radius = radar.width * 0.5f - 6f;

        Target_Base[] targets = FindObjectsByType<Target_Base>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        Vector3 origin = transform.position;
        float yaw = transform.eulerAngles.y;
        Quaternion toRadar = Quaternion.Euler(0f, -yaw, 0f);

        for (int i = 0; i < targets.Length; i++)
        {
            Target_Base target = targets[i];
            if (target == null || target.isDead || target.isSample)
                continue;

            Vector3 delta = target.transform.position - origin;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f)
                continue;

            Vector3 local = toRadar * delta;
            Vector2 n = new Vector2(local.x / range, local.z / range);
            if (n.sqrMagnitude > 1f)
                n.Normalize();

            Vector2 point = new Vector2(
                center.x + n.x * radius,
                center.y - n.y * radius
            );

            Color color = unknownColor;
            if (cctv != null && cctv.TryGetIdentification(target, out bool asEnemy))
                color = asEnemy ? enemyColor : friendlyColor;

            DrawQuad(
                new Rect(point.x - blipSize * 0.5f, point.y - blipSize * 0.5f, blipSize, blipSize),
                color
            );
        }

        float playerSize = blipSize + 2f;
        DrawQuad(
            new Rect(center.x - playerSize * 0.5f, center.y - playerSize * 0.5f, playerSize, playerSize),
            playerColor
        );
    }

    static void DrawBorder(Rect rect, float thickness, Color color)
    {
        DrawQuad(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawQuad(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawQuad(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawQuad(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    static void DrawQuad(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
