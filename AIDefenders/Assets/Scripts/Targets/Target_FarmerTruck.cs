using UnityEngine;

public class Target_FarmerTruck : Target_Friendly
{
    [Header("Truck")]
    public Transform truck;
    public Light[] headlights;
    public GameObject explosionPrefab;
    public float explosionLife = 3.5f;

    protected override void Die()
    {
        WreckTruck();
        base.Die();
    }

    void WreckTruck()
    {
        Vector3 pos = truck != null
            ? truck.position
            : transform.position + Vector3.up * 0.8f;

        if (headlights != null)
        {
            for (int i = 0; i < headlights.Length; i++)
            {
                Light light = headlights[i];
                if (light == null)
                    continue;
                light.enabled = false;
                light.gameObject.SetActive(false);
            }
        }

        if (truck != null)
            truck.gameObject.SetActive(false);

        CombatVfx.Play(explosionPrefab, pos, Vector3.up, explosionLife);
    }
}
