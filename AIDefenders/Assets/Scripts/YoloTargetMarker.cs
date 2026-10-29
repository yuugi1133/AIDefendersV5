using UnityEngine;

public class YoloTargetMarker : MonoBehaviour
{
    public bool classifiedAsEnemy;
    public float confidence;
    bool announced;
    public Color enemyColor = new Color(1f, 0.2f, 0.15f, 1f);
    public Color friendlyColor = new Color(0.2f, 0.85f, 1f, 1f);

    Target_Base target;

    public Target_Base Target
    {
        get { return target; }
    }

    void Awake()
    {
        target = GetComponent<Target_Base>();
    }

    public void SetClassifiedAsEnemy(bool asEnemy, float confidence)
    {
        bool first = !announced;
        classifiedAsEnemy = asEnemy;
        this.confidence = confidence;
        if (!first)
            return;

        announced = true;
        GameSfx.PlayIdentified();
    }

    public bool TryGetWorldBounds(out Bounds bounds)
    {
        if (target != null && target.TryGetRenderBounds(out bounds))
            return true;

        bounds = default;
        Renderer[] children = GetComponentsInChildren<Renderer>();
        bool found = false;
        for (int i = 0; i < children.Length; i++)
        {
            Renderer mesh = children[i];
            if (mesh == null || !mesh.enabled)
                continue;
            if (!(mesh is MeshRenderer) && !(mesh is SkinnedMeshRenderer))
                continue;

            if (!found)
            {
                bounds = mesh.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(mesh.bounds);
            }
        }

        return found;
    }
}
