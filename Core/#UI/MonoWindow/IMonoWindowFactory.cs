/// <summary>
/// ‘абрика создани€ окон на основе MonoWindowBase.
/// ќпредел€ет параметры создани€ и размещени€ окна.
/// </summary>
public interface IMonoWindowFactory : IMonoBehaviourFactory
{
    /// <summary>
    /// »спользовать общий Canvas дл€ размещени€ окна.
    /// ≈сли false, окно будет добавлено в основной контейнер окон.
    /// </summary>
    bool UseSharedCanvas { get; }
}