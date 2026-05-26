/// <summary>
/// Интерфейс требования работы с переводами.
/// </summary>
public interface IRequiredTranslate 
{
    /// <summary>
    /// Признак, что переводы проинициализированы.
    /// </summary>
    public bool IsInitTranslate { get; }

    /// <summary>
    /// Инициализация переводов.
    /// </summary>
    public void InitTranslate();

    /// <summary>
    /// Обновление переводов.
    /// </summary>
    public void UpdateTranslate();
}
