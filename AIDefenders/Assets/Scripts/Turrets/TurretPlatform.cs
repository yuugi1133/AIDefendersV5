using UnityEngine;

public class TurretPlatform : MonoBehaviour
{
    public bool occupied;
    public TurretController mountedTurret;
    public Vector3 mountOffset = Vector3.zero;

    public bool IsEmpty
    {
        get { return mountedTurret == null && !occupied; }
    }

    public Vector3 GetMountPosition()
    {
        Collider col = GetComponent<Collider>();
        Vector3 top = transform.position + Vector3.up * 0.5f;
        if (col != null)
            top = col.bounds.center + Vector3.up * col.bounds.extents.y;

        return top + transform.TransformVector(mountOffset);
    }

    public void Mount(TurretController turret)
    {
        mountedTurret = turret;
        occupied = turret != null;
    }
}
