using UnityEngine;

public class GameSfx : MonoBehaviour
{
    public static GameSfx Instance { get; private set; }

    public SfxEvent uiClick;
    public SfxEvent platformPlaced;
    public SfxEvent groupSpawned;
    public SfxEvent identified;
    [Range(0f, 1f)]
    public float uiVolume = 0.7f;
    [Range(0f, 1f)]
    public float worldVolume = 1f;
    public float worldMinDistance = 6f;
    public float worldMaxDistance = 48f;

    AudioSource uiSource;

    void Awake()
    {
        Instance = this;
        uiSource = CreateUiSource();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void PlayUiClick()
    {
        Play(Instance != null ? Instance.uiClick : null);
    }

    public static void PlayPlatformPlaced()
    {
        Play(Instance != null ? Instance.platformPlaced : null);
    }

    public static void PlayGroupSpawned()
    {
        Play(Instance != null ? Instance.groupSpawned : null);
    }

    public static void PlayIdentified()
    {
        Play(Instance != null ? Instance.identified : null);
    }

    public static void PlayWorld(SfxEvent sfx, Vector3 position)
    {
        if (sfx == null || !sfx.TryPick(out AudioClip clip, out float volumeScale, out float pitchScale))
            return;

        float volume = volumeScale;
        float minDistance = 6f;
        float maxDistance = 48f;
        if (Instance != null)
        {
            volume *= Instance.worldVolume;
            minDistance = Instance.worldMinDistance;
            maxDistance = Instance.worldMaxDistance;
        }

        SpawnWorldSource(clip, position, volume, pitchScale, minDistance, maxDistance);
    }

    static void Play(SfxEvent sfx)
    {
        if (Instance != null)
            Instance.PlayUi(sfx);
    }

    public void PlayUi(SfxEvent sfx)
    {
        if (sfx == null || uiSource == null)
            return;
        if (!sfx.TryPick(out AudioClip clip, out float volumeScale, out float pitchScale))
            return;

        uiSource.pitch = pitchScale;
        uiSource.PlayOneShot(clip, uiVolume * volumeScale);
    }

    static void SpawnWorldSource(
        AudioClip clip,
        Vector3 position,
        float volume,
        float pitch,
        float minDistance,
        float maxDistance)
    {
        GameObject go = new GameObject("WorldSfx");
        go.transform.position = position;
        if (Instance != null)
            go.transform.SetParent(Instance.transform, true);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.priority = 128;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = Mathf.Max(minDistance + 0.1f, maxDistance);
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        float duration = clip.length;
        if (duration < 0.02f)
            duration = 2f;
        duration = duration / Mathf.Max(0.05f, Mathf.Abs(pitch)) + 0.08f;
        Object.Destroy(go, duration);
    }

    AudioSource CreateUiSource()
    {
        GameObject go = new GameObject("UiSfx");
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.priority = 32;
        source.dopplerLevel = 0f;
        source.ignoreListenerPause = true;
        return source;
    }
}
