using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Хранит UI-окна с уникальными ключами и управляет их отображением.
/// </summary>
public class MonoWindowsTracker : TrackerBase<MonoWindowBase>, IMonoWindowEvents
{
    private static MonoWindowsTracker eventSubscriber;

    /// <summary>
    /// Окна, о показе которых уже сообщено (<see cref="IMonoWindowVisibilityEvent"/>).
    /// </summary>
    private readonly HashSet<MonoWindowBase> shownWindows = new();

    /// <summary>
    /// Окна, которые зарегистрированы описанием и ещё не созданы: ключ и фабрика.
    /// </summary>
    private readonly Dictionary<Enumeration, IMonoWindowFactory> lazyWindows = new();

    /// <summary>
    /// Текущее видимое окно либо <see langword="null"/>.
    /// </summary>
    public MonoWindowBase CurrentWindow { get; private set; }

    /// <summary>
    /// Показывает, что в трекере осталось хотя бы одно открытое окно.
    /// </summary>
    public bool HasOpenWindows =>
        CurrentWindow != null && CurrentWindow.IsVisible ||
        elements.Any(x => x != null && x.IsVisible);

    /// <summary>
    /// Создаёт трекер и подписывает его на глобальные команды MonoWindow.
    /// </summary>
    public MonoWindowsTracker()
    {
        if (eventSubscriber != null)
            EventBus.Unsubscribe(eventSubscriber);

        eventSubscriber = this;
        EventBus.Subscribe(this);
    }

    /// <summary>
    /// Регистрирует ненулевое окно, если объект и его ключ ещё не заняты.
    /// </summary>
    public override bool Register(MonoWindowBase element)
    {
        RemoveDestroyedWindows();

        if (element == null)
            return false;

        if (element.Key == null)
        {
            PRLog.WriteWarning(element, "MonoWindow без ключа не может быть зарегистрировано.");
            return false;
        }

        if (elements.Contains(element))
            return false;

        MonoWindowBase duplicate = elements.FirstOrDefault(x => x != null && x.Key == element.Key);
        if (duplicate != null)
        {
            PRLog.WriteWarning(element,
                $"MonoWindow с ключом '{element.Key}' уже зарегистрировано объектом '{duplicate.name}'.");
            return false;
        }

        elements.Add(element);

        if (element.IsVisible)
            NotifyWindowShown(element);

        return true;
    }

    /// <summary>
    /// Регистрирует окно описанием: само окно создаётся при первом обращении по ключу.
    /// </summary>
    /// <remarks>
    /// Окно, созданное при запуске, стоит времени запуска, даже если игрок его ни разу не откроет.
    /// Описание ничего не стоит: префаб грузится и создаётся, когда окно понадобилось.
    /// <para>
    /// Подходит только окну, которому незачем жить закрытым. Окно, которое показывает себя само
    /// или слушает события игры, пока закрыто, создают сразу — до первого показа его просто нет.
    /// </para>
    /// </remarks>
    /// <param name="key">Ключ окна; должен совпадать с <see cref="MonoWindowBase.Key"/> созданного.</param>
    /// <param name="factory">Чем окно создаётся.</param>
    /// <returns><see langword="false"/>, если ключ уже занят окном или другим описанием.</returns>
    public bool RegisterLazy(Enumeration key, IMonoWindowFactory factory)
    {
        if (key == null || factory == null)
            return false;

        if (lazyWindows.ContainsKey(key) || elements.Any(x => x != null && x.Key == key))
        {
            PRLog.WriteWarning(typeof(MonoWindowsTracker), $"MonoWindow с ключом '{key}' уже зарегистрировано.");
            return false;
        }

        lazyWindows.Add(key, factory);
        return true;
    }

    /// <summary>
    /// Находит окно по ключу; зарегистрированное описанием — создаёт.
    /// </summary>
    private MonoWindowBase FindOrCreate(Enumeration key)
    {
        MonoWindowBase window = elements.FirstOrDefault(x => x != null && x.Key == key);

        if (window != null || !lazyWindows.TryGetValue(key, out IMonoWindowFactory factory))
            return window;

        // Описание снимается до создания: не вышло один раз — повторять на каждый вызов незачем.
        lazyWindows.Remove(key);
        window = factory.CreateWindow();

        if (window == null)
        {
            PRLog.WriteError(typeof(MonoWindowsTracker), $"MonoWindow с ключом '{key}' не удалось создать.");
            return null;
        }

        if (window.Key != key)
        {
            PRLog.WriteWarning(window,
                $"MonoWindow зарегистрировано с ключом '{key}', а его собственный ключ — '{window.Key}'.");
        }

        return window;
    }

