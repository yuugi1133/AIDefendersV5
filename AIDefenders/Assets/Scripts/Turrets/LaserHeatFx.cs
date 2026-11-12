using System.Collections.Generic;
using UnityEngine;

public class LaserHeatFx : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly Color SkyBlue = new Color(0.32f, 0.82f, 1f, 1f);
    const float SmokeStopSlack = 0.04f;

    public WeaponSystem weapon;
    public GameObject smokePrefab;
    public Vector3 smokeLocalOffset = new Vector3(0f, 0.95f, 0f);
    public float smokeScale = 0.55f;
    [Range(0.05f, 1f)]
    public float smokeHeatThreshold = 0.5f;
    public float maxEmissionIntensity = 6f;

    readonly List<Material> glowMaterials = new List<Material>();
    GameObject smokeLoop;
    bool smokeOn;

    void Awake()
    {
        if (weapon == null)
            weapon = GetComponent<WeaponSystem>();
        BindGlowMaterials();
    }

    void LateUpdate()
    {
        float heat = 0f;
        if (weapon != null && weapon.UsesHeat)
            heat = weapon.HeatRatio;

        ApplyGlow(heat);
        TickSmoke(heat);
    }

    void OnDestroy()
    {
        StopSmoke();
        for (int i = 0; i < glowMaterials.Count; i++)
        {
            if (glowMaterials[i] != null)
                Destroy(glowMaterials[i]);
        }
        glowMaterials.Clear();
    }

    void BindGlowMaterials()
    {
        glowMaterials.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer renderer = renderers[r];
            if (renderer == null)
                continue;

            Material[] shared = renderer.sharedMaterials;
            if (shared == null || shared.Length == 0)
                continue;

            bool hasGlow = false;
            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] != null && IsGlowMaterial(shared[i]))
                {
                    hasGlow = true;
                    break;
                }
            }
            if (!hasGlow)
                continue;

            Material[] instances = renderer.materials;
            for (int i = 0; i < instances.Length; i++)
            {
                Material mat = instances[i];
                if (mat == null || !IsGlowMaterial(mat))
                    continue;

                mat.EnableKeyword("_EMISSION");
                glowMaterials.Add(mat);
            }
            renderer.materials = instances;
        }
    }

    static bool IsGlowMaterial(Material mat)
    {
        string name = mat.name;
        int cut = name.IndexOf(" (Instance)", System.StringComparison.Ordinal);
        if (cut >= 0)
            name = name.Substring(0, cut);
        return string.Equals(name, "Glow", System.StringComparison.OrdinalIgnoreCase);
    }

    void ApplyGlow(float heat)
    {
        Color emission = Color.black;
        if (heat > 0.001f)
        {
            emission = SkyBlue * (Mathf.Max(0.01f, maxEmissionIntensity) * heat);
        }

        for (int i = 0; i < glowMaterials.Count; i++)
        {
            Material mat = glowMaterials[i];
            if (mat == null)
                continue;
            if (mat.HasProperty(EmissionColorId))
                mat.SetColor(EmissionColorId, emission);
        }
    }

    void TickSmoke(float heat)
    {
        if (smokePrefab == null)
            return;

        if (!smokeOn && heat >= smokeHeatThreshold)
            StartSmoke();
        else if (smokeOn && heat < smokeHeatThreshold - SmokeStopSlack)
            StopSmoke();
    }

    void StartSmoke()
    {
        if (smokeOn)
            return;

        Transform parent = transform;
        Vector3 world = parent.TransformPoint(smokeLocalOffset);
        smokeLoop = CombatVfx.PlayLoop(smokePrefab, world, parent);
        if (smokeLoop == null)
            return;

        smokeLoop.transform.localPosition = smokeLocalOffset;
        smokeLoop.transform.localRotation = Quaternion.identity;
        float scale = Mathf.Max(0.05f, smokeScale);
        smokeLoop.transform.localScale = new Vector3(scale, scale, scale);
        smokeOn = true;
    }

    void StopSmoke()
    {
        if (smokeLoop != null)
        {
            CombatVfx.StopLoop(smokeLoop);
            smokeLoop = null;
        }
        smokeOn = false;
    }
}
