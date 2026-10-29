using System.Collections;
using UnityEngine;

public class Target_ThiefBoat : Target_Enemy
{
    [Header("Boat Death")]
    public GameObject splashPrefab;
    public float pitchDegrees = 62f;
    public float sinkDepth = 7f;
    public float sinkDuration = 5f;
    public int splashCount = 8;
    public float splashRadius = 3.4f;
    public float splashLife = 1.35f;

    protected override bool PlayAnimatorDeath
    {
        get { return false; }
    }

    protected override bool StickToGround
    {
        get { return false; }
    }

    protected override IEnumerator DeathRoutine()
    {
        yield return Target_BoatDeath.Play(
            transform,
            splashPrefab,
            pitchDegrees,
            sinkDepth,
            sinkDuration,
            splashCount,
            splashRadius,
            splashLife);
        Destroy(gameObject);
    }
}