    /// <summary>
    /// Удаляет ранее зарегистрированное окно.
    /// </summary>
    public override bool Unregister(MonoWindowBase element)
    {
        if (element == null)
            return false;

        bool removed = elements.Remove(element);
        if (!removed)
            return false;

        if (CurrentWindow == element)
            CurrentWindow = elements.LastOrDefault(x => x != null && x.IsVisible);

        UpdateGlobalWindowState();
        return true;
    }

    /// <summary>
    /// Скрывает все открытые окна с обычным завершением их работы.
    /// </summary>
    public void HideAllWindows()
    {
        HideWindows(isForceClose: false);
    }

    /// <summary>
    /// Принудительно скрывает все открытые окна без запуска сохранения.
    /// </summary>
    public void HideForceAllWindows()
    {
        HideWindows(isForceClose: true);
    }

    /// <summary>
    /// Пытается показать окно по типизированному ключу.
    /// </summary>
    public bool TryShowWindow(Enumeration key, MonoWindowArgs args)
    {
        if (key == null)
            return false;

        var requiredWindow = FindOrCreate(key);
        if (requiredWindow == null)
            return false;

        requiredWindow.Show(args ?? new MonoWindowArgsEmpty());
        return true;
    }

    /// <summary>
    /// Пытается показать окно по типизированному ключу без дополнительных данных.
    /// </summary>
    public bool TryShowWindow(Enumeration key)
    {
        return TryShowWindow(key, new MonoWindowArgsEmpty());
    }

    /// <inheritdoc />
    public bool TryShowWindow(string key)
    {
        return TryShowWindow(Enumeration.GetOrCreate(key));
    }

    /// <inheritdoc />
    public bool TryShowWindow(string key, MonoWindowArgs args)
    {
        return TryShowWindow(Enumeration.GetOrCreate(key), args);
    }

    public bool TryGetWindow<T>(Enumeration key, out T window) 
        where T : MonoWindowBase
    {
        window = null;

        if (key == null)
            return false;

        window = FindOrCreate(key) as T;
        return window != null;
    }

    /// <summary>
    /// Обновляет трекер после прямого вызова <see cref="MonoWindowBase.Show"/>.
    /// </summary>
    internal void NotifyWindowShown(MonoWindowBase window)
    {
        if (window == null)
            return;

        foreach (MonoWindowBase openedWindow in elements.ToList())
        {
            if (openedWindow != null && openedWindow != window && openedWindow.IsVisible)
                openedWindow.Hide();
        }

        CurrentWindow = window;
        UpdateGlobalWindowState();

        // Окно сообщает о показе и при повторном включении — событие только на первый раз.
        if (shownWindows.Add(window))
            EventBus.RaiseEvent<IMonoWindowVisibilityEvent>(x => x.OnWindowShown(window));
    }

    /// <summary>
    /// Обновляет трекер после скрытия окна.
    /// </summary>
    internal void NotifyWindowHidden(MonoWindowBase window)
    {
        if (CurrentWindow == window)
            CurrentWindow = elements.LastOrDefault(x => x != null && x.IsVisible);

        UpdateGlobalWindowState();

        // Скрытие приходит и от выключения окна, которое не открывали: такое не событие.
        if (shownWindows.Remove(window))
            EventBus.RaiseEvent<IMonoWindowVisibilityEvent>(x => x.OnWindowHidden(window));
    }

    private void HideWindows(bool isForceClose)
    {
        foreach (MonoWindowBase window in elements.ToList())
        {
            if (window != null && window.IsVisible)
                window.Hide(isForceClose);
        }

        UpdateGlobalWindowState();
    }

    private void RemoveDestroyedWindows()
    {
        elements.RemoveAll(x => x == null);

        if (CurrentWindow == null)
            CurrentWindow = elements.LastOrDefault(x => x != null && x.IsVisible);
    }

    private void UpdateGlobalWindowState()
    {
        RemoveDestroyedWindows();
        PRUnitySDK.SetWindowsState(HasOpenWindows);
    }
}
