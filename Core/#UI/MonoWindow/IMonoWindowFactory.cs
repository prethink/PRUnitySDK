/// <summary>
/// Фабрика создания окон на основе MonoWindowBase.
/// Определяет параметры создания и размещения окна.
/// </summary>
public interface IMonoWindowFactory : IMonoBehaviourFactory
{
    /// <summary>
    /// Использовать общий Canvas для размещения окна.
    /// Если false, окно будет добавлено в основной контейнер окон.
    /// </summary>
    bool UseSharedCanvas { get; }

    /// <summary>
    /// Создаёт окно.
    /// </summary>
    /// <remarks>
    /// Без типа окна: так его создаёт реестр, который знает только ключ и фабрику
    /// (<see cref="MonoWindowsTracker.RegisterLazy"/>).
    /// </remarks>
    MonoWindowBase CreateWindow();
}