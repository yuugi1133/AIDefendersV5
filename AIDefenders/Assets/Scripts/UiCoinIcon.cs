using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UiCoinIcon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Vector2 center = rectTransform.rect.center;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .46f;
        Disc(mesh, center + Vector2.down * 3, radius, new Color32(111, 61, 9, 255));
        Disc(mesh, center, radius, new Color32(255, 229, 111, 255));
        Disc(mesh, center, radius * .82f, new Color32(225, 135, 12, 255));
        Disc(mesh, center + Vector2.up * 1.5f, radius * .68f, new Color32(255, 199, 34, 255));
        Vector2 top = center + Vector2.up * radius * .44f;
        Vector2 bottom = center - Vector2.up * radius * .44f;
        int index = mesh.currentVertCount;
        Color gold = new Color32(255, 247, 184, 255);
        mesh.AddVert(top, gold, Vector2.zero);
        mesh.AddVert(center + Vector2.right * radius * .26f, gold, Vector2.zero);
        mesh.AddVert(bottom, gold, Vector2.zero);
        mesh.AddVert(center - Vector2.right * radius * .26f, gold, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }

    static void Disc(VertexHelper mesh, Vector2 center, float radius, Color color)
    {
        int first = mesh.currentVertCount;
        mesh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i <= 40; i++)
        {
            float a = i * Mathf.PI * 2 / 40;
            mesh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, Vector2.zero);
            if (i > 0) mesh.AddTriangle(first, first + i, first + i + 1);
        }
    }
}
