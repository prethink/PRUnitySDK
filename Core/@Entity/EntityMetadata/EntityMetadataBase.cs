using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityMetadataBase : ScriptableObject, IEntityMetadata
{
    /// <summary>
    /// Вид сущности, который описывает ассет.
    /// </summary>
    /// <remarks>
    /// Лежит здесь, а не в коде сущности, чтобы <see cref="Entity"/> можно было повесить
    /// на префаб без написания класса: тип - последнее, что оставалось объявлять кодом.
    /// <para>
    /// У сущностей с определением вид по-прежнему объявляется в классе: там ассет описывает
    /// позицию, а не вид, и запись вида в него означала бы одно и то же слово в сотне
    /// ассетов - с сотней возможностей ошибиться.
    /// </para>
    /// </remarks>
    [field: SerializeField, Header("Вид")]
    public EnumerationReference<EntityTypeEnumerations> EntityType { get; protected set; }

    [field: SerializeField, Header("Описание")] public string Name { get; protected set; }

    [field:SerializeField] public Sprite Icon { get; protected set; }
    [field: SerializeField] public QualityType Quality { get; protected set; }

    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localization { get; private set; }

    /// <summary>
    /// Ключ перевода описания.
    /// </summary>
    /// <remarks>
    /// Префикс остался от прежнего имени класса намеренно: ключи уже разошлись по словарям
    /// и сохранениям, и переименование тихо оборвало бы переводы у всех сущностей разом.
    /// </remarks>
    public string LocalizationKey => $"EntityInfo_{Name.ToLower()}";

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localization;
}
