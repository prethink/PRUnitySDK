using UnityEngine;
using UnityEngine.UI;

public abstract partial class MonoWindowBase : PRMonoBehaviour
{
    private bool ownsLogicPause;

    /// <summary>
    /// Уникальный ключ окна в <see cref="MonoWindowsTracker"/>.
    /// </summary>
    public abstract Enumeration Key { get; }

    [Header("Заголовок")]
    [SerializeField] protected GameObject container;
    [SerializeField] protected RectTransform header;
    [SerializeField] protected Image iconHeader;
    [SerializeField] protected LocalizationObserver titleHeader;
    [SerializeField] protected Button exitButton;

    [Header("Тело")]
    [SerializeField] protected RectTransform body;

    [SerializeField] protected bool setPauseWhenOpen;

    /// <summary>
    /// Показывает, активно ли сейчас содержимое окна.
    /// </summary>
    public bool IsVisible => GetContainer().activeSelf;

    /// <summary>
    /// Отображает окно с указанными параметрами.
    /// </summary>
    public virtual void Show(MonoWindowArgs args)
    {
        GameObject windowContainer = GetContainer();
        if (!windowContainer.activeSelf)
            windowContainer.SetActive(true);

        InitTranslate();
        windowContainer.RefreshLayoutGroupsImmediateAndRecursive();
        PRUnitySDK.Trackers.MonoWindows.NotifyWindowShown(this);

        AcquireLogicPause();

        Cursor.visible = true;
    }

    /// <summary>
    /// Скрывает окно и освобождает принадлежащее ему состояние паузы.
    /// </summary>
    /// <param name="isForceClose">
    /// При принудительном закрытии сохранение пользовательских данных не запускается.
    /// </param>
    public virtual void Hide(bool isForceClose = false)
    {
        GameObject windowContainer = GetContainer();
        bool wasVisible = windowContainer.activeSelf;

        if (!wasVisible && !ownsLogicPause)
            return;

        if (wasVisible && !isForceClose)
            GameManager.Instance.StartSaveTask();

        if (wasVisible)
            windowContainer.SetActive(false);

        ReleaseLogicPause();
        PRUnitySDK.Trackers.MonoWindows.NotifyWindowHidden(this);

        if (!PRUnitySDK.Trackers.MonoWindows.HasOpenWindows)
            GameManager.Instance.LoadingUserCursorState();
    }

    protected GameObject GetContainer()
    {
        return container != null 
            ? container 
            : gameObject;
    }

    public abstract void InitTranslate();

    protected virtual void ExitButtonAction()
    {
        Hide();
    }

    protected override void OnEnable()
    {
        exitButton?.onClick.AddListener(ExitButtonAction);
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        exitButton?.onClick.RemoveListener(ExitButtonAction);
        base.OnDisable();
    }

    protected override void RegisterEventsOnCreated()
    {
        PRUnitySDK.Trackers.MonoWindows.Register(this);
        base.RegisterEventsOnCreated();
    }

    protected override void UnRegisterEventsOnDestroy()
    {
        ReleaseLogicPause();
        PRUnitySDK.Trackers.MonoWindows.Unregister(this);
        base.UnRegisterEventsOnDestroy();
    }

    /// <inheritdoc />
    public override void OnPauseStateChanged(PauseStateEventArgs args)
    {
        base.OnPauseStateChanged(args);

        if (!setPauseWhenOpen || !IsVisible || args == null || object.ReferenceEquals(args.Executer, this))
            return;

        if (args.isLogicStateChange && PRUnitySDK.PauseManager.IsLogicPaused)
        {
            ownsLogicPause = false;
            return;
        }

        AcquireLogicPause();
    }

    private void AcquireLogicPause()
    {
        if (!setPauseWhenOpen || ownsLogicPause || PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        ownsLogicPause = true;
        PRUnitySDK.PauseManager.SetLogicPaused(true, this);
    }

    private void ReleaseLogicPause()
    {
        if (!ownsLogicPause)
            return;

        ownsLogicPause = false;
        PRUnitySDK.PauseManager.SetLogicPaused(false, this);
    }
}
