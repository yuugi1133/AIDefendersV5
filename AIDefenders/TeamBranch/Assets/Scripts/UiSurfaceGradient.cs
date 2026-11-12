using UnityEngine;
using UnityEngine.UI;

public class UiSurfaceGradient : BaseMeshEffect
{
    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || mesh.currentVertCount == 0) return;
        UIVertex v = new UIVertex();
        float low = float.MaxValue, high = float.MinValue;
        for (int i = 0; i < mesh.currentVertCount; i++)
        {
            mesh.PopulateUIVertex(ref v, i);
            low = Mathf.Min(low, v.position.y);
            high = Mathf.Max(high, v.position.y);
        }
        for (int i = 0; i < mesh.currentVertCount; i++)
        {
            mesh.PopulateUIVertex(ref v, i);
            Color color = v.color;
            float t = Mathf.InverseLerp(low, high, v.position.y);
            float light = t > .52f ? 1.06f : .84f + t * .22f;
            color.r *= light; color.g *= light; color.b *= light;
            v.color = color;
            mesh.SetUIVertex(v, i);
        }
    }
}
