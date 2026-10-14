using UnityEngine;

[System.Serializable]
public class TurretUpgradeTier
{
    public string label = "1단계";
    public GameObject visualRoot;
    public Transform barrel;
    public Transform firePoint;
    public float damageScale = 1f;
    public float fireDelay = 0.5f;
    public int maxAmmo = 10;
    public float reloadTime = 2f;
    public float range = 30f;
    public int upgradeCost;
    public float upgradeTime = 4f;
}
