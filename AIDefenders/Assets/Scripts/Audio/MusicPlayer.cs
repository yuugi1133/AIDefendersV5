using System.Collections;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public WaveDirector waveDirector;
    public AudioClip prepMusic;
    public AudioClip combatMusic;
    [Range(0f, 1f)]
    public float volume = 0.55f;
    public float fadeSeconds = 1.25f;
    public bool playOnStart;

    AudioSource primary;
    AudioSource secondary;
    Coroutine fadeRoutine;
    AudioClip currentClip;

    void Awake()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();

        primary = CreateSource("MusicA");
        secondary = CreateSource("MusicB");
    }

    void OnEnable()
    {
        if (waveDirector == null)
            waveDirector = FindFirstObjectByType<WaveDirector>();
        if (waveDirector == null)
            return;

        waveDirector.OnPrepStarted += HandlePrepStarted;
        waveDirector.OnWaveStarted += HandleWaveStarted;
        waveDirector.OnStageComplete += HandleStageComplete;
    }

    void Start()
    {
        if (!playOnStart)
            return;
        Play(prepMusic != null ? prepMusic : combatMusic);
    }

    void OnDisable()
    {
        if (waveDirector == null)
            return;

        waveDirector.OnPrepStarted -= HandlePrepStarted;
        waveDirector.OnWaveStarted -= HandleWaveStarted;
        waveDirector.OnStageComplete -= HandleStageComplete;
    }

    void HandlePrepStarted(int waveIndex, float duration)
    {
        Play(prepMusic != null ? prepMusic : combatMusic);
    }

    void HandleWaveStarted(int waveIndex)
    {
        Play(combatMusic != null ? combatMusic : prepMusic);
    }

    void HandleStageComplete()
    {
        FadeOut();
    }

    public void Play(AudioClip clip)
    {
        if (clip == null)
            return;
        if (currentClip == clip && primary.isPlaying)
            return;

        currentClip = clip;
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossfadeTo(clip));
    }

    public void FadeOut()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    IEnumerator CrossfadeTo(AudioClip clip)
    {
        secondary.Stop();
        secondary.clip = clip;
        secondary.volume = 0f;
        secondary.Play();

        float startA = primary.volume;
        float duration = Mathf.Max(0.05f, fadeSeconds);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            primary.volume = Mathf.Lerp(startA, 0f, u);
            secondary.volume = Mathf.Lerp(0f, volume, u);
            yield return null;
        }

        primary.Stop();
        primary.volume = 0f;
        secondary.volume = volume;

        AudioSource swap = primary;
        primary = secondary;
        secondary = swap;
        fadeRoutine = null;
    }

    IEnumerator FadeOutRoutine()
    {
        float start = primary.volume;
        float duration = Mathf.Max(0.05f, fadeSeconds);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            primary.volume = Mathf.Lerp(start, 0f, u);
            secondary.volume = 0f;
            yield return null;
        }

        primary.Stop();
        secondary.Stop();
        primary.volume = 0f;
        currentClip = null;
        fadeRoutine = null;
    }

    AudioSource CreateSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.priority = 0;
        return source;
    }
}
