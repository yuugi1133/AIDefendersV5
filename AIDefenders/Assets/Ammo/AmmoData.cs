using UnityEngine;

[CreateAssetMenu(fileName = "AmmoData", menuName = "Game/Ammo")]
public class AmmoData : ScriptableObject
{
    public float bulletSpeed = 20f;
    public float gravity = 9.81f;
    public float damage = 10f;
    public GameObject bulletPrefab;
}
