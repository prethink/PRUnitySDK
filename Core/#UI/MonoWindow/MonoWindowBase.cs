using UnityEngine;
using UnityEngine.UI;

public abstract partial class MonoWindowBase : PRMonoBehaviour
{
    private bool ownsLogicPause;
    private bool ownsCursor;
    private bool isShown;

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
    public bool IsVisible => isActiveAndEnabled && GetContainer().activeInHierarchy;

    /// <summary>
    /// Отображает окно с указанными параметрами.
    /// </summary>
    public virtual void Show(MonoWindowArgs args)
    {
        GameObject windowContainer = GetContainer();
        if (!windowContainer.activeSelf)
            windowContainer.SetActive(true);

        windowContainer.RefreshLayoutGroupsImmediateAndRecursive();
        isShown = true;
        AcquireWindowState();
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
        isShown = false;

        if (!wasVisible && !ownsLogicPause && !ownsCursor)
            return;

        if (wasVisible && !isForceClose)
            GameManager.Instance.StartSaveTask();

        if (wasVisible)
            windowContainer.SetActive(false);

        ReleaseWindowState();
    }

    protected GameObject GetContainer()
    {
        return container != null 
            ? container 
            : gameObject;
    }

    protected virtual void ExitButtonAction()
    {
        Hide();
    }

    protected override void OnEnable()
    {
        exitButton?.onClick.AddListener(ExitButtonAction);
        base.OnEnable();

        if (isShown)
            AcquireWindowState();
    }

    protected override void OnDisable()
    {
        exitButton?.onClick.RemoveListener(ExitButtonAction);
        ReleaseWindowState();
        base.OnDisable();
    }

    protected override void RegisterEventsOnCreated()
    {
        PRUnitySDK.Trackers.MonoWindows.Register(this);
        base.RegisterEventsOnCreated();
    }

    protected override void UnRegisterEventsOnDestroy()
    {
        isShown = false;
        ReleaseWindowState();
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

    private void AcquireWindowState()
    {
        if (!IsVisible)
            return;

        PRUnitySDK.Trackers.MonoWindows.NotifyWindowShown(this);
        AcquireLogicPause();
        ownsCursor = true;
        CursorManager.Instance.Show(this);
    }

    private void ReleaseWindowState()
    {
        ReleaseLogicPause();
        PRUnitySDK.Trackers.MonoWindows.NotifyWindowHidden(this);

        if (!ownsCursor)
            return;

        ownsCursor = false;
        CursorManager.Instance.Release(this);

        if (!PRUnitySDK.Trackers.MonoWindows.HasOpenWindows && GameManager.HasInstance
            && GameManager.Instance.ReadySignal.IsReady)
            GameManager.Instance.LoadingUserCursorState();
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
