using UnityEngine;
using UnityEngine.Rendering;

public class TurretRangeRing
{
    const int Segments = 64;
    const float Height = 0.12f;

    GameObject root;
    MeshFilter filter;
    MeshRenderer meshRenderer;
    Mesh mesh;
    Material material;
    float lastRadius = -1f;

    public void Show(Vector3 center, float radius, Color color)
    {
        if (radius < 0.2f)
        {
            Hide();
            return;
        }

        Ensure();
        root.SetActive(true);
        root.transform.position = new Vector3(center.x, center.y + Height, center.z);

        if (Mathf.Abs(radius - lastRadius) > 0.05f)
        {
            BuildAnnulus(radius);
            lastRadius = radius;
        }

        ApplyColor(color);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Dispose()
    {
        if (root != null)
            Object.Destroy(root);
        if (mesh != null)
            Object.Destroy(mesh);
        if (material != null)
            Object.Destroy(material);

        root = null;
        filter = null;
        meshRenderer = null;
        mesh = null;
        material = null;
        lastRadius = -1f;
    }

    void Ensure()
    {
        if (root != null)
            return;

        root = new GameObject("TurretRangeRing");
        filter = root.AddComponent<MeshFilter>();
        meshRenderer = root.AddComponent<MeshRenderer>();
        mesh = new Mesh();
        mesh.name = "TurretRangeRingMesh";
        filter.sharedMesh = mesh;
        material = CreateMaterial();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    static Material CreateMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material created = new Material(shader);
        created.renderQueue = 3000;
        return created;
    }

    void ApplyColor(Color color)
    {
        color.a = 0.85f;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        material.color = color;
    }

    void BuildAnnulus(float radius)
    {
        float thickness = Mathf.Clamp(radius * 0.045f, 0.5f, 1.4f);
        float inner = Mathf.Max(0.2f, radius - thickness);
        int vertCount = (Segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uv = new Vector2[vertCount];
        int[] triangles = new int[Segments * 6];

        for (int i = 0; i <= Segments; i++)
        {
            float t = (i / (float)Segments) * Mathf.PI * 2f;
            float cos = Mathf.Cos(t);
            float sin = Mathf.Sin(t);
            vertices[i * 2] = new Vector3(cos * inner, 0f, sin * inner);
            vertices[i * 2 + 1] = new Vector3(cos * radius, 0f, sin * radius);
            uv[i * 2] = new Vector2(0f, i / (float)Segments);
            uv[i * 2 + 1] = new Vector2(1f, i / (float)Segments);
        }

        int tri = 0;
        for (int i = 0; i < Segments; i++)
        {
            int i0 = i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;
            triangles[tri++] = i0;
            triangles[tri++] = i2;
            triangles[tri++] = i1;
            triangles[tri++] = i1;
            triangles[tri++] = i2;
            triangles[tri++] = i3;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
