using UnityEngine;

public class CCTVController : MonoBehaviour
{
    public AIInputCapture aiInput;  //AI캡쳐 스크립트

    public Camera cctvCamera;       //전장 촬영 카메라

    public LayerMask detectionMask;

    public TurretController[] turrets;
    GameObject[] enemies;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        aiInput.OnDetectionReceived += HandleDetections;
    }

    void OnDestroy()
    {
        aiInput.OnDetectionReceived -= HandleDetections;
    }

    // Update is called once per frame
    void Update()
    {
        /*-------------월드 기반 자동 조준(더이상 사용하지 않음.)------------
        enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (TurretController turret in turrets)
        {
            Transform target = FindClosestEnemyInRange(turret, enemies);

            if (target == null)
                continue;

            Vector3 aimPoint = CalculateInterceptPoint(turret, target);

            turret.SetTarget(target, aimPoint);

            Debug.DrawLine(turret.firePoint.position, aimPoint, Color.red);
        }
        */
    }

    //포탑별로 해당 포탑과 가장 가까운 대상 찾기
    Transform FindClosestEnemyInRange(TurretController turret, GameObject[] enemies)
    {
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(
                turret.transform.position,
                enemy.transform.position
            );

            if (dist <= turret.range && dist < closestDist)
            {
                closestDist = dist;
                closest = enemy.transform;
            }
        }

        return closest;
    }

    // 포탑별로 대상 사격을 위한 예측 조준 위치를 계산 후 반환 
    Vector3 CalculateInterceptPoint(TurretController turret, Transform target)
    {
        Rigidbody rb = target.GetComponentInParent<Rigidbody>();

        Vector3 targetPos = target.position;
        Vector3 targetVel = rb != null ? rb.linearVelocity : Vector3.zero;

        Vector3 turretPos = turret.firePoint.position;

        if (turret.weapon == null || !turret.weapon.UsesProjectileLead)
            return targetPos;

        float speed = turret.weapon.GetMuzzleSpeed();
        if (speed <= 0.01f)
            return targetPos;

        Vector3 dir = targetPos - turretPos;

        float a = Vector3.Dot(targetVel, targetVel) - speed * speed;
        float b = 2f * Vector3.Dot(dir, targetVel);
        float c = Vector3.Dot(dir, dir);

        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
            return targetPos;

        float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

        float t = Mathf.Min(t1, t2);

        if (t < 0)
            t = Mathf.Max(t1, t2);

        if (t < 0)
            return targetPos;

        return targetPos + targetVel * t;
    }

    bool TryFindTargetAlongRay(Ray ray, out Target_Base targetbase, out Transform target)
    {
        targetbase = null;
        target = null;

        int mask = detectionMask.value;
        if (mask == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            mask = enemyLayer >= 0
                ? (1 << enemyLayer)
                : Physics.DefaultRaycastLayers;
        }

        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, mask);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Target_Base found = hit.collider.GetComponentInParent<Target_Base>();
            if (found == null || found.isDead)
                continue;

            if (found.CompareTag("Enemy") == false)
                continue;

            targetbase = found;
            target = found.transform;
            return true;
        }

        return false;
    }

    //탐지한 적 json 데이터 처리
    private void HandleDetections(DetectionResponse response)
    {
        if (response == null)
            return;

        if (response.detections == null)
            return;

        foreach (var detection in response.detections)
        {
            // 적 클래스만 처리
            if (detection.class_id != 0)
                continue;

            // 신뢰도 필터 (0.5이상인 것만 사격)
            if (detection.confidence < 0.5f)
                continue;

            // YOLO 이미지 내의 좌표를 Unity 좌표로 변환(주의: y축은 반전해야 한다.)
            Vector2 screenPos = new Vector2(
                    detection.center_x,
                    cctvCamera.pixelHeight
                    - detection.center_y
                );

            Ray ray = cctvCamera.ScreenPointToRay(screenPos);
            Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);

            if (!TryFindTargetAlongRay(ray, out Target_Base targetbase, out Transform target))
                continue;

            if (turrets == null)
                continue;

            foreach (TurretController turret in turrets)
            {
                if (turret == null || turret.firePoint == null)
                    continue;

                float distance =
                    Vector3.Distance(turret.transform.position, target.position);

                if (distance > turret.range)
                    continue;

                Vector3 aimPoint = CalculateInterceptPoint(turret, target);
                turret.SetTarget(target, aimPoint);

                Debug.DrawLine(
                    turret.transform.position,
                    aimPoint,
                    Color.green,
                    1f
                );
            }
        }
    }
}
