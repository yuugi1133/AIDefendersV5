using UnityEngine;

public static class HudFx
{
    public static event System.Action<Vector3, float, float> DamageShown;
    public static event System.Action<Vector3, int> FundsShown;
    public static event System.Action<Vector3, int> ScoreShown;

    public static void ShowDamage(Vector3 world, float amount, float pierceDelta)
    {
        if (DamageShown != null)
            DamageShown(world, amount, pierceDelta);
    }

    public static void ShowFunds(Vector3 world, int amount)
    {
        if (amount == 0 || FundsShown == null)
            return;
        FundsShown(world, amount);
    }

    public static void ShowScore(Vector3 world, int amount)
    {
        if (amount == 0 || ScoreShown == null)
            return;
        ScoreShown(world, amount);
    }
}
