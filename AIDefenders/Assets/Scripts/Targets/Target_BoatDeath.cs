using System.Collections;
using UnityEngine;

public static class Target_BoatDeath
{
    public static IEnumerator Play(
        Transform root,
        GameObject splashPrefab,
        float pitchDegrees,
        float sinkDepth,
        float duration,
        int splashCount,
        float splashRadius,
        float splashLife)
    {
        if (root == null)
            yield break;

        duration = Mathf.Max(0.2f, duration);
        Quaternion startRot = root.rotation;
        Quaternion diveRot = startRot * Quaternion.Euler(Mathf.Abs(pitchDegrees), 0f, 0f);
        Vector3 startPos = root.position;
        Vector3 endPos = startPos + Vector3.down * Mathf.Max(0.5f, sinkDepth);
        float waterY = startPos.y;

        float elapsed = 0f;
        int splashesDone = 0;
        float nextSplash = 0.05f;
        int totalSplashes = Mathf.Max(1, splashCount);
        float splashStep = duration / totalSplashes;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = t * t;
            root.rotation = Quaternion.Slerp(startRot, diveRot, ease);
            root.position = Vector3.Lerp(startPos, endPos, ease);

            if (splashesDone < totalSplashes && elapsed >= nextSplash)
            {
                PlaySplash(root, splashPrefab, waterY, splashRadius, splashLife);
                splashesDone++;
                nextSplash += splashStep * Random.Range(0.55f, 1.15f);
            }

            yield return null;
        }

        root.rotation = diveRot;
        root.position = endPos;

        while (splashesDone < totalSplashes)
        {
            PlaySplash(root, splashPrefab, waterY, splashRadius, splashLife);
            splashesDone++;
            yield return new WaitForSeconds(0.08f);
        }

        yield return new WaitForSeconds(0.35f);
    }

    static void PlaySplash(
        Transform root,
        GameObject splashPrefab,
        float waterY,
        float radius,
        float life)
    {
        if (splashPrefab == null || root == null)
            return;

        Vector2 offset = Random.insideUnitCircle * Mathf.Max(0.4f, radius);
        Vector3 pos = root.position + root.right * offset.x + root.forward * offset.y;
        pos.y = waterY;
        CombatVfx.Play(splashPrefab, pos, Vector3.up, Mathf.Max(0.2f, life));
    }
}
