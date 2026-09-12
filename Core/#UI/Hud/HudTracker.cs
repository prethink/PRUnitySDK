using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Реестр постоянного интерфейса и его общая видимость.
/// </summary>
/// <remarks>
/// Постоянный интерфейс собирается из разных модулей и живёт на разных canvas: экранный
/// у полос и панели, свой мировой у каждой полосы над сущностью. Общего выключателя у них
/// нет, поэтому состояние держит трекер, а элементы получают его по регистрации.
/// <para>
/// Доступ: <c>PRUnitySDK.Trackers.Hud</c>. Удобные обёртки — <c>PRUnitySDK.Windows.ShowHud()</c>
/// и <c>HideHud()</c>.
/// </para>
/// </remarks>
public class HudTracker : TrackerBase<IHudElement>
{
    private readonly HashSet<object> hideSources = new();

    private bool isVisibleSet = true;
    private bool isVisibleApplied = true;

    /// <summary>
    /// Постоянный интерфейс показан.
    /// </summary>
    /// <remarks>
    /// Итог двух вещей: заданного состояния и просьб спрятать. Пока хоть один источник
    /// просит скрыть, интерфейса не видно.
    /// </remarks>
    public bool IsVisible => isVisibleSet && hideSources.Count == 0;

    /// <summary>
    /// Регистрирует элемент и сразу приводит его к текущему состоянию.
    /// </summary>
    /// <remarks>
    /// Элемент, созданный после скрытия, событие уже пропустил — без этого он вышел бы
    /// на экран поверх спрятанного интерфейса.
    /// </remarks>
    /// <param name="element">Элемент постоянного интерфейса.</param>
    /// <returns>Элемент зарегистрирован.</returns>
    public override bool Register(IHudElement element)
    {
        RemoveDestroyedElements();

        if (element == null || elements.Contains(element))
            return false;

        elements.Add(element);
        element.SetHudVisible(IsVisible);

        return true;
    }

    /// <summary>
    /// Снимает элемент с учёта.
    /// </summary>
    /// <param name="element">Элемент постоянного интерфейса.</param>
    /// <returns>Элемент был зарегистрирован.</returns>
    public override bool Unregister(IHudElement element)
    {
        RemoveDestroyedElements();

        return element != null && elements.Remove(element);
    }

    /// <summary>
    /// Задаёт постоянное состояние интерфейса.
    /// </summary>
    /// <remarks>
    /// Для временного скрытия — <see cref="Hide"/> и <see cref="Release"/>: они возвращают
    /// то состояние, что было до них, и не спорят друг с другом.
    /// </remarks>
    /// <param name="isVisible">Показать интерфейс.</param>
    public void SetVisible(bool isVisible)
    {
        isVisibleSet = isVisible;

        Apply();
    }

    /// <summary>
    /// Просит спрятать интерфейс на время.
    /// </summary>
    /// <remarks>
    /// Источник — тот, кто прячет: катсцена, окно, ролик открытия кейса. Пока он не отпустил,
    /// интерфейс скрыт, и второй источник поверх ничего не ломает. Возвращать прежнее
    /// состояние вручную не нужно: его вернёт <see cref="Release"/>, когда отпустят все.
    /// </remarks>
    /// <param name="source">Кто просит скрыть.</param>
    public void Hide(object source)
    {
        if (source == null || !hideSources.Add(source))
            return;

        Apply();
    }

    /// <summary>
    /// Отпускает просьбу источника скрыть интерфейс.
    /// </summary>
    /// <param name="source">Кто просил скрыть.</param>
    public void Release(object source)
    {
        if (source == null || !hideSources.Remove(source))
            return;

        Apply();
    }

    /// <summary>
    /// Источник просит скрыть интерфейс прямо сейчас.
    /// </summary>
    /// <param name="source">Кто просил скрыть.</param>
    /// <returns>Просьба этого источника в силе.</returns>
    public bool IsHiddenBy(object source) => source != null && hideSources.Contains(source);

    /// <summary>
    /// Снимает все просьбы скрыть интерфейс.
    /// </summary>
    /// <remarks>
    /// На смену сцены и сброс сессии: источник, уничтоженный без отпускания, иначе держал бы
    /// интерфейс скрытым. Обычный путь — <see cref="Release"/> в паре к <see cref="Hide"/>.
    /// </remarks>
    public void ReleaseAll()
    {
        if (hideSources.Count == 0)
            return;

        hideSources.Clear();

        Apply();
    }

    /// <summary>
    /// Приводит элементы к текущему состоянию.
    /// </summary>
    private void Apply()
    {
        // Источник могли уничтожить, не отпустив: иначе интерфейс остался бы скрытым навсегда.
        hideSources.RemoveWhere(source => source is Object unityObject && unityObject == null);

        bool isVisible = IsVisible;

        if (isVisibleApplied == isVisible)
            return;

        isVisibleApplied = isVisible;

        RemoveDestroyedElements();

        // Копия списка: элемент вправе сняться с учёта прямо в обработчике.
        foreach (IHudElement element in elements.ToList())
            element.SetHudVisible(isVisible);

        // Событие — для тех, кто не элемент: аналитики, звука, чужого кода.
        HudVisibilityEvents.RaiseVisibilityChanged(isVisible);
    }

    /// <summary>
    /// Возвращает зарегистрированные элементы нужного вида.
    /// </summary>
    /// <typeparam name="T">Вид элемента.</typeparam>
    /// <returns>Найденные элементы.</returns>
    public IEnumerable<T> GetElements<T>()
        where T : class, IHudElement
    {
        RemoveDestroyedElements();

        return elements.OfType<T>();
    }

    /// <summary>
    /// Выбрасывает уничтоженные элементы.
    /// </summary>
    /// <remarks>
    /// Полосы уничтожаются вместе со сценой и сущностями, а сняться с учёта успевают
    /// не всегда: сравнение с <c>null</c> у Unity-объекта делается через его же оператор,
    /// поэтому проверяем приведением к <see cref="Object"/>.
    /// </remarks>
    private void RemoveDestroyedElements()
    {
        elements.RemoveAll(element => element == null || element is Object unityObject && unityObject == null);
    }
}
