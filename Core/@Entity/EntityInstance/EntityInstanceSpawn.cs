using System;

/// <summary>
/// Создаёт сущность сразу с готовой записью экземпляра.
/// </summary>
/// <remarks>
/// Запись стоит уже во время <c>InitializeEntity</c>, поэтому инициализация считает
/// характеристики по сохранённым значениям, а не по значениям нового экземпляра.
/// Сущность создаётся как обычно — пулом или <c>Instantiate</c> — внутри переданной функции.
/// <para>
/// Запись достаётся ровно одной сущности — той, которую вернула функция. Вложенная сущность
/// того же определения запись не берёт вовсе. Если её всё же забрала другая (соседняя из
/// пула), та получает новую запись, а эта переходит вернувшейся — но обе к этому времени
/// уже прошли <c>InitializeEntity</c> не со своей записью, поэтому о таком случае пишется
/// предупреждение.
/// </para>
/// <para>
/// Создание отменяется, если экземпляр уже живёт на сцене: второй предмет с тем же
/// идентификатором появляться не должен, а новый предмет — это осознанное действие игры
/// (<see cref="EntityInstanceData.Duplicate"/>), а не способ замять ошибку.
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
    /// Идёт создание сущности с записью: кто-то сейчас внутри <see cref="Create{T}"/>.
    /// </summary>
    public static bool IsCreating => current != null;

    /// <summary>
    /// Создаёт сущность и отдаёт ей запись во владение.
    /// </summary>
    /// <param name="record">Запись экземпляра; после вызова ею пользуется только сущность.</param>
    /// <param name="create">Как создать сущность: выдать из пула или инстанцировать префаб.</param>
    /// <returns>
    /// Созданная сущность. <c>null</c>, если экземпляр уже живёт на сцене: функция создания
    /// в этом случае не вызывается. Сущность другого определения возвращается без записи —
    /// проверить это можно сравнением её <c>Instance</c> с переданной записью.
    /// </returns>
    public static T Create<T>(EntityInstanceData record, Func<T> create)
        where T : class
    {
        if (create == null)
            throw new ArgumentNullException(nameof(create));

        if (record == null)
            return create();

        // Экземпляр уже живёт на сцене: вторая сущность с той же записью стала бы его двойником.
        // Копию с новым идентификатором не делаем — так ошибка передачи превращалась бы
        // в лишний предмет у игрока.
        if (EntityInstanceOwners.Find(record.InstanceId) != null)
        {
            PRLog.WriteError(typeof(EntityInstanceSpawn),
                $"Экземпляр {record.InstanceId} ('{record.DefinitionId}') уже принадлежит сущности на сцене: вторая сущность не создана.");

            return null;
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
        {
            request.Taker.SetInstance(request.Taker.CreateInstance());

            PRLog.WriteWarning(typeof(EntityInstanceSpawn),
                $"Запись экземпляра {record.InstanceId} при создании забрала не та сущность, что вернулась из функции. " +
                "Запись возвращена нужной, но обе уже прошли InitializeEntity не со своей записью: " +
                "всё, что выводится из записи, должно пересчитываться в SetInstance.");
        }

        if (owner == null || ReferenceEquals(owner.Instance, record))
            return entity;

        // Сущность не начинала жизнь внутри create (объект создан выключенным) либо запись
        // у неё перехватили: ставится следом. Но только своей: запись кирки питомцу не нужна.
        string ownerDefinition = owner.CreateInstance()?.DefinitionId;

        if (!record.Fits(ownerDefinition))
        {
            PRLog.WriteError(typeof(EntityInstanceSpawn),
                $"Запись экземпляра {record.InstanceId} относится к '{record.DefinitionId}', а создана сущность '{ownerDefinition}': запись ей не передана.");

            return entity;
        }

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
    /// <param name="nested">
    /// Сущность вложена в другую того же определения. Такая запись не берёт: она предназначена
    /// внешней, а порядок пробуждения внутри префаба Unity не обещает.
    /// </param>
    /// <returns>Запись либо <c>null</c>, если её нет, она чужая или уже отдана.</returns>
    public static EntityInstanceData Take(string definitionId, IEntityInstance taker, bool nested = false)
    {
        Request request = current;

        if (request == null || request.Taker != null || taker == null || nested)
            return null;

        if (!request.Record.Fits(definitionId))
            return null;

        request.Taker = taker;

        return request.Record;
    }
}
