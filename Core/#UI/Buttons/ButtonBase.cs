using UnityEngine;
using UnityEngine.UI;

public class ButtonBase : PRMonoBehaviour
{
    #region Поля и свойства

    protected Button button;

    #endregion

    [Header("Ресурсы")]
    [SerializeField] protected Image buttonIcon;
    [SerializeField] protected AudioClip clickSound;

    [Header("Настройки", order = 0)]
    [SerializeField][Tooltip("Отключить кнопку после клика.")] protected bool disablePostClick;
    [SerializeField][Tooltip("Скрыть кнопку после клика.")] protected bool hidePostClick;
    [SerializeField][Tooltip("Кликабельность кнопки зависит от паузы")] protected bool changeStateButtonByPauseEvent;
    [SerializeField][Tooltip("Возможность кликабельности кнопки при паузе.")] protected bool canExecuteOnPause;
    [SerializeField] protected string metricKey;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        button = GetComponentInChildren<Button>();
    }

    public virtual bool CanExecute()
    {
        return !PRUnitySDK.PauseManager.IsLogicPaused || canExecuteOnPause;
    }

    protected override void OnEnable()
    {
        button.onClick.AddListener(Execute);
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        button.onClick.RemoveListener(Execute);
        base.OnDisable();
    }

    protected void Execute()
    {
        SendMetric();
        ClickSound();
        DisablePostClick();
        HidePostClick();

        if(CanExecute())
            InternalExecute();
    }

    protected virtual void InternalExecute()
    {

    }

    protected virtual void HidePostClick()
    {
        if (hidePostClick)
            gameObject.SetActive(false);
    }

    public virtual void DisablePostClick()
    {
        if(disablePostClick)
            button.interactable = false;
    }

    public override void OnPauseStateChanged(PauseStateEventArgs args)
    {
        if (!changeStateButtonByPauseEvent)
            return;

        button.interactable = PRUnitySDK.PauseManager.IsLogicPaused 
            ? false 
            : true;
    }

    public void SendMetric()
    {
        if (!CanExecute())
            return;

        if (string.IsNullOrEmpty(metricKey))
            return;

        PRUnitySDK.Metric.Send($"button", "click", metricKey);
    }

    /// <summary>
    /// Играет звук нажатия, если он задан на самой кнопке.
    /// </summary>
    /// <remarks>
    /// Общего звука кнопки у SDK нет: каждая кнопка звучит своим клипом или молчит.
    /// </remarks>
    public void ClickSound()
    {
        if (clickSound == null || PRUnitySDK.Managers.Sound == null)
            return;

        PRUnitySDK.Managers.Sound.PlaySoundUIOneShot(clickSound);
    }
}
