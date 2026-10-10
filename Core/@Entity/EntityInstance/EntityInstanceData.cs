using System;

/// <summary>
/// Запись об экземпляре сущности: чем он отличается от других того же определения.
/// </summary>
/// <remarks>
/// Живёт без игрового объекта, поэтому её сохраняют, кладут в инвентарь и отдают
/// сущности, созданной заново. Свои значения сущность хранит по ключам
/// <see cref="EnumerationType{T}"/>.
/// <para>
/// <c>IIdentifiable</c> не реализует намеренно: конвертер сохранения пишет от таких
/// объектов один <c>Id</c>, и значения записи пропали бы.
/// </para>
/// </remarks>
[Serializable]
public class EntityInstanceData : StateValues, ICloneable
{
    /// <summary>
    /// Идентификатор экземпляра. Один и тот же между запусками игры и при переносе
    /// между контейнерами.
    /// </summary>
    public string InstanceId;

    /// <summary>
    /// Идентификатор определения, по которому сущность создают заново.
    /// </summary>
    public string DefinitionId;

    /// <summary>
    /// Запись нового экземпляра.
    /// </summary>
    /// <param name="definitionId">Идентификатор определения сущности.</param>
    public static EntityInstanceData Create(string definitionId)
    {
        return new EntityInstanceData
        {
            InstanceId = Guid.NewGuid().ToString("D"),
            DefinitionId = definitionId
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Копия остаётся тем же экземпляром: идентификатор не меняется.
    /// </remarks>
    public object Clone()
    {
        var clone = new EntityInstanceData
        {
            InstanceId = InstanceId,
            DefinitionId = DefinitionId
        };

        CopyValuesTo(clone);

        return clone;
    }
}
