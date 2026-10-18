using UnityEngine;
using UnityEngine.UI;

public class SettingScreen : BaseScreen
{
    [System.Serializable]
    private class AudioSettingUI
    {
        public Button toggleButton;
        public Image buttonImage;
        public Sprite enabledSprite;
        public Sprite disabledSprite;
        public Slider volumeSlider;
    }

    [Header("Music")]
    [SerializeField] private AudioSettingUI music;

    [Header("Sound")]
    [SerializeField] private AudioSettingUI sound;

    [Header("Language")]
    [SerializeField] private Button languageButton;
    [SerializeField] private TMPro.TMP_Text languageText;

    private bool _isRefreshing;

    private AudioSettingsManager Audio => AudioSettingsManager.Instance;

    private void Awake()
    {
        music.toggleButton.onClick.AddListener(ToggleMusic);
        sound.toggleButton.onClick.AddListener(ToggleSound);

        music.volumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sound.volumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);

        languageButton.onClick.AddListener(ToggleLanguage);
    }

    public override void Show()
    {
        base.Show();
        RefreshUI();
        RefreshLanguageUI();

        if (LocalizeManager.Instance != null)
        {
            LocalizeManager.Instance.OnLanguageChanged -= RefreshLanguageUI;
            LocalizeManager.Instance.OnLanguageChanged += RefreshLanguageUI;
        }
    }

    private void RefreshUI()
    {
        if (Audio == null)
        {
            Debug.LogWarning("[SettingScreen] AudioSettingsManager not found.");
            return;
        }

        _isRefreshing = true;

        RefreshSetting(music, Audio.IsMusicEnabled, Audio.MusicVolume);

        RefreshSetting(sound, Audio.IsSoundEnabled, Audio.SoundVolume);

        _isRefreshing = false;
    }

    private void RefreshSetting(AudioSettingUI ui, bool isEnabled, float savedVolume)
    {
        ui.buttonImage.sprite = isEnabled ? ui.enabledSprite : ui.disabledSprite;

        ui.volumeSlider.interactable = isEnabled;

        // Khi tắt, chỉ slider về 0. Volume đã lưu trong Manager không bị thay đổi.
        ui.volumeSlider.SetValueWithoutNotify(isEnabled ? savedVolume : 0f);
    }

    private void ToggleMusic()
    {
        if (Audio == null) return;

        Audio.SetMusicEnabled(!Audio.IsMusicEnabled);
        RefreshUI();
    }

    private void ToggleSound()
    {
        if (Audio == null) return;

        Audio.SetSoundEnabled(!Audio.IsSoundEnabled);
        RefreshUI();
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (_isRefreshing || Audio == null) return;

        Audio.SetMusicVolume(value);
    }

    private void OnSoundVolumeChanged(float value)
    {
        if (_isRefreshing || Audio == null) return;

        Audio.SetSoundVolume(value);
    }

    private void OnDestroy()
    {
        if (music.toggleButton != null)
            music.toggleButton.onClick.RemoveListener(ToggleMusic);

        if (sound.toggleButton != null)
            sound.toggleButton.onClick.RemoveListener(ToggleSound);

        if (music.volumeSlider != null)
            music.volumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sound.volumeSlider != null)
            sound.volumeSlider.onValueChanged.RemoveListener(OnSoundVolumeChanged);

        if (languageButton != null)
            languageButton.onClick.RemoveListener(ToggleLanguage);

        if (LocalizeManager.Instance != null)
        {
            LocalizeManager.Instance.OnLanguageChanged -= RefreshLanguageUI;
        }
    }

    private void ToggleLanguage()
    {
        if (LocalizeManager.Instance == null)
        {
            Debug.LogWarning("[SettingScreen] LocalizeManager not found.");
            return;
        }

        Language nextLanguage = LocalizeManager.Instance.CurrentLanguage == Language.Vietnamese ? Language.English : Language.Vietnamese;

        LocalizeManager.Instance.ChangeLanguage(nextLanguage);
    }

    private void RefreshLanguageUI()
    {
        if (LocalizeManager.Instance == null || languageText == null)
            return;

        languageText.text = LocalizeManager.Instance.CurrentLanguage == Language.Vietnamese ? "Language: Tiếng Việt" : "Language: English";
    }
}
