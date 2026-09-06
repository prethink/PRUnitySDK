using UnityEngine;

/// <summary>
/// Показывает поля ассета прямо в инспекторе того, кто на него ссылается.
/// </summary>
/// <remarks>
/// Ссылка на ScriptableObject в инспекторе видна только именем ассета: чтобы посмотреть
/// или поправить значение, приходится идти в Project, искать файл и возвращаться обратно.
/// С этим атрибутом поля ассета раскрываются прямо в компоненте и правятся на месте.
/// <para>
/// Правка меняет сам ассет, а значит и всех, кто на него ссылается — это общие данные,
/// а не копия для конкретного объекта. Поэтому рядом с полями рисуется путь к ассету.
/// </para>
/// <para>
/// Атрибут ставится на поле любого типа-наследника <see cref="Object"/>: рисуется тот
/// экземпляр, который реально присвоен, со своими полями. Поэтому поле, объявленное
/// базовым типом, покажет поля наследника, а поле в базовом классе работает и во всех
/// его наследниках.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [field: SerializeField, InlineAsset] public PlayerStats Stats { get; protected set; }
/// </code>
/// </example>
public class InlineAssetAttribute : PropertyAttribute
{
    /// <summary>
    /// Раскрывать поля сразу, не дожидаясь клика по треугольнику.
    /// </summary>
    public bool Expanded { get; }

    /// <summary>
    /// Показывать путь к ассету под полями.
    /// </summary>
    /// <remarks>
    /// Напоминание, что правка общая. Для мелких настроек, лежащих рядом с объектом,
    /// строку можно убрать — она занимает высоту и не несёт новости.
    /// </remarks>
    public bool ShowAssetPath { get; }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="expanded">Раскрыть поля по умолчанию.</param>
    /// <param name="showAssetPath">Показывать путь к ассету.</param>
    public InlineAssetAttribute(bool expanded = false, bool showAssetPath = true)
    {
        Expanded = expanded;
        ShowAssetPath = showAssetPath;
    }
}
