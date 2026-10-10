using System;

/// <summary>
/// Создаёт сущность сразу с готовой записью экземпляра.
/// </summary>
/// <remarks>
/// Запись стоит уже во время <c>InitializeEntity</c>, поэтому инициализация считает
/// характеристики по сохранённым значениям, а не по значениям нового экземпляра.
/// Сущность создаётся как обычно — пулом или <c>Instantiate</c> — внутри переданной функции.
/// <para>
/// Запись достаётся ровно одной сущности — той, которую вернула функция. Если раньше её
/// забрала другая (вложенная того же определения, соседняя из пула), та получает новую
/// запись, а эта переходит вернувшейся.
/// </para>
/// </remarks>
public static class EntityInstanceSpawn
{
    /// <summary>
    /// Одна передача записи: что передают и кто уже забрал.
    /// </summary>
    private sealed class Request
    {
        public EntityInstanceData Record;
        public IEntityInstance Taker;
    }

    private static Request current;

    /// <summary>
    /// Создаёт сущность и отдаёт ей запись во владение.
    /// </summary>
    /// <param name="record">Запись экземпляра; после вызова ею пользуется только сущность.</param>
    /// <param name="create">Как создать сущность: выдать из пула или инстанцировать префаб.</param>
    public static T Create<T>(EntityInstanceData record, Func<T> create)
        where T : class
    {
        if (create == null)
            throw new ArgumentNullException(nameof(create));

        if (record == null)
            return create();

        // Экземпляр уже живёт на сцене: вторая сущность с той же записью стала бы его двойником.
        // Игру это не останавливает, но предмет раздваивается — ошибка в коде передачи.
        if (EntityInstanceOwners.Find(record.InstanceId) != null)
        {
            PRLog.WriteError(typeof(EntityInstanceSpawn),
                $"Экземпляр {record.InstanceId} уже принадлежит сущности на сцене: новая сущность получит его копию с другим идентификатором.");

            record = record.Duplicate();
        }

        // Прежняя передача возвращается: создание одной сущности может вызвать создание другой.
        Request previous = current;
        var request = new Request { Record = record };
        current = request;

        T entity;

        try
        {
            entity = create();
        }
        finally
        {
            current = previous;
        }

        var owner = entity as IEntityInstance;

        if (request.Taker != null && !ReferenceEquals(request.Taker, owner))
            request.Taker.SetInstance(request.Taker.CreateInstance());

        // Сущность не начинала жизнь внутри create (объект создан выключенным) либо запись
        // у неё перехватили: ставится следом, уже после InitializeEntity.
        if (owner != null && !ReferenceEquals(owner.Instance, record))
            owner.SetInstance(record);

        return entity;
    }

    /// <summary>
    /// Забирает ожидающую запись для сущности, которая начинает жизнь.
    /// </summary>
    /// <remarks>
    /// Зовёт <see cref="EntityBase"/> перед <c>InitializeEntity</c>. Запись отдаётся один раз
    /// и только сущности того же определения: вместе с префабом просыпаются его дочерние
    /// сущности, и чужую запись они забрать не должны.
    /// </remarks>
    /// <param name="definitionId">Определение сущности, которая спрашивает.</param>
    /// <param name="taker">Сама сущность.</param>
    /// <returns>Запись либо <c>null</c>, если её нет, она чужая или уже отдана.</returns>
    public static EntityInstanceData Take(string definitionId, IEntityInstance taker)
    {
        Request request = current;

        if (request == null || request.Taker != null || taker == null || request.Record.DefinitionId != definitionId)
            return null;

        request.Taker = taker;

        return request.Record;
    }
}
