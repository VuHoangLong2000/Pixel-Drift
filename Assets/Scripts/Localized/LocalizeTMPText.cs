using UnityEngine;
using TMPro;

public class LocalizeTMPText : MonoBehaviour
{
    [SerializeField] private string key;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }
    private void OnEnable()
    {
        if (LocalizeManager.Instance != null)
        {
            LocalizeManager.Instance.OnLanguageChanged += Refresh;
        }
        Refresh();
    }
    private void OnDisable()
    {
        if (LocalizeManager.Instance != null)
        {
            LocalizeManager.Instance.OnLanguageChanged -= Refresh;
        }
    }
    public void SetKey(string key)
    {
        this.key = key;
        Refresh();
    }
    private void Refresh()
    {
        if (_text == null)
            return;
        if (string.IsNullOrEmpty(key))
            return;

        _text.text = LocalizeManager.Instance.GetLocalize(key);
    }
}
