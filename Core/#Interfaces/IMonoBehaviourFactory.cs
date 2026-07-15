/// <summary>
/// Предоставляет настройки создания экземпляров MonoBehaviour.
/// </summary>
public interface IMonoBehaviourFactory : IResourcePathProvider
{
    /// <summary>
    /// Определяет, требуется ли использовать единственный экземпляр объекта.
    /// При повторном запросе вместо создания нового экземпляра
    /// будет возвращён ранее созданный объект.
    /// </summary>
    bool IsSingleton { get; }

    /// <summary>
    /// Сохранять мировую позицию объекта при изменении родителя.
    /// Используется при вызове Transform.SetParent.
    /// </summary>
    bool WorldPositionStays { get; }
}