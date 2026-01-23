/// <summary>
/// Токен отмены действий. 
/// Если установлен в состояние отмены, действие или корутина не будут выполнены.
/// </summary>
public class PRToken
{
    /// <summary>
    /// Флаг, указывающий, что операция была отменена.
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// Отменяет запланированное действие.
    /// </summary>
    public void Cancel()
    {
        if (IsCancelled)
            return;

        IsCancelled = true;
    }
}