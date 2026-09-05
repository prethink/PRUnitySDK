using UnityEngine;

/// <summary>
/// Добавляет полю с <c>[SerializeReference]</c> выбор конкретной реализации из списка.
/// </summary>
/// <remarks>
/// Unity сериализует в такое поле экземпляр произвольного класса, но выбрать тип
/// в инспекторе не даёт, и поле остаётся пустым. Атрибут добавляет выпадающий список
/// подходящих реализаций и рисует их поля на месте:
/// <code>
/// [SerializeReference, ReferenceSelector] private IAction action;
/// [SerializeReference, ReferenceSelector] private List&lt;IAction&gt; actions;
/// </code>
/// <para>
/// Реализация должна быть обычным классом с <see cref="System.SerializableAttribute"/>
/// и конструктором без параметров. ScriptableObject и MonoBehaviour в такое поле
/// не сериализуются, для них нужна обычная ссылка на ассет или компонент.
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
