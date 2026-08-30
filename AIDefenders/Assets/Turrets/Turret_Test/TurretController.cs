using UnityEngine;

public class TurretController : MonoBehaviour
{
    public Transform firePoint;
    public WeaponSystem weapon;

    public float rotationSpeed = 5f;
    public float fireAngleThreshold = 3f;   //발사를 위한 최소 조준각
    public float range = 15f;

    Transform currentTarget;
    Vector3 currentAimPoint;

    public void SetTarget(Transform target, Vector3 aimPoint)
    {
        currentTarget = target;
        currentAimPoint = aimPoint;
    }

    void Update()
    {
        if (currentTarget == null)
            return;

        if (weapon == null || firePoint == null)
            return;

        AimAndShoot();
    }

    void AimAndShoot()
    {
        Vector3 dir = (currentAimPoint - firePoint.position).normalized;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );

        float angle = Vector3.Angle(transform.forward, dir);

        if (angle <= fireAngleThreshold)
            weapon.Fire(firePoint.position, dir, currentTarget);
    }
}
