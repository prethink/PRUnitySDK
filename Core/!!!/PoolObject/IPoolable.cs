/// <summary>
/// Интерфейс для объектов, которые могут быть управляемы через пул объектов.
/// Реализуя этот интерфейс, объект получает стандартный набор методов для 
/// регистрации, инициализации и возврата в пул.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Связанный экземпляр PoolBehaviour, который содержит логику работы с пулом.
    /// </summary>
    public PoolBehaviour PoolBehaviour { get; }

    /// <summary>
    /// Регистрирует объект в пуле. Вызывается при создании или привязке объекта к пулу.
    /// </summary>
    /// <param name="poolObject">Ссылка на объект пула, к которому привязывается IPoolable.</param>
    public void RegisterPoolObject(PoolObject poolObject);

    /// <summary>
    /// Инициализация объекта после того, как он взят из пула.
    /// Обычно используется для сброса состояния и подготовки к использованию.
    /// </summary>
    public void InitializationPoolObject();

    /// <summary>
    /// Обработка возвращения объекта в пул или его полного уничтожения.
    /// </summary>
    /// <param name="fullDestroy">Если true, объект полностью уничтожается, иначе возвращается в пул.</param>
    public void OnDestroyPool(bool fullDestroy = false);

    /// <summary>
    /// Получить ключ пула, к которому принадлежит этот объект.
    /// Используется для идентификации и управления пулами.
    /// </summary>
    /// <returns>Строковый ключ пула.</returns>
    public string GetPoolKey();
}
