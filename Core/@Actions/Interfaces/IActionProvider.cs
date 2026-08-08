/// <summary>
/// Предоставляет действие потребителю без привязки к его конкретной реализации.
/// </summary>
public interface IActionProvider
{
    /// <summary>
    /// Предоставляемое действие.
    /// </summary>
    IAction Action { get; }
}
