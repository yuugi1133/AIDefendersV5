using UnityEngine;

public static class HudFx
{
    public static event System.Action<Vector3, float> DamageShown;

    public static void ShowDamage(Vector3 world, float amount)
    {
        if (DamageShown != null)
            DamageShown(world, amount);
    }
}
