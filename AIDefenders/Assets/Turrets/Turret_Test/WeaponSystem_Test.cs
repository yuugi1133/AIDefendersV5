using UnityEngine;
using System.Collections;

public class WeaponSystem : MonoBehaviour
{
    public AmmoData ammo;

    public float fireDelay = 0.5f;  //발사속도
    public int maxAmmo = 10;        //최대 장탄수
    public float reloadTime = 2f;   //재장전 시간

    int currentAmmo;                
    bool isReloading = false;
    float lastFireTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentAmmo = maxAmmo;
    }

    //발사 가능 조건
    public bool CanFire()
    {
        if (isReloading) return false;
        if (Time.time - lastFireTime < fireDelay) return false;
        if (currentAmmo <= 0) return false;

        return true;
    }

    //발사
    public void Fire(Vector3 origin, Vector3 velocity)
    {
        if (!CanFire()) return;

        GameObject bulletObj = Instantiate(ammo.bulletPrefab, origin, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();


        bullet.Init(velocity, ammo.gravity, ammo.damage);

        currentAmmo--;
        lastFireTime = Time.time;

        if (currentAmmo <= 0)
            StartCoroutine(Reload());
    }

    //재장전
    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
