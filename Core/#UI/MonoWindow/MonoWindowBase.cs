using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract partial class MonoWindowBase : PRMonoBehaviour
{
    private bool ownsLogicPause;
    private bool ownsCursor;
    private bool isShown;
    private Coroutine layoutRoutine;

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
    /// Окно наложило замедление на глобальный слой времени и должно его снять.
    /// </summary>
    private bool ownsSlowTime;

    /// <summary>
    /// Что делать со временем игры, пока окно открыто.
    /// </summary>
    /// <remarks>
    /// По умолчанию — флаг префаба <c>setPauseWhenOpen</c>. Окно, которому режим задают
    /// настройки, переопределяет свойство; флаг тогда не читается.
    /// </remarks>
    protected virtual WindowTimeMode TimeModeWhenOpen => setPauseWhenOpen ? WindowTimeMode.Pause : WindowTimeMode.None;

    /// <summary>
    /// Множитель времени для <see cref="WindowTimeMode.SlowTime"/>: 0,5 — вдвое медленнее.
    /// </summary>
    protected virtual float SlowTimeScale => 0.5f;

    /// <summary>
    /// Показывает, активно ли сейчас содержимое окна.
    /// </summary>
    /// <remarks>
    /// Окно, которое уже закрыто и только доигрывает переход, видимым не считается.
    /// </remarks>
    public bool IsVisible => isActiveAndEnabled && !isHiding && GetContainer().activeInHierarchy;

    /// <summary>
    /// Отображает окно с указанными параметрами.
    /// </summary>
    /// <remarks>
    /// Переход играет, только если окно было закрыто: повторный показ открытого окна
    /// с новыми данными его не дёргает.
    /// </remarks>
    public virtual void Show(MonoWindowArgs args)
    {
        GameObject windowContainer = GetContainer();
        bool wasHidden = !windowContainer.activeSelf || isHiding;

        if (!windowContainer.activeSelf)
            windowContainer.SetActive(true);

        windowContainer.RefreshLayoutGroupsImmediateAndRecursive();
        isShown = true;

        if (wasHidden)
            PlayShowTransition();

        AcquireWindowState();
        ScheduleLayoutRefresh();
    }

    /// <summary>
    /// Скрывает окно и освобождает принадлежащее ему состояние паузы.
    /// </summary>
    /// <param name="isForceClose">
    /// При принудительном закрытии сохранение пользовательских данных не запускается,
    /// а переход не играет: окно исчезает сразу.
    /// </param>
    public virtual void Hide(bool isForceClose = false)
    {
        GameObject windowContainer = GetContainer();
        bool wasHiding = isHiding;
        bool wasVisible = windowContainer.activeSelf && !wasHiding;
        isShown = false;

        // Принудительное закрытие не ждёт уже начатого перехода.
        if (wasHiding && isForceClose)
        {
            StopTransition();
            windowContainer.SetActive(false);
        }

        if (!wasVisible && !ownsLogicPause && !ownsCursor && !ownsSlowTime)
            return;

        if (wasVisible && !isForceClose)
            GameManager.Instance.StartSaveTask();

        if (wasVisible && (isForceClose || !TryPlayHideTransition()))
        {
            StopTransition();
            windowContainer.SetActive(false);
        }

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

        layoutRoutine = null;
        ReleaseWindowState();
        base.OnDisable();
    }

    /// <summary>
    /// Пересобирает раскладку окна в конце кадра после показа.
    /// </summary>
    /// <remarks>
    /// Подписи окно выставляет уже после <see cref="Show"/>, пока контейнер был скрыт или
    /// только что включился: раскладка, собранная до этого, остаётся прежней, и значок
    /// заголовка налезает на надпись. Смену языка отдельно ловить не нужно: у открытого
    /// окна текст перерисовывает <c>LocalizationObserver</c>, а TextMeshPro сам помечает
    /// раскладку на пересборку.
    /// </remarks>
    private void ScheduleLayoutRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (layoutRoutine != null)
            StopCoroutine(layoutRoutine);

        layoutRoutine = StartCoroutine(RefreshLayoutRoutine());
    }

    private IEnumerator RefreshLayoutRoutine()
    {
        yield return new WaitForEndOfFrame();

        layoutRoutine = null;

        GameObject windowContainer = GetContainer();
        if (windowContainer != null && windowContainer.activeInHierarchy)
            windowContainer.RefreshLayoutGroupsImmediateAndRecursive();
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

        if (TimeModeWhenOpen != WindowTimeMode.Pause || !IsVisible || args == null || object.ReferenceEquals(args.Executer, this))
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
        AcquireSlowTime();
        ownsCursor = true;
        CursorManager.Instance.Show(this);
    }

    private void ReleaseWindowState()
    {
        ReleaseLogicPause();
        ReleaseSlowTime();
        PRUnitySDK.Trackers.MonoWindows.NotifyWindowHidden(this);

        if (!ownsCursor)
            return;

        ownsCursor = false;
        CursorManager.Instance.Release(this);
    }

    private void AcquireLogicPause()
    {
        if (TimeModeWhenOpen != WindowTimeMode.Pause || ownsLogicPause || PRUnitySDK.PauseManager.IsLogicPaused)
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

    /// <summary>
    /// Замедляет мир, пока окно открыто.
    /// </summary>
    /// <remarks>
    /// Множитель ложится на глобальный слой от имени окна: снимается ровно своё, а чужие
    /// замедления (эффекты, отладка) остаются. Через этот слой замедляются физика,
    /// игровое время и аниматоры.
    /// </remarks>
    private void AcquireSlowTime()
    {
        if (TimeModeWhenOpen != WindowTimeMode.SlowTime || ownsSlowTime)
            return;

        ownsSlowTime = true;
        PRTimeScale.Instance.AddGlobalModifier(Mathf.Clamp01(SlowTimeScale), this);
    }

    private void ReleaseSlowTime()
    {
        if (!ownsSlowTime)
            return;

        ownsSlowTime = false;
        PRTimeScale.Instance.RemoveModifiers(this);
    }
}
