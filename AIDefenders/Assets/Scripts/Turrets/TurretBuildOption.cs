using UnityEngine;

[System.Serializable]
public class TurretBuildOption
{
    public string displayName;
    public GameObject prefab;

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
