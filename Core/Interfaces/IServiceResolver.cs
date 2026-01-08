public interface IServiceResolver 
{
    TContract Resolve<TContract>();
    bool TryResolve<T>(out T service) where T : class;
}
