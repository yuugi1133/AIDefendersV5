using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Target_Base : MonoBehaviour
{
    [Header("Stats")]
    public float maxHP = 100f;  //??? ???
    public float currentHP;     //???? ???
    public float moveSpeed = 3f; //???

    public bool isSample = false; //???????? ??? ??????? ??? ???????, ??? ????

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
            RigidbodyConstraints.FreezeRotation |
            RigidbodyConstraints.FreezePositionY;


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

    //?????? ?? ???(bbox ?????? ???)
    public Renderer getRenderer()
    {
        return renderer;
    }

    //??? ???
    void Move()
    {
        Vector3 forward = transform.forward * moveSpeed;
        rigidbody.linearVelocity = new Vector3(forward.x, 0f, forward.z);

        //???????? ??
        if (animator)
            animator.SetBool("Move", true);
    }

    //??? ???
    public virtual void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHP -= damage;

        if (currentHP <= 0)
            Die();
    }

    //??? ?? ???
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
