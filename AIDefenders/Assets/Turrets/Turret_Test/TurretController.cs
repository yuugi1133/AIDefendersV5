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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // CCTV의 지시를 받아 호출
    public void SetTarget(Transform target, Vector3 aimPoint)
    {
        currentTarget = target;
        currentAimPoint = aimPoint;
    }

    void Update()
    {
        if (currentTarget == null)
            return;

        AimAndShoot();
    }

    void AimAndShoot()
    {
        Vector3 dir = (currentAimPoint - firePoint.position).normalized;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );

        float angle = Vector3.Angle(transform.forward, dir);

        if (angle <= fireAngleThreshold)
        {
            Vector3 velocity = dir * weapon.ammo.bulletSpeed;
            weapon.Fire(firePoint.position, velocity);
        }
    }

    /* 이제 더이상 포탑이 스스로 적을 찾지 못합니다. 자세한 사항은 CCTVController 클래스에게 문의하십시오.
    void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);

            if (dist < shortestDistance && dist <= range)
            {
                shortestDistance = dist;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null)
            target = nearestEnemy.transform;
        else
            target = null;
    }
    */

    /* 더이상 예측 조준도 스스로 하지 않습니다. 자세한 사항은 CCTVController 클래스에게 문의하십시오.
    Vector3? CalculateIntercept()
    {
        Vector3 targetPos = target.position;
        Rigidbody targetRb = target.GetComponent<Rigidbody>();

        Vector3 targetVel = targetRb != null ? targetRb.linearVelocity : Vector3.zero;

        Vector3 shooterPos = firePoint.position;

        float speed = weapon.ammo.bulletSpeed;

        Vector3 dir = targetPos - shooterPos;

        float a = Vector3.Dot(targetVel, targetVel) - speed * speed;
        float b = 2f * Vector3.Dot(dir, targetVel);
        float c = Vector3.Dot(dir, dir);

        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
            return dir.normalized; // �׳� ���� ���� fallback

        float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

        float t = Mathf.Min(t1, t2);
        if (t < 0) t = Mathf.Max(t1, t2);
        if (t < 0) return dir.normalized;

        Vector3 futurePos = targetPos + targetVel * t;

        return (futurePos - shooterPos).normalized;
    }
    */

    /*포탑 회전
    void RotateTurret(Vector3 dir)
    {
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );
    }
    */
}
