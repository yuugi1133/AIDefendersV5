using UnityEngine;

public class IdentificationDart : Projectile_Base
{
    CCTVController cctv;
    bool asEnemy;

    public void Init(
        Vector3 initialVelocity,
        float gravityValue,
        float life,
        CCTVController controller,
        bool markAsEnemy,
        Transform ignore)
    {
        cctv = controller;
        asEnemy = markAsEnemy;
        Launch(initialVelocity, gravityValue, life, 0f, ignore);
    }

    protected override void OnLaunched()
    {
        ApplyTint();
    }

    protected override void OnHitTarget(Target_Base target, Vector3 point)
    {
        if (target != null && cctv != null)
            cctv.ForceIdentify(target, asEnemy);
    }

    void ApplyTint()
    {
        Renderer mesh = GetComponent<Renderer>();
        if (mesh == null)
            return;

        Color color = asEnemy
            ? new Color(0.95f, 0.2f, 0.15f, 1f)
            : new Color(0.2f, 0.55f, 1f, 1f);
        mesh.material.color = color;
    }
}
