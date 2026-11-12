using UnityEngine;

[System.Serializable]
public class StoredTurret
{
    public GameObject prefab;
    public string displayName;
    public int sellPrice;
    public float buildTime = 3f;
    public int upgradeLevel = 1;

    public string Label
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
            return prefab != null ? prefab.name : "포탑";
        }
    }
}

[System.Serializable]
public class TurretBuildOption
{
    public string displayName;
    public GameObject prefab;
    public int cost = 50;
    public int sellPrice = 25;
    public float buildTime = 3f;

    public string Label
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            if (prefab != null)
            {
                if (prefab.name == "Turret_Test Missile") return "미사일 포탑";
                if (prefab.name == "Turret_Test Laser") return "레이저 포탑";
            }
            return prefab != null ? prefab.name : "포탑";
        }
    }
}
