using UnityEngine;

public static class CombatCollision
{
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
