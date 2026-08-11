using UnityEngine;

public class Bullet : MonoBehaviour
{
    Vector3 velocity;   //현재 탄속
    float gravity;      //중력가속도
    float damage;       //피해량

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //터렛과 총알은 충돌을 무시한다.
        Physics.IgnoreLayerCollision(
            LayerMask.NameToLayer("Bullet"),
            LayerMask.NameToLayer("Turret")
        );
    }

    public void Init(Vector3 initialVelocity, float gravityValue, float initialdamage)
    {
        velocity = initialVelocity;
        gravity = gravityValue;
        damage = initialdamage;
    }

    void OnCollisionEnter(Collision col)
    {
        Target_Base target = col.collider.GetComponentInParent<Target_Base>();

        if (target != null)
        {
            target.TakeDamage(damage);
        }
    }

    // Update is called once per frame
    void Update()
    {
        velocity += Vector3.down * gravity * Time.deltaTime;

        transform.position += velocity * Time.deltaTime;
    }
}
