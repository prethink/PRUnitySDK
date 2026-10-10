/// <summary>
/// Определение, у которого есть префаб сущности: предмет можно поставить в мир.
/// </summary>
public interface IEntityPrefabProvider
{
    /// <summary>
    /// Префаб сущности либо <c>null</c>, если он не задан или префаб не сущность.
    /// </summary>
    EntityBase EntityPrefab { get; }
}
