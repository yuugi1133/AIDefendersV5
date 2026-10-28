using System.Collections.Generic;
using UnityEngine;

public class CCTVController : MonoBehaviour
{
    public AIInputCapture aiInput;  //AI캡쳐 스크립트

    public Camera cctvCamera;       //전장 촬영 카메라

    public LayerMask detectionMask;

    public TurretController[] turrets;

    [Header("YOLO Track")]
    public float minConfidence = 0.3f;
    public List<YoloSighting> enemies = new List<YoloSighting>();
    public List<YoloSighting> friendlies = new List<YoloSighting>();

    PythonRuntime.YoloClassMap classMap;

    class YoloTrack
    {
        public Target_Base target;
        public bool asEnemy;
        public float confidence;
        public bool locked;
        public YoloTargetMarker marker;
    }

    readonly Dictionary<Target_Base, YoloTrack> tracks = new Dictionary<Target_Base, YoloTrack>();

    void Start()
    {
        if (aiInput != null)
            aiInput.OnDetectionReceived += HandleDetections;
    }

    public void ApplyYoloClassMap(PythonRuntime.YoloClassMap map)
    {
        classMap = map;
        tracks.Clear();
        enemies.Clear();
        friendlies.Clear();
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
        return TryGetIdentification(target, out asEnemy, out _);
    }

    public bool TryGetIdentification(Target_Base target, out bool asEnemy, out float confidence)
    {
        asEnemy = false;
        confidence = 0f;
        if (target == null)
            return false;

        if (!tracks.TryGetValue(target, out YoloTrack track))
            return false;

        if (track.target == null)
            return false;

        asEnemy = track.asEnemy;
        confidence = track.confidence;
        return true;
    }

    public void ForceIdentify(Target_Base target, bool asEnemy)
    {
        if (target == null || target.isDead || target.isSample)
            return;

        YoloTrack track;
        if (!tracks.TryGetValue(target, out track))
        {
            track = new YoloTrack { target = target };
            tracks[target] = track;
        }

        track.asEnemy = asEnemy;
        track.confidence = 1f;
        track.locked = true;
        EnsureMarker(track);
        RebuildLists();
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

            bool asEnemy;
            if (classMap == null || !classMap.TryIsEnemy(detection.class_id, out asEnemy))
                continue;

            UpsertTrack(targetbase, asEnemy, detection.confidence);
        }

        PruneDeadTracks();
        RebuildLists();
    }

    void UpsertTrack(Target_Base target, bool asEnemy, float confidence)
    {
        if (tracks.TryGetValue(target, out YoloTrack track))
        {
            if (track.locked || confidence <= track.confidence)
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

}
