/// <summary>
/// Сущность с данными экземпляра: уровнем, улучшениями, прочностью.
/// </summary>
/// <remarks>
/// <see cref="EntityBase"/> зовёт <see cref="ResetInstance"/> перед каждой
/// <c>InitializeEntity</c>, поэтому сущность из пула не приходит с данными прошлой жизни.
/// Сохранённую запись подставляют уже после выдачи сущности, через <see cref="BindInstance"/>.
/// </remarks>
public interface IEntityInstance
{
    /// <summary>
    /// Запись экземпляра. До первой инициализации сущности равна <c>null</c>.
    /// </summary>
    EntityInstanceData Instance { get; }

    /// <summary>
    /// Заводит запись нового экземпляра взамен прежней.
    /// </summary>
    void ResetInstance();

    /// <summary>
    /// Подставляет готовую запись: из сохранения, инвентаря или от другой сущности.
    /// </summary>
    /// <param name="data">Запись экземпляра.</param>
    void BindInstance(EntityInstanceData data);
}
