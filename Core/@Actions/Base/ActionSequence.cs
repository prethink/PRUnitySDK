using System.Collections.Generic;

/// <summary>
/// Последовательное выполнение набора действий.
/// </summary>
/// <remarks>
/// Общая механика для всех, кто хранит список действий: компонента
/// <see cref="ActionRunner"/> и ассета <see cref="InlineActionPipeline"/>.
/// </remarks>
public static class ActionSequence
{
    /// <summary>
    /// Выполняет действия по порядку.
    /// </summary>
    /// <param name="actions">Список действий; пустые элементы пропускаются.</param>
    /// <param name="stopOnFailure">Прерывать выполнение на первом отказавшем действии.</param>
    /// <returns>Количество успешно выполненных действий.</returns>
    public static int Execute<T>(IReadOnlyList<T> actions, bool stopOnFailure) where T : IAction
    {
        int executed = 0;
        Execute(actions, stopOnFailure, ref executed);
        return executed;
    }

    /// <summary>
    /// Выполняет действия по порядку, накапливая счётчик между несколькими списками.
    /// </summary>
    /// <returns>
    /// При stopOnFailure — первый отказ, с отметкой <see cref="ActionResult.IsPartiallyApplied"/>,
    /// если до него что-то успело выполниться; иначе успешен, если хотя бы одно действие выполнено.
    /// </returns>
    public static ActionResult Execute<T>(IReadOnlyList<T> actions, bool stopOnFailure, ref int executed)
        where T : IAction
    {
        if (actions == null)
            return ActionResult.Success;

        int before = executed;
        ActionResult? firstFailure = null;
        foreach (T action in actions)
        {
            if (action == null)
                continue;

            ActionResult result = action.Execute();
            if (result.IsSuccess)
            {
                executed++;
                continue;
            }

            if (stopOnFailure)
                return executed > before ? result.AsPartiallyApplied() : result;
            firstFailure ??= result;
        }

        return executed > before ? ActionResult.Success : firstFailure ?? ActionResult.Success;
    }

    /// <summary>
    /// Проверяет, есть ли в списке хотя бы одно выполнимое сейчас действие.
    /// </summary>
    public static bool CanExecuteAny<T>(IReadOnlyList<T> actions) where T : IAction
    {
        return CheckAny(actions).IsSuccess;
    }

    /// <summary>
    /// Успех, если хотя бы одно действие выполнимо; иначе первый отказ из списка.
    /// </summary>
    /// <returns>Для списка без действий — <see cref="ActionLabels.EmptySequence"/>.</returns>
    public static ActionResult CheckAny<T>(IReadOnlyList<T> actions) where T : IAction
    {
        ActionResult? firstFailure = null;

        if (actions != null)
        {
            foreach (T action in actions)
            {
                if (action == null)
                    continue;

                ActionResult result = action.CanExecute();
                if (result.IsSuccess)
                    return result;

                firstFailure ??= result;
            }
        }

        return firstFailure ?? ActionResult.Fail(ActionLabels.EmptySequence);
    }

    /// <summary>
    /// Проверяет, есть ли в списке хотя бы одно заполненное действие.
    /// </summary>
    public static bool HasAny<T>(IReadOnlyList<T> actions) where T : IAction
    {
        if (actions == null)
            return false;

        foreach (T action in actions)
        {
            if (action != null)
                return true;
        }

        return false;
    }
}
