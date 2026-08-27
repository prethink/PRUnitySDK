using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Компонент, выполняющий набор действий, настроенных прямо в инспекторе.
/// </summary>
/// <remarks>
/// Готовая точка входа для кнопок, триггеров и UnityEvent: вешается на объект,
/// действия выбираются из списка реализаций, ассеты создавать не нужно.
/// Ассетные действия тоже поддерживаются - отдельным списком, потому что
/// <c>[SerializeReference]</c> не хранит наследников <see cref="Object"/>.
/// </remarks>
public class ActionRunner : PRMonoBehaviour
{
    [Header("Встроенные действия")]
    [SerializeReference, ReferenceSelector]
    [Tooltip("Действия, настроенные здесь же. Выполняются сверху вниз.")]
    private List<IAction> actions = new();

    [Header("Действия-ассеты")]
    [SerializeField]
    [Tooltip("Переиспользуемые действия, вынесенные в отдельные ассеты.")]
    private List<ActionBase> assetActions = new();

    [Header("Поведение")]
    [SerializeField]
    [Tooltip("Останавливаться, если очередное действие вернуло false.")]
    private bool stopOnFailure;

    /// <summary>
    /// Выполняет все настроенные действия по порядку: сначала встроенные, затем ассеты.
    /// </summary>
    /// <returns>Количество успешно выполненных действий.</returns>
    public int Execute()
    {
        int executed = 0;

        if (!ActionSequence.Execute(actions, stopOnFailure, ref executed))
            return executed;

        ActionSequence.Execute(assetActions, stopOnFailure, ref executed);
        return executed;
    }

    /// <summary>
    /// Проверяет, есть ли хотя бы одно выполнимое сейчас действие.
    /// </summary>
    public bool CanExecuteAny()
    {
        return ActionSequence.CanExecuteAny(actions) || ActionSequence.CanExecuteAny(assetActions);
    }
}
