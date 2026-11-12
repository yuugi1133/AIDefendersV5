using UnityEngine;

public static class GameAudioSettings
{
    const string VolumeKey = "Audio.MasterVolume";
    const float DefaultVolume = .5f;
    public static event System.Action Changed;
    public static float MusicVolume { get { return PlayerPrefs.GetFloat("Audio.MusicVolume", DefaultVolume); } }
    public static float EffectsVolume { get { return PlayerPrefs.GetFloat("Audio.EffectsVolume", DefaultVolume); } }
    public static float MoveSensitivity { get { return PlayerPrefs.GetFloat("Camera.MoveSensitivity", 1f); } }
    public static float LookSensitivity { get { return PlayerPrefs.GetFloat("Camera.LookSensitivity", 1f); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        Changed = null;
        // Apply the revised defaults once, then preserve the player's later adjustments.
        if (PlayerPrefs.GetInt("Audio.DefaultsVersion", 0) < 2)
        {
            PlayerPrefs.SetFloat(VolumeKey, DefaultVolume);
            PlayerPrefs.SetFloat("Audio.MusicVolume", DefaultVolume);
            PlayerPrefs.SetFloat("Audio.EffectsVolume", DefaultVolume);
            PlayerPrefs.SetInt("Audio.DefaultsVersion", 2);
            PlayerPrefs.Save();
        }
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
    }

    public static void SetVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
    }

    public static void Save() { PlayerPrefs.Save(); }
    public static void SetMusicVolume(float value) { Set("Audio.MusicVolume", value, 0, 1); }
    public static void SetEffectsVolume(float value) { Set("Audio.EffectsVolume", value, 0, 1); }
    public static void SetMoveSensitivity(float value) { Set("Camera.MoveSensitivity", value, .25f, 2); }
    public static void SetLookSensitivity(float value) { Set("Camera.LookSensitivity", value, .25f, 2); }
    static void Set(string key, float value, float min, float max)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp(value, min, max));
        if (Changed != null) Changed();
    }
}
