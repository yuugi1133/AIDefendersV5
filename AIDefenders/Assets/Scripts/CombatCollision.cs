using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatCollision
{
    public const string GroundLayerName = "Ground";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IgnoreBlockingPairs()
    {
        int enemy = LayerMask.NameToLayer("Enemy");
        int friendly = LayerMask.NameToLayer("Friendly");
        int turret = LayerMask.NameToLayer("Turret");

        if (enemy >= 0)
            Physics.IgnoreLayerCollision(enemy, enemy, true);
        if (friendly >= 0)
            Physics.IgnoreLayerCollision(friendly, friendly, true);
        if (enemy >= 0 && friendly >= 0)
            Physics.IgnoreLayerCollision(enemy, friendly, true);
        if (turret >= 0 && enemy >= 0)
            Physics.IgnoreLayerCollision(turret, enemy, true);
        if (turret >= 0 && friendly >= 0)
            Physics.IgnoreLayerCollision(turret, friendly, true);

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyGroundLayers();
    }

    public static int GroundMask()
    {
        int ground = LayerMask.NameToLayer(GroundLayerName);
        if (ground >= 0)
            return 1 << ground;

        int mask = Physics.DefaultRaycastLayers;
        mask &= ~LayerMask.GetMask("Enemy", "Friendly", "Turret", "Bullet", "Ignore Raycast");
        return mask;
    }

    public static int GroundMaskOrOverride(LayerMask overrideMask)
    {
        int ground = GroundMask();
        if (overrideMask.value == 0)
            return ground;
        return overrideMask.value | ground;
    }

    public static void ApplyGroundLayers()
    {
        int layer = LayerMask.NameToLayer(GroundLayerName);
        if (layer < 0)
            return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                if (roots[r] != null)
                    ClassifyGround(roots[r].transform, layer);
            }
        }
    }

    static void ClassifyGround(Transform node, int layer)
    {
        if (IsTerrainObject(node))
        {
            SetLayer(node, layer);
            return;
        }

        for (int i = 0; i < node.childCount; i++)
            ClassifyGround(node.GetChild(i), layer);
    }

    static bool IsTerrainObject(Transform node)
    {
        string name = node.name;
        if (name == "Terrains" || name == "Terrain" || name.StartsWith("Terrain_"))
            return true;
        return node.GetComponent<Terrain>() != null || node.GetComponent<TerrainCollider>() != null;
    }

    public static void AssignLayer(GameObject root, string layerName)
    {
        if (root == null)
            return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
            return;

        SetLayer(root.transform, layer);
    }

    static void SetLayer(Transform node, int layer)
    {
        node.gameObject.layer = layer;
        for (int i = 0; i < node.childCount; i++)
            SetLayer(node.GetChild(i), layer);
    }
}
