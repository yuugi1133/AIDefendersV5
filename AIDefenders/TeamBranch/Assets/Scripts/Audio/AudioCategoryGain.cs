using UnityEngine;

// Keep fade/playback levels separate from the user's category gain, including at zero volume.
[RequireComponent(typeof(AudioSource))]
public class AudioCategoryGain : MonoBehaviour
{
    public bool music;
    public float Level { get; private set; }
    AudioSource source;
    void Awake() { source = GetComponent<AudioSource>(); Level = source.volume; }
    void OnEnable() { GameAudioSettings.Changed += Apply; }
    void OnDisable() { GameAudioSettings.Changed -= Apply; }
    void Apply() { if (source != null) source.volume = Level * (music ? GameAudioSettings.MusicVolume : GameAudioSettings.EffectsVolume); }
    public static void Set(AudioSource source, float level, bool music = false)
    {
        var gain = source.GetComponent<AudioCategoryGain>();
        if (gain == null) gain = source.gameObject.AddComponent<AudioCategoryGain>();
        gain.music = music;
        gain.Level = Mathf.Clamp01(level);
        gain.Apply();
    }
    public static float Get(AudioSource source)
    {
        var gain = source.GetComponent<AudioCategoryGain>();
        return gain != null ? gain.Level : source.volume;
    }
}
