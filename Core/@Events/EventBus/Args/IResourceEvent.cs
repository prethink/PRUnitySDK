/// <summary>
/// Получает уведомления о любых изменениях игровых ресурсов.
/// <para>
/// Подходит для подписчиков, которым достаточно факта изменения: обновить экран,
/// пересчитать доступность покупок, записать метрику. Если нужны сами значения,
/// подписывайтесь на <see cref="IResourceValueChangedEvent"/> - там они приходят
/// готовыми, без приведения типа аргумента.
/// </para>
/// </summary>
public interface IResourceEvent : IGlobalSubscriber
{
    /// <summary>
    /// Вызывается после изменения ресурса.
    /// </summary>
    void OnResourceUpdate(ResourceEventArgs args);
}
