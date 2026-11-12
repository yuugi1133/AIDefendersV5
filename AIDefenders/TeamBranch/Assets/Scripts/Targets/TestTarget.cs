using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TestTarget : MonoBehaviour
{
    public float speed = 5f;
    Rigidbody rb;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();


        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        Vector3 forward = transform.forward * speed;
        rb.linearVelocity = new Vector3(forward.x, rb.linearVelocity.y, forward.z);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
