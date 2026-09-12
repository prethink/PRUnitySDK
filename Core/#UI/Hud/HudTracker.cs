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
    /// <summary>
    /// Постоянный интерфейс показан.
    /// </summary>
    public bool IsVisible { get; private set; } = true;

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
    /// Показывает либо прячет весь постоянный интерфейс.
    /// </summary>
    /// <param name="isVisible">Показать интерфейс.</param>
    public void SetVisible(bool isVisible)
    {
        if (IsVisible == isVisible)
            return;

        IsVisible = isVisible;

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
