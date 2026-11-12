using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UiHeartIcon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Heart(mesh, 1f, new Vector2(0, -2), new Color32(85, 19, 32, 255), new Color32(85, 19, 32, 255));
        Heart(mesh, .88f, Vector2.zero, new Color32(255, 145, 127, 255), new Color32(244, 40, 60, 255));
    }

    void Heart(VertexHelper mesh, float scale, Vector2 offset, Color top, Color bottom)
    {
        Rect rect = rectTransform.rect;
        int first = mesh.currentVertCount;
        mesh.AddVert(rect.center + offset, bottom, Vector2.zero);
        for (int i = 0; i <= 64; i++)
        {
            float t = i * Mathf.PI * 2 / 64;
            float x = 16 * Mathf.Pow(Mathf.Sin(t), 3) / 34f;
            float y = (13 * Mathf.Cos(t) - 5 * Mathf.Cos(2*t) - 2 * Mathf.Cos(3*t) - Mathf.Cos(4*t) + 2) / 32f;
            Vector2 point = rect.center + offset + new Vector2(x * rect.width, y * rect.height) * scale;
            mesh.AddVert(point, Color.Lerp(bottom, top, Mathf.Clamp01(y + .5f)), Vector2.zero);
            if (i > 0) mesh.AddTriangle(first, first + i, first + i + 1);
        }
    }
}
