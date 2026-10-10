using System;

/// <summary>
/// Создаёт сущность сразу с готовой записью экземпляра.
/// </summary>
/// <remarks>
/// Запись стоит уже во время <c>InitializeEntity</c>, поэтому инициализация считает
/// характеристики по сохранённым значениям, а не по значениям нового экземпляра.
/// Сущность создаётся как обычно — пулом или <c>Instantiate</c> — внутри переданной функции.
/// </remarks>
public static class EntityInstanceSpawn
{
    /// <summary>
    /// Запись, которую ждёт создаваемая сейчас сущность.
    /// </summary>
    private static EntityInstanceData pending;

    /// <summary>
    /// Создаёт сущность и отдаёт ей запись во владение.
    /// </summary>
    /// <param name="record">Запись экземпляра; после вызова ею пользуется только сущность.</param>
    /// <param name="create">Как создать сущность: выдать из пула или инстанцировать префаб.</param>
    public static T Create<T>(EntityInstanceData record, Func<T> create)
        where T : EntityBase
    {
        if (create == null)
            throw new ArgumentNullException(nameof(create));

        // Прежнее значение возвращается: создание одной сущности может вызвать создание другой.
        EntityInstanceData previous = pending;
        pending = record;

        T entity;

        try
        {
            entity = create();
        }
        finally
        {
            pending = previous;
        }

        // Сущность не начинала жизнь внутри create — например, объект создан выключенным.
        if (record != null && entity is IEntityInstance instance && !ReferenceEquals(instance.Instance, record))
            instance.SetInstance(record);

        return entity;
    }

    /// <summary>
    /// Забирает ожидающую запись, если она для сущности этого определения.
    /// </summary>
    /// <remarks>
    /// Сверка по определению нужна вложенным сущностям: вместе с префабом просыпаются
    /// и его дочерние сущности, и чужую запись они забрать не должны.
    /// </remarks>
    internal static EntityInstanceData Take(string definitionId)
    {
        if (pending == null || pending.DefinitionId != definitionId)
            return null;

        EntityInstanceData record = pending;
        pending = null;

        return record;
    }
}
