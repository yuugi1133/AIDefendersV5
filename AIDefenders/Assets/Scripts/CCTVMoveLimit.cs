using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CCTVMoveLimit : MonoBehaviour
{
    public Collider Volume
    {
        get
        {
            if (cached == null)
                cached = GetComponent<Collider>();
            return cached;
        }
    }

    Collider cached;
}
