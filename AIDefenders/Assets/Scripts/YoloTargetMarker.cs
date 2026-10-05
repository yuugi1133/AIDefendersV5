using UnityEngine;

public class YoloTargetMarker : MonoBehaviour
{
    public bool classifiedAsEnemy;
    public float confidence;
    public Color enemyColor = new Color(1f, 0.2f, 0.15f, 1f);
    public Color friendlyColor = new Color(0.2f, 0.85f, 1f, 1f);

    static Material lineMaterial;
    Target_Base target;

    void Awake()
    {
        target = GetComponent<Target_Base>();
    }

    public void SetClassifiedAsEnemy(bool asEnemy, float confidence)
    {
        classifiedAsEnemy = asEnemy;
        this.confidence = confidence;
    }

    void OnRenderObject()
    {
        if (!TryGetBounds(out Bounds bounds))
            return;

        Color color = classifiedAsEnemy ? enemyColor : friendlyColor;
        if (!EnsureMaterial())
            return;

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.LINES);
        GL.Color(color);
        DrawBounds(bounds);
        GL.End();
        GL.PopMatrix();
    }

    void OnGUI()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.enabled)
            return;

        if (!TryGetBounds(out Bounds bounds))
            return;

        Vector3 world = bounds.center + Vector3.up * (bounds.extents.y + 0.7f);
        Vector3 screen = camera.WorldToScreenPoint(world);
        if (screen.z <= 0.1f)
            return;

        float x = screen.x;
        float y = Screen.height - screen.y;
        Color color = classifiedAsEnemy ? enemyColor : friendlyColor;
        DrawArrow(x, y, 16f, color);

        string label = classifiedAsEnemy
            ? $"E {confidence:0.00}"
            : $"F {confidence:0.00}";
        GUI.color = color;
        GUI.Label(new Rect(x - 28f, y - 28f, 72f, 20f), label);
        GUI.color = Color.white;
    }

    bool TryGetBounds(out Bounds bounds)
    {
        bounds = default;
        Renderer mesh = target != null ? target.getRenderer() : null;
        if (mesh == null)
            mesh = GetComponentInChildren<Renderer>();

        if (mesh == null)
            return false;

        bounds = mesh.bounds;
        return true;
    }

    static void DrawBounds(Bounds bounds)
    {
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;
        Vector3[] p = new Vector3[8];
        p[0] = c + new Vector3(-e.x, -e.y, -e.z);
        p[1] = c + new Vector3(e.x, -e.y, -e.z);
        p[2] = c + new Vector3(e.x, -e.y, e.z);
        p[3] = c + new Vector3(-e.x, -e.y, e.z);
        p[4] = c + new Vector3(-e.x, e.y, -e.z);
        p[5] = c + new Vector3(e.x, e.y, -e.z);
        p[6] = c + new Vector3(e.x, e.y, e.z);
        p[7] = c + new Vector3(-e.x, e.y, e.z);

        DrawEdge(p[0], p[1]);
        DrawEdge(p[1], p[2]);
        DrawEdge(p[2], p[3]);
        DrawEdge(p[3], p[0]);
        DrawEdge(p[4], p[5]);
        DrawEdge(p[5], p[6]);
        DrawEdge(p[6], p[7]);
        DrawEdge(p[7], p[4]);
        DrawEdge(p[0], p[4]);
        DrawEdge(p[1], p[5]);
        DrawEdge(p[2], p[6]);
        DrawEdge(p[3], p[7]);
    }

    static void DrawEdge(Vector3 a, Vector3 b)
    {
        GL.Vertex(a);
        GL.Vertex(b);
    }

    static void DrawArrow(float x, float y, float size, Color color)
    {
        Texture2D tex = Texture2D.whiteTexture;
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(x - size * 0.5f, y, size, size * 0.35f), tex);
        GUI.DrawTexture(new Rect(x - size * 0.22f, y + size * 0.3f, size * 0.44f, size * 0.55f), tex);
        GUI.color = previous;
    }

    static bool EnsureMaterial()
    {
        if (lineMaterial != null)
            return true;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return false;

        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        return true;
    }
}
