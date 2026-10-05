using UnityEngine;

[System.Serializable]
public class TurretBuildOption
{
    public string displayName;
    public GameObject prefab;
    public int cost = 50;
    public int sellPrice = 25;

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
