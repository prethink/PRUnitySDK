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
    /// <see langword="false"/>, если выполнение прервано и продолжать не нужно.
    /// </returns>
    public static bool Execute<T>(IReadOnlyList<T> actions, bool stopOnFailure, ref int executed)
        where T : IAction
    {
        if (actions == null)
            return true;

        foreach (T action in actions)
        {
            // Пустой элемент - обычное дело: строку в списке добавили, тип ещё не выбрали.
            if (action == null)
                continue;

            if (action.Execute())
            {
                executed++;
                continue;
            }

            if (stopOnFailure)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Проверяет, есть ли в списке хотя бы одно выполнимое сейчас действие.
    /// </summary>
    public static bool CanExecuteAny<T>(IReadOnlyList<T> actions) where T : IAction
    {
        if (actions == null)
            return false;

        foreach (T action in actions)
        {
            if (action != null && action.CanExecute())
                return true;
        }

        return false;
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
