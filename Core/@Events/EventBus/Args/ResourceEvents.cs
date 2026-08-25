/// <summary>
/// События игровых ресурсов.
/// </summary>
public static class ResourceEvents
{
    /// <summary>
    /// Публикует общее уведомление об изменении ресурса.
    /// </summary>
    public static void RaiseResourceUpdate(ResourceEventArgs args)
    {
        EventBus.RaiseEvent<IResourceEvent>(x => x.OnResourceUpdate(args));
    }

    /// <summary>
    /// Публикует изменение количества ресурса: сначала подписчикам со значениями,
    /// затем общим. Порядок такой же, как у событий свойств проекта - обработчик,
    /// которому нужны значения, отрабатывает до тех, кто реагирует на факт изменения.
    /// </summary>
    public static void RaiseResourceValueChange(ResourceValueChangeEventArgs args)
    {
        EventBus.RaiseEvent<IResourceValueChangedEvent>(x => x.OnResourceValueChanged(args));
        EventBus.RaiseEvent<IResourceEvent>(x => x.OnResourceUpdate(args));
    }
}
