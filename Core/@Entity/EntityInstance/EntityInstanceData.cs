using System;

/// <summary>
/// Запись об экземпляре сущности: чем он отличается от других того же определения.
/// </summary>
/// <remarks>
/// Живёт без игрового объекта, поэтому её сохраняют, кладут в инвентарь и отдают
/// сущности, созданной заново. Свои значения сущность хранит по ключам
/// <see cref="EnumerationType{T}"/>. Сюда идёт постоянное: уровень, улучшения, прочность.
/// То, что верно только сейчас — «в руках», «идёт откат», — остаётся в самой сущности.
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
    /// Значение записи изменилось.
    /// </summary>
    /// <remarks>
    /// Подписывается владелец записи, чтобы пересчитать то, что из неё выводит. Подписчики
    /// не сохраняются и в копии не переходят. Загрузка из сохранения событие не поднимает.
    /// </remarks>
    public event Action Changed;

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

    /// <summary>
    /// У записи есть оба идентификатора. Запись без них из сохранения не восстановить.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(InstanceId) && !string.IsNullOrEmpty(DefinitionId);
    }

    /// <summary>
    /// Запись относится к сущности этого определения.
    /// </summary>
    public bool Fits(string definitionId)
    {
        return !string.IsNullOrEmpty(definitionId) && DefinitionId == definitionId;
    }

    /// <summary>
    /// Снимок записи: тот же экземпляр на момент вызова.
    /// </summary>
    /// <remarks>
    /// Идентификатор не меняется, поэтому снимок годится для сохранения и не годится
    /// как второй предмет: два предмета с одним идентификатором инвентарь сочтёт одним.
    /// Для второго предмета есть <see cref="Duplicate"/>.
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

    /// <summary>
    /// Запись ещё одного, самостоятельного экземпляра с теми же значениями.
    /// </summary>
    public EntityInstanceData Duplicate()
    {
        EntityInstanceData copy = Create(DefinitionId);
        CopyValuesTo(copy);

        return copy;
    }

    /// <inheritdoc />
    protected override void OnValuesChanged()
    {
        Changed?.Invoke();
    }
}
