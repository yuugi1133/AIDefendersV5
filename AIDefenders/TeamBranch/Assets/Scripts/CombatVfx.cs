using UnityEngine;

public static class CombatVfx
{
    const float MuzzleLife = 0.28f;
    const float ImpactLife = 0.7f;
    const float BeamImpactLife = 0.25f;

    public static GameObject PlayMuzzle(GameObject prefab, Vector3 position, Vector3 forward)
    {
        return Play(prefab, position, forward, MuzzleLife);
    }

    public static GameObject PlayImpact(GameObject prefab, Vector3 position, Vector3 forward)
    {
        return Play(prefab, position, forward, ImpactLife);
    }

    public static GameObject PlayBeamImpact(GameObject prefab, Vector3 position, Vector3 forward)
    {
        return Play(prefab, position, forward, BeamImpactLife);
    }

    public static GameObject Play(GameObject prefab, Vector3 position, Vector3 forward, float maxLife)
    {
        if (prefab == null)
            return null;

        Quaternion rotation = forward.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(forward.normalized)
            : Quaternion.identity;
        return Play(prefab, position, rotation, null, maxLife);
    }

    public static GameObject Play(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        float maxLife)
    {
        if (prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, position, rotation, parent);
        PrepareOneShot(instance);
        Object.Destroy(instance, Mathf.Max(0.05f, maxLife));
        return instance;
    }

    public static void AttachTrail(GameObject prefab, Transform host)
    {
        if (prefab == null || host == null)
            return;

        GameObject trail = Object.Instantiate(prefab, host.position, host.rotation, host);
        trail.transform.localPosition = Vector3.zero;
        trail.transform.localRotation = Quaternion.identity;
        StripDemoBehaviours(trail);
    }

    static void PrepareOneShot(GameObject instance)
    {
        StripDemoBehaviours(instance);

        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
        }

        for (int i = 0; i < systems.Length; i++)
            systems[i].Play(false);

        AudioSource[] audios = instance.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audios.Length; i++)
        {
            if (audios[i] == null)
                continue;
            audios[i].loop = false;
        }
    }

    static void StripDemoBehaviours(GameObject root)
    {
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName == "csDestroyEffect" || typeName == "CFX_AutoDestructShuriken")
                Object.Destroy(behaviour);
        }
    }
}
