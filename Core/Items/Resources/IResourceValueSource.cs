/// <summary>
/// Источник значения ресурса, которое ведёт не сохранение, а другая система.
/// </summary>
/// <remarks>
/// Подключается через <see cref="ResourceManager.SetValueSource"/>. Такой ресурс только
/// читается: записать или списать его нельзя, а об изменениях источник сообщает
/// через <see cref="ResourceManager.NotifyValueSourceChanged"/>.
/// </remarks>
public interface IResourceValueSource
{
    /// <summary>
    /// Текущее значение ресурса.
    /// </summary>
    long GetValue();
}
