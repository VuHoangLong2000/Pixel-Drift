using UnityEngine;

public abstract class BaseScreen : MonoBehaviour
{
    [SerializeField] private string screenID;

    public string ScreenID => screenID;

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    public virtual void Release()
    {

    }

    public void Close()
    {
        ScreenManager.Instance.HideScreen(screenID);
    }
}