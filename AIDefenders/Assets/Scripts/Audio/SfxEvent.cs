using UnityEngine;

[CreateAssetMenu(menuName = "AIDefenders/Sfx Event", fileName = "SfxEvent")]
public class SfxEvent : ScriptableObject
{
    public AudioClip[] clips;
    [Range(0f, 1f)]
    public float volume = 1f;
    public Vector2 pitch = new Vector2(0.97f, 1.03f);

    public AudioClip FirstClip()
    {
        if (clips == null)
            return null;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
                return clips[i];
        }
        return null;
    }

    public bool TryPick(out AudioClip clip, out float volumeScale, out float pitchScale)
    {
        clip = null;
        volumeScale = volume;
        pitchScale = 1f;
        if (clips == null || clips.Length == 0)
            return false;

        int start = Random.Range(0, clips.Length);
        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip candidate = clips[(start + i) % clips.Length];
            if (candidate == null)
                continue;
            clip = candidate;
            float min = Mathf.Min(pitch.x, pitch.y);
            float max = Mathf.Max(pitch.x, pitch.y);
            pitchScale = min >= max ? min : Random.Range(min, max);
            return true;
        }

        return false;
    }
}
