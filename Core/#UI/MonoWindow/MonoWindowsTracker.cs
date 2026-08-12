using System.Linq;

/// <summary>
/// Хранит UI-окна с уникальными ключами и управляет их отображением.
/// </summary>
public class MonoWindowsTracker : TrackerBase<MonoWindowBase>, IMonoWindowEvents
{
    private static MonoWindowsTracker eventSubscriber;

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

        var requiredWindow = elements.FirstOrDefault(x => x != null && x.Key == key);
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

        window = elements.FirstOrDefault(x => x != null && x.Key == key) as T;
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
    }

    /// <summary>
    /// Обновляет трекер после скрытия окна.
    /// </summary>
    internal void NotifyWindowHidden(MonoWindowBase window)
    {
        if (CurrentWindow == window)
            CurrentWindow = elements.LastOrDefault(x => x != null && x.IsVisible);

        UpdateGlobalWindowState();
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
