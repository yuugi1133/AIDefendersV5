using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Target_Base : MonoBehaviour
{
    [Header("Stats")]
    public float maxHP = 100f;  //최대 체력
    public float currentHP;     //현재 체력
    public float moveSpeed = 3f; //이속

    public bool isSample = false; //움직이지 않는 샘플이면 자동 애니메이션, 이동 무시

    [Header("Death")]
    public bool isDead = false;
    public float sinkSpeed = 1f;
    public float destroyDelay = 3f;

    [Header("Components")]
    public Animator animator;
    public Renderer renderer;
    public Rigidbody rigidbody;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHP = maxHP;

        rigidbody = GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.constraints =
            RigidbodyConstraints.FreezeRotation;


    }

    // Update is called once per frame
    void Update()
    {}
    protected virtual void FixedUpdate()
    {
        if (isSample) return;
        if (isDead) return;

        Move();
    }

    //렌더링 모델 출력(bbox 생성에 필요)
    public Renderer getRenderer()
    {
        return renderer;
    }

    //이동 기능
    void Move()
    {
        Vector3 forward = transform.forward * moveSpeed;
        rigidbody.linearVelocity = new Vector3(forward.x, rigidbody.linearVelocity.y, forward.z);

        //애니메이터 요구
        if (animator)
            animator.SetBool("Move", true);
    }

    //피격 처리
    public virtual void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHP -= damage;

        if (currentHP <= 0)
            Die();
    }

    //사망 시 처리
    protected virtual void Die()
    {
        isDead = true;

        rigidbody.linearVelocity =
            Vector3.zero;

        rigidbody.isKinematic = true;

        if (animator)
        {
            animator.SetTrigger(
                "Die"
            );
        }

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(destroyDelay);

        while (transform.position.y > -2f)
        {
            transform.position +=
                Vector3.down *
                sinkSpeed *
                Time.deltaTime;

            yield return null;
        }

        Destroy(gameObject);
    }
}
