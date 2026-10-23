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
    /// Хуки <see cref="HiddenStage"/> уже отработали после последнего показа.
    /// </summary>
    /// <remarks>
    /// Стадию зовут и <see cref="Hide"/>, и <see cref="OnDisable"/>: у окна без контейнера закрытие
    /// выключает сам объект, а закрытое окно выключается ещё раз вместе со сценой. Флаг держит
    /// один вызов на показ. Не показанному окну закрывать нечего — поэтому изначально <c>true</c>.
    /// </remarks>
    private bool hiddenHooksDone = true;

    /// <summary>
    /// Уникальный ключ окна в <see cref="MonoWindowsTracker"/>.
    /// </summary>
    public abstract Enumeration Key { get; }

    /// <summary>
    /// Стадия хуков: окно показано, и его раскладка собрана в конце кадра.
    /// </summary>
    /// <remarks>
    /// К этому моменту окна, которые собирают шапку кодом, уже завели свой крестик
    /// (<see cref="exitButton"/>), поэтому к нему можно пристраиваться. Хуки — защищённые методы
    /// с <c>[MethodHook(LayoutReadyStage)]</c>; так справка ставит кнопку «?».
    /// </remarks>
    public const string LayoutReadyStage = "MonoWindowLayoutReady";

    /// <summary>
    /// Стадия хуков: окно скрывается или выключается. Здесь закрывают то, что живёт вместе с окном.
    /// </summary>
    /// <remarks>
    /// Срабатывает один раз на каждый показ, что бы ни закрыло окно первым: <see cref="Hide"/> или выключение объекта.
    /// </remarks>
    public const string HiddenStage = "MonoWindowHidden";

    [Header("Заголовок")]
    [SerializeField] protected GameObject container;
    [SerializeField] protected RectTransform header;
    [SerializeField] protected Image iconHeader;
    [SerializeField] protected LocalizationObserver titleHeader;
    [SerializeField] protected Button exitButton;

    [Header("Тело")]
    [SerializeField] protected RectTransform body;

    [Header("Справка")]
    [SerializeField, Tooltip("Описание окна для кнопки «?» рядом с крестиком. Пусто — кнопки нет. " +
                             "Окно может задать описание кодом, переопределив HelpText.")]
    protected LocalizationControl helpText = new();

    /// <summary>
    /// Описание окна для справки; <c>null</c> — справки нет.
    /// </summary>
    /// <remarks>
    /// По умолчанию — поле префаба <see cref="helpText"/>, если в нём есть хоть одна строка.
    /// Окна, собранные кодом, переопределяют свойство и отдают свой провайдер.
    /// </remarks>
    public virtual ILocalizationProvider HelpText => helpText.OrFallback(null);

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
        hiddenHooksDone = false;

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

        RunHiddenHooks();

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

    /// <summary>
    /// Закрывает окно так же, как крестик, — если крестик есть и сейчас нажимается.
    /// </summary>
    /// <remarks>
    /// Нажатие идёт через сам крестик, а не через <see cref="Hide"/>: окно, переопределившее
    /// <see cref="ExitButtonAction"/>, и обработчики с префаба отработают как при клике. Окно без крестика
    /// или с выключенным крестиком так не закрыть — значит, закрываться ему сейчас нельзя.
    /// </remarks>
    /// <returns><c>false</c> — крестика нет, он скрыт или неактивен.</returns>
    internal bool TryPressExitButton()
    {
        if (!IsVisible || exitButton == null || !exitButton.isActiveAndEnabled || !exitButton.interactable)
            return false;

        exitButton.onClick.Invoke();
        return true;
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
        RunHiddenHooks();

        layoutRoutine = null;
        ReleaseWindowState();
        base.OnDisable();
    }

    private void RunHiddenHooks()
    {
        if (hiddenHooksDone)
            return;

        hiddenHooksDone = true;
        this.RunMethodHooks(HiddenStage);
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

        // Окно могли закрыть в том же кадре: контейнер ещё виден, пока играет переход, а хуки закрытия
        // уже отработали — готовить к показу уходящее окно нельзя.
        GameObject windowContainer = GetContainer();
        if (!isShown || isHiding || windowContainer == null || !windowContainer.activeInHierarchy)
            yield break;

        windowContainer.RefreshLayoutGroupsImmediateAndRecursive();
        this.RunMethodHooks(LayoutReadyStage);
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
