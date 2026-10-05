/// <summary>
/// Реестр блоков постоянного интерфейса.
/// </summary>
/// <remarks>
/// Нужен окну отладки: блоки создают установщики на сцене, и без реестра их пришлось бы
/// искать перебором всех объектов при каждом обновлении окна.
/// </remarks>
public class HudBlockTracker : TrackerBase<IHudBlock>
{
    /// <inheritdoc />
    public override bool Register(IHudBlock element)
    {
        RemoveDestroyed();

        if (element == null || elements.Contains(element))
            return false;

        elements.Add(element);
        return true;
    }

    /// <inheritdoc />
    public override bool Unregister(IHudBlock element)
    {
        RemoveDestroyed();

        return element != null && elements.Remove(element);
    }

    /// <remarks>
    /// Блок — обычно компонент сцены. Уничтоженный вместе со сценой и не успевший сняться
    /// с учёта, он остался бы в списке ссылкой на то, чего уже нет.
    /// </remarks>
    private void RemoveDestroyed()
    {
        elements.RemoveAll(element => element == null || element is UnityEngine.Object unityObject && unityObject == null);
    }
}
