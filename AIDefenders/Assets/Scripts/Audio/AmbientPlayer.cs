using System.Collections;
using UnityEngine;

public class AmbientPlayer : MonoBehaviour
{
    public WaveDirector waveDirector;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 0.22f;
    [Range(0f, 1f)]
    public float combatVolume = 0.14f;
    public float fadeSeconds = 1.25f;
    public bool playOnStart;

    AudioSource source;
    Coroutine fadeRoutine;

    void Awake()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();

        GameObject go = new GameObject("Ambient");
        go.transform.SetParent(transform, false);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.priority = 16;
        source.dopplerLevel = 0f;
    }

    void OnEnable()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();
        if (waveDirector == null)
            return;

        waveDirector.OnPrepStarted += HandlePrepStarted;
        waveDirector.OnWaveStarted += HandleWaveStarted;
    }

    void Start()
    {
        if (playOnStart)
            FadeTo(volume);
    }

    void OnDisable()
    {
        if (waveDirector == null)
            return;

        waveDirector.OnPrepStarted -= HandlePrepStarted;
        waveDirector.OnWaveStarted -= HandleWaveStarted;
    }

    void HandlePrepStarted(int waveIndex, float duration)
    {
        FadeTo(volume);
    }

    void HandleWaveStarted(int waveIndex)
    {
        FadeTo(combatVolume);
    }

    void FadeTo(float target)
    {
        if (clip == null || source == null)
            return;

        if (source.clip != clip)
            source.clip = clip;
        if (!source.isPlaying)
            source.Play();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    IEnumerator FadeRoutine(float target)
    {
        float start = source.volume;
        float duration = Mathf.Max(0.05f, fadeSeconds);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }

        source.volume = target;
        fadeRoutine = null;
    }
}
