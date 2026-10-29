using UnityEngine;

public class Target_ThiefMotorCycle : Target_Enemy
{
    [Header("Motorcycle")]
    public Transform motorcycle;
    public Light headlight;
    public GameObject explosionPrefab;
    public float explosionLife = 3.5f;

    protected override void Die()
    {
        WreckMotorcycle();
        base.Die();
    }

    void WreckMotorcycle()
    {
        Vector3 pos = motorcycle != null
            ? motorcycle.position
            : transform.position + Vector3.up * 0.6f;

        if (headlight != null)
        {
            headlight.enabled = false;
            headlight.gameObject.SetActive(false);
        }

        if (motorcycle != null)
            motorcycle.gameObject.SetActive(false);

        CombatVfx.Play(explosionPrefab, pos, Vector3.up, explosionLife);
    }
}
