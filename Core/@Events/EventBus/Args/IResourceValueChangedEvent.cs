/// <summary>
/// Получает уведомления об изменении числового значения ресурса вместе
/// с предыдущим и текущим значениями.
/// <para>
/// Подписка на изменение именно количества: общее событие ресурсов приходит в другой
/// метод и требует приведения аргумента.
/// </para>
/// </summary>
public interface IResourceValueChangedEvent : IGlobalSubscriber
{
    /// <summary>
    /// Вызывается после изменения количества ресурса.
    /// </summary>
    void OnResourceValueChanged(ResourceValueChangeEventArgs args);
}
