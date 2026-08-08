using System;

/// <summary>
/// Общая логика проверки и выполнения действий для разных базовых Unity-типов.
/// </summary>
public class ActionExecuter
{
    /// <summary>
    /// Проверяет общие условия выполнения действия.
    /// </summary>
    public bool CanExecute()
    {
        if (!PRUnitySDK.IsInitialized)
        {
            PRLog.WriteWarning(GetType(), $"Can't execute action, SDK not initialized.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Выполняет действие после проверки, предоставленной владельцем.
    /// </summary>
    /// <param name="canExecute">Полная проверка владельца, включая переопределения.</param>
    /// <param name="action">Действие для выполнения.</param>
    /// <returns>True, если проверка пройдена и действие вызвано.</returns>
    public bool Execute(Func<bool> canExecute, Action action)
    {
        if (canExecute == null)
            throw new ArgumentNullException(nameof(canExecute));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (!canExecute())
            return false;

        action();
        return true;
    }

    /// <summary>
    /// Выполняет действие, используя только общую проверку executor.
    /// </summary>
    public bool Execute(Action action)
    {
        return Execute(CanExecute, action);
    }
}
