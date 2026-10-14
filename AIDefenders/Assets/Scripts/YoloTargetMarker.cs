using UnityEngine;

public class YoloTargetMarker : MonoBehaviour
{
    public bool classifiedAsEnemy;
    public float confidence;
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
        classifiedAsEnemy = asEnemy;
        this.confidence = confidence;
    }

    public bool TryGetWorldBounds(out Bounds bounds)
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
}
