using System;
using System.Collections.Generic;

public class ServiceResolver : IServiceResolver
{
    private readonly Dictionary<Type, object> services = new();

    public void Register<T>(T service)
    {
        services[typeof(T)] = service;
    }

    public T Resolve<T>()
    {
        if (!services.TryGetValue(typeof(T), out var service))
            throw new InvalidOperationException($"Service {typeof(T).Name} not registered.");

        return (T)service;
    }

    public bool TryResolve<T>(out T service) where T : class
    {
        if (services.TryGetValue(typeof(T), out var obj))
        {
            service = (T)obj;
            return true;
        }

        service = default;
        return false;
    }
}
