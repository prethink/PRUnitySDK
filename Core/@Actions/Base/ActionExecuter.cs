using System;

/// <summary>
/// Общая логика проверки и выполнения действий.
/// </summary>
public class ActionExecuter
{
    public ActionResult CanExecute()
    {
        return PRUnitySDK.IsInitialized
            ? ActionResult.Success
            : ActionResult.Fail(ActionLabels.GameNotReady);
    }

    /// <summary>
    /// Проверяет доступность один раз и сохраняет результат владельца.
    /// </summary>
    public ActionResult Execute(Func<ActionResult> canExecute, Func<ActionResult> action)
    {
        if (canExecute == null)
            throw new ArgumentNullException(nameof(canExecute));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        ActionResult availability = canExecute();
        return availability.IsFailed ? availability : action();
    }

    public ActionResult Execute(Func<ActionResult> action)
    {
        return Execute(CanExecute, action);
    }
}
