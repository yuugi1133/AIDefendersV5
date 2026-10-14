using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TargetGoal : MonoBehaviour
{
    Collider goalCollider;

    void Awake()
    {
        goalCollider = GetComponent<Collider>();
        if (goalCollider != null)
            goalCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        Target_Base target = other.GetComponentInParent<Target_Base>();
        if (target == null)
            return;

        target.NotifyReachedGoal();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.25f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            return;
        }

        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
