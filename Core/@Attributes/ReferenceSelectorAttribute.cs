using UnityEngine;

/// <summary>
/// Добавляет полю с <c>[SerializeReference]</c> выбор конкретной реализации из списка.
/// </summary>
/// <remarks>
/// Unity умеет сериализовать в такое поле экземпляр произвольного класса, но выбирать
/// тип в инспекторе не даёт - поле остаётся пустым. Атрибут добавляет выпадающий список
/// всех подходящих реализаций и рисует их поля прямо в инспекторе.
/// <para>
/// Так действие настраивается там же, где используется, и не требует отдельного ассета:
/// <code>
/// [SerializeReference, ReferenceSelector] private IAction action;
/// [SerializeReference, ReferenceSelector] private List&lt;IAction&gt; actions;
/// </code>
/// </para>
/// <para>
/// Реализация должна быть обычным классом с <see cref="System.SerializableAttribute"/>
/// и конструктором без параметров. Наследники <see cref="Object"/> - ScriptableObject
/// и MonoBehaviour - в такое поле не сериализуются: для них используйте обычную ссылку
/// на ассет или компонент.
/// </para>
/// </remarks>
public class ReferenceSelectorAttribute : PropertyAttribute
{
    /// <summary>
    /// Показывать полное имя типа вместе с namespace.
    /// </summary>
    public bool ShowFullName { get; }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="showFullName">Показывать namespace в списке и заголовке.</param>
    public ReferenceSelectorAttribute(bool showFullName = false)
    {
        ShowFullName = showFullName;
    }
}
