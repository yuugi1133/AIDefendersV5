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
        Rigidbody rb = target.GetComponent<Rigidbody>();

        Vector3 targetPos = target.position;
        Vector3 targetVel = rb.linearVelocity;

        Vector3 turretPos = turret.firePoint.position;

        float speed = turret.weapon.ammo.bulletSpeed;

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

            // 적의 위치로 Ray 발사! (해당 Ray는 적의 collision를 맞추려고 한다.)
            Ray ray =cctvCamera.ScreenPointToRay(screenPos);

            Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);

            // 발사한 Ray가 적을 맞추고 난 후 처리 (맞은 콜라이더 위치를 포탑에게 전달하여 공격하도록 지시)
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Transform target = hit.collider.transform;
                Target_Base targetbase = hit.collider.GetComponentInParent<Target_Base>();

                if (targetbase.isDead)
                    continue;

                // 모든 포탑에 전달
                foreach (TurretController turret in turrets)
                {
                    float distance =
                        Vector3.Distance(turret.transform.position, target.position);

                    if (distance >turret.range)
                        continue;

                    Vector3 aimPoint =
                        CalculateInterceptPoint(
                            turret,
                            target
                        );

                    turret.SetTarget(
                        target,
                        aimPoint
                    );

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
}
