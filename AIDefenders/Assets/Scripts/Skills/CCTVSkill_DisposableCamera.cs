using UnityEngine;

public class CCTVSkill_DisposableCamera : CCTVActiveSkill
{
    public GameObject dronePrefab;
    public float duration = 30f;
    public float spawnForward = 2.4f;
    public float droneScale = 0.4f;
    public SfxEvent deploySfx;

    protected override bool CanUse(CCTVSkillContext context)
    {
        return dronePrefab != null;
    }

    protected override void OnUse(CCTVSkillContext context)
    {
        Transform origin = context != null && context.Origin != null
            ? context.Origin
            : transform;
        Camera view = context != null ? context.ViewCamera : null;
        Camera captureTemplate = context != null && context.Cctv != null
            ? context.Cctv.cctvCamera
            : null;

        Vector3 position = origin.position;
        Quaternion rotation = origin.rotation;
        if (view != null)
        {
            position = view.transform.position;
            rotation = view.transform.rotation;
        }

        position += rotation * Vector3.forward * spawnForward;

        GameObject spawned = Instantiate(dronePrefab, position, rotation);
        spawned.transform.localScale = Vector3.one * Mathf.Max(0.05f, droneScale);

        Rigidbody body = spawned.GetComponent<Rigidbody>();
        if (body == null)
            body = spawned.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezeAll;

        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        Collider[] colliders = spawned.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
                continue;
            colliders[i].isTrigger = true;
            if (ignoreRaycast >= 0)
                colliders[i].gameObject.layer = ignoreRaycast;
        }

        DisposableCameraDrone drone = spawned.GetComponent<DisposableCameraDrone>();
        if (drone == null)
            drone = spawned.AddComponent<DisposableCameraDrone>();

        AIInputCapture input = context != null && context.Cctv != null
            ? context.Cctv.aiInput
            : FindFirstObjectByType<AIInputCapture>();
        drone.Activate(input, duration, captureTemplate);
        GameSfx.PlayWorld(deploySfx, position);
    }
}
