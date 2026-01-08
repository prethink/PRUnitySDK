using Zenject;

public sealed class ZenjectServiceResolver : IServiceResolver
{
    private readonly DiContainer container;

    public ZenjectServiceResolver(DiContainer container)
    {
        this.container = container;
    }

    public T Resolve<T>() => container.Resolve<T>();

    public bool TryResolve<T>(out T service) where T : class
    {
        service = container.TryResolve<T>();
        return service != null;
    }
}
