using System.Collections.Generic;
using UnityEngine;

public class CCTVController : MonoBehaviour
{
    public AIInputCapture aiInput;  //AI캡쳐 스크립트

    public Camera cctvCamera;       //전장 촬영 카메라

    public LayerMask detectionMask;

    public TurretController[] turrets;

    [Header("YOLO Track")]
    public int enemyClassId = 0;
    public float minConfidence = 0.3f;
    public List<YoloSighting> enemies = new List<YoloSighting>();
    public List<YoloSighting> friendlies = new List<YoloSighting>();

    class YoloTrack
    {
        public Target_Base target;
        public bool asEnemy;
        public float confidence;
        public YoloTargetMarker marker;
    }

    readonly Dictionary<Target_Base, YoloTrack> tracks = new Dictionary<Target_Base, YoloTrack>();

    void Start()
    {
        if (aiInput != null)
            aiInput.OnDetectionReceived += HandleDetections;
    }

    public void RegisterTurret(TurretController turret)
    {
        if (turret == null)
            return;

        if (turrets == null)
            turrets = System.Array.Empty<TurretController>();

        for (int i = 0; i < turrets.Length; i++)
        {
            if (turrets[i] == turret)
                return;
        }

        System.Array.Resize(ref turrets, turrets.Length + 1);
        turrets[turrets.Length - 1] = turret;
    }

    public void UnregisterTurret(TurretController turret)
    {
        if (turret == null || turrets == null || turrets.Length == 0)
            return;

        int write = 0;
        for (int i = 0; i < turrets.Length; i++)
        {
            if (turrets[i] == null || turrets[i] == turret)
                continue;

            turrets[write] = turrets[i];
            write++;
        }

        if (write == turrets.Length)
            return;

        System.Array.Resize(ref turrets, write);
    }

    public bool TryGetIdentification(Target_Base target, out bool asEnemy)
    {
        asEnemy = false;
        if (target == null)
            return false;

        if (!tracks.TryGetValue(target, out YoloTrack track))
            return false;

        if (track.target == null || track.target.isDead)
            return false;

        asEnemy = track.asEnemy;
        return true;
    }

    void OnDestroy()
    {
        if (aiInput != null)
            aiInput.OnDetectionReceived -= HandleDetections;
    }

    void Update()
    {
        PruneDeadTracks();
        RebuildLists();
        CommandTurrets();
    }

    //포탑별로 YOLO가 적이라고 본 대상 중 사거리 안 가장 가까운 대상 찾기
    Transform FindClosestIdentifiedEnemy(TurretController turret)
    {
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (YoloSighting sighting in enemies)
        {
            if (sighting == null || sighting.target == null || sighting.target.isDead)
                continue;

            float dist = Vector3.Distance(
                turret.transform.position,
                sighting.target.transform.position
            );

            if (dist <= turret.range && dist < closestDist)
            {
                closestDist = dist;
                closest = sighting.target.transform;
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

        int mask = GetDetectionMask();

        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, mask);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Target_Base found = hit.collider.GetComponentInParent<Target_Base>();
            if (found == null || found.isDead || found.isSample)
                continue;

            targetbase = found;
            target = found.transform;
            return true;
        }

        return false;
    }

    int GetDetectionMask()
    {
        int mask = detectionMask.value;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int friendlyLayer = LayerMask.NameToLayer("Friendly");

        if (mask == 0)
        {
            if (enemyLayer >= 0)
                mask |= 1 << enemyLayer;
            if (friendlyLayer >= 0)
                mask |= 1 << friendlyLayer;
            if (mask == 0)
                mask = Physics.DefaultRaycastLayers;
            return mask;
        }

        if (enemyLayer >= 0)
            mask |= 1 << enemyLayer;
        if (friendlyLayer >= 0)
            mask |= 1 << friendlyLayer;
        return mask;
    }

    // YOLO 탐지 결과를 태그 정답이 아니라 모델 판단으로 적/아군 리스트에 넣는다.
    private void HandleDetections(DetectionResponse response)
    {
        if (response == null || response.detections == null)
            return;

        foreach (var detection in response.detections)
        {
            if (detection.confidence < minConfidence)
                continue;

            if (cctvCamera == null)
                continue;

            Vector2 screenPos = new Vector2(
                    detection.center_x,
                    cctvCamera.pixelHeight
                    - detection.center_y
                );

            Ray ray = cctvCamera.ScreenPointToRay(screenPos);
            Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);

            if (!TryFindTargetAlongRay(ray, out Target_Base targetbase, out Transform target))
                continue;

            bool asEnemy = detection.class_id == enemyClassId;
            UpsertTrack(targetbase, asEnemy, detection.confidence);
        }

        PruneDeadTracks();
        RebuildLists();
    }

    void UpsertTrack(Target_Base target, bool asEnemy, float confidence)
    {
        if (tracks.TryGetValue(target, out YoloTrack track))
        {
            if (confidence <= track.confidence)
                return;
        }
        else
        {
            track = new YoloTrack { target = target };
            tracks[target] = track;
        }

        track.asEnemy = asEnemy;
        track.confidence = confidence;
        EnsureMarker(track);
    }

    void EnsureMarker(YoloTrack track)
    {
        if (track.target == null)
            return;

        if (track.marker == null)
            track.marker = track.target.GetComponent<YoloTargetMarker>();

        if (track.marker == null)
            track.marker = track.target.gameObject.AddComponent<YoloTargetMarker>();

        track.marker.enabled = true;
        track.marker.SetClassifiedAsEnemy(track.asEnemy, track.confidence);
    }

    void PruneDeadTracks()
    {
        List<Target_Base> remove = null;
        foreach (KeyValuePair<Target_Base, YoloTrack> pair in tracks)
        {
            YoloTrack track = pair.Value;
            if (track.target != null && !track.target.isDead)
                continue;

            if (track.marker != null)
                Destroy(track.marker);

            if (remove == null)
                remove = new List<Target_Base>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            tracks.Remove(remove[i]);
    }

    void RebuildLists()
    {
        enemies.Clear();
        friendlies.Clear();

        foreach (YoloTrack track in tracks.Values)
        {
            if (track.target == null || track.target.isDead)
                continue;

            if (track.asEnemy)
            {
                enemies.Add(new YoloSighting
                {
                    target = track.target,
                    confidence = track.confidence
                });
            }
            else
            {
                friendlies.Add(new YoloSighting
                {
                    target = track.target,
                    confidence = track.confidence
                });
            }
        }
    }

    void CommandTurrets()
    {
        if (turrets == null)
            return;

        foreach (TurretController turret in turrets)
        {
            if (turret == null || turret.firePoint == null)
                continue;

            Transform target = FindClosestIdentifiedEnemy(turret);
            if (target == null)
                continue;

            Vector3 aimPoint = CalculateInterceptPoint(turret, target);
            turret.SetTarget(target, aimPoint);

            Debug.DrawLine(
                turret.transform.position,
                aimPoint,
                Color.green
            );
        }
    }
}
