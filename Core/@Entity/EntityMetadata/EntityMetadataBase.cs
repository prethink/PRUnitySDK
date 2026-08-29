using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityMetadataBase : ScriptableObject, IEntityMetadata
{
    [field: SerializeField] public string Name { get; protected set; }

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
