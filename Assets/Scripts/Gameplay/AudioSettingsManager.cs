using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [SerializeField] private string soundVolumeParameter = "SoundVolume";

    private const string MusicEnabledKey = "Settings_MusicEnabled";
    private const string SoundEnabledKey = "Settings_SoundEnabled";
    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string SoundVolumeKey = "Settings_SoundVolume";

    public bool IsMusicEnabled { get; private set; }
    public bool IsSoundEnabled { get; private set; }

    // Âm lượng gốc, không bị mất khi tắt tiếng.
    public float MusicVolume { get; private set; }
    public float SoundVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyMusicVolume();
        ApplySoundVolume();
    }

    private void LoadSettings()
    {
        IsMusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        IsSoundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;

        MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));

        SoundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SoundVolumeKey, 1f));
    }

    public void SetMusicEnabled(bool enabled)
    {
        IsMusicEnabled = enabled;
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        ApplyMusicVolume();
    }

    public void SetSoundEnabled(bool enabled)
    {
        IsSoundEnabled = enabled;
        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        ApplySoundVolume();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyMusicVolume();
    }

    public void SetSoundVolume(float volume)
    {
        SoundVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SoundVolumeKey, SoundVolume);
        ApplySoundVolume();
    }

    private void ApplyMusicVolume()
    {
        ApplyVolume(musicVolumeParameter, IsMusicEnabled ? MusicVolume : 0f);
    }

    private void ApplySoundVolume()
    {
        ApplyVolume(soundVolumeParameter,
            IsSoundEnabled ? SoundVolume : 0f);
    }

    private void ApplyVolume(string parameter, float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[AudioSettingsManager] Missing AudioMixer.");
            return;
        }

        // Slider dùng thang tuyến tính 0–1.
        // AudioMixer dùng decibel.
        float decibels = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;

        if (!audioMixer.SetFloat(parameter, decibels))
        {
            Debug.LogWarning($"[AudioSettingsManager] Parameter not found: {parameter}");
        }
    }
}
