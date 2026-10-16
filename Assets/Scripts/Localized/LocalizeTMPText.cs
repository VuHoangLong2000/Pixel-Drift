using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizeTMPText : MonoBehaviour
{
    [SerializeField] private string key;

    private TMP_Text _text;
    private object[] _arguments = System.Array.Empty<object>();

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (LocalizeManager.Instance != null)
            LocalizeManager.Instance.OnLanguageChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (LocalizeManager.Instance != null)
            LocalizeManager.Instance.OnLanguageChanged -= Refresh;
    }

    public void SetKey(string key)
    {
        this.key = key;
        Refresh();
    }

    public void SetArguments(params object[] arguments)
    {
        _arguments = arguments ?? System.Array.Empty<object>();
        Refresh();
    }

    // Đổi key và tham số cùng lúc.
    public void SetText(string key, params object[] arguments)
    {
        this.key = key;
        _arguments = arguments ?? System.Array.Empty<object>();
        Refresh();
    }

    private void Refresh()
    {
        if (_text == null || string.IsNullOrEmpty(key))
            return;

        if (LocalizeManager.Instance == null)
            return;

        _text.text = LocalizeManager.Instance.GetLocalize(key, _arguments);
    }
}
