using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : BaseScreen
{
    [Header("Settings")]
    [SerializeField] private Button settingsButton;

    [Header("Coin")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Button addCoinButton;

    [Header("Level")]
    [SerializeField] private PixelBlockManager pixelBlockManager;
    [SerializeField] private TMP_Text levelText;

    [Header("Spells")]
    [SerializeField] private Button[] spellButtons = new Button[4];

    private void OnEnable()
    {
        if (pixelBlockManager != null)
            pixelBlockManager.OnLevelLoaded += RefreshLevelText;

        if (LocalizeManager.Instance != null)
            LocalizeManager.Instance.OnLanguageChanged += RefreshLevelText;

        RefreshLevelText();
    }

    private void OnDisable()
    {
        if (pixelBlockManager != null)
            pixelBlockManager.OnLevelLoaded -= RefreshLevelText;

        if (LocalizeManager.Instance != null)
            LocalizeManager.Instance.OnLanguageChanged -= RefreshLevelText;
    }

    private void Awake()
    {
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (addCoinButton != null)
            addCoinButton.onClick.AddListener(OnAddCoinClicked);

        if (spellButtons != null)
        {
            foreach (Button button in spellButtons)
            {
                if (button != null)
                    button.onClick.AddListener(OnSpellClicked);
            }
        }
    }

    private void RefreshLevelText()
    {
        if (levelText == null || pixelBlockManager == null)
            return;

        int levelNumber = pixelBlockManager.CurrentLevelIndex + 1;

        if (pixelBlockManager.CurrentLevelIndex < 0)
            return;

        if (LocalizeManager.Instance != null)
        {
            levelText.text = LocalizeManager.Instance.GetLocalize("text_level", levelNumber);
        }
        else
        {
            levelText.text = $"Level {levelNumber}";
        }
    }

    private void OpenSettings()
    {
        if (ScreenManager.Instance != null)
            ScreenManager.Instance.ShowScreen("SettingScreen");
    }

    private void OnAddCoinClicked()
    {
        // TODO: Connect Shop after its design is finalized.
    }

    private void OnSpellClicked()
    {
        // TODO: Connect spell activation system.
    }

    public void SetCoinAmount(int amount)
    {
        if (coinText != null)
            coinText.text = Mathf.Max(0, amount).ToString("N0");
    }

    private void OnDestroy()
    {
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OpenSettings);

        if (addCoinButton != null)
            addCoinButton.onClick.RemoveListener(OnAddCoinClicked);

        if (spellButtons == null)
            return;

        foreach (Button button in spellButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(OnSpellClicked);
        }
    }
}
