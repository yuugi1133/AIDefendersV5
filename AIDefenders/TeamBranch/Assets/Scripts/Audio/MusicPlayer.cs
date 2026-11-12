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
        AudioCategoryGain.Set(secondary, 0f, true);
        secondary.Play();

        float startA = AudioCategoryGain.Get(primary);
        float duration = Mathf.Max(0.05f, fadeSeconds);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            AudioCategoryGain.Set(primary, Mathf.Lerp(startA, 0f, u), true);
            AudioCategoryGain.Set(secondary, Mathf.Lerp(0f, volume, u), true);
            yield return null;
        }

        primary.Stop();
        AudioCategoryGain.Set(primary, 0f, true);
        AudioCategoryGain.Set(secondary, volume, true);

        AudioSource swap = primary;
        primary = secondary;
        secondary = swap;
        fadeRoutine = null;
    }

    IEnumerator FadeOutRoutine()
    {
        float start = AudioCategoryGain.Get(primary);
        float duration = Mathf.Max(0.05f, fadeSeconds);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            AudioCategoryGain.Set(primary, Mathf.Lerp(start, 0f, u), true);
            AudioCategoryGain.Set(secondary, 0f, true);
            yield return null;
        }

        primary.Stop();
        secondary.Stop();
        AudioCategoryGain.Set(primary, 0f, true);
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
        AudioCategoryGain.Set(source, 0f, true);
        source.priority = 0;
        return source;
    }
}
