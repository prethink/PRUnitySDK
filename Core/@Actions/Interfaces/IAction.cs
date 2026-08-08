/// <summary>
/// Действие с проверкой возможности выполнения.
/// </summary>
public interface IAction
{
    /// <summary>
    /// Выполняет действие, если оно доступно.
    /// </summary>
    bool Execute();

    /// <summary>
    /// Проверяет возможность выполнения действия без изменения состояния.
    /// </summary>
    bool CanExecute();
}
