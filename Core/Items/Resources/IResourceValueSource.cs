/// <summary>
/// Источник значения ресурса, которое ведёт не сохранение, а другая система.
/// </summary>
/// <remarks>
/// Подключается через <see cref="ResourceManager.SetValueSource"/>. Такой ресурс только
/// читается: записать или списать его нельзя, а об изменениях источник сообщает
/// через <see cref="ResourceManager.NotifyValueSourceChanged"/>.
/// <para>
/// Итог за всё время (<see cref="ResourceManager.GetResourceTotal"/>) у такого ресурса — текущее значение
/// источника: подключайте так значения, которые не убывают, или не стройте на них рейтинг заработка.
/// </para>
/// </remarks>
public interface IResourceValueSource
{
    /// <summary>
    /// Текущее значение ресурса.
    /// </summary>
    long GetValue();
}
