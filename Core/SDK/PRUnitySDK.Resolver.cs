using System;

public partial class PRUnitySDK
{
    /// <summary>
    /// Реестр сервисов SDK.
    /// </summary>
    private static IServiceResolver serviceResolver;

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, 0)]
    private static void InitializeResolver()
    {
        InitializeModuleSDK(nameof(IServiceResolver), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IServiceResolver));

            InitializeDefault(nameof(IServiceResolver), () => serviceResolver, () => { serviceResolver = new ServiceResolver(); return serviceResolver; });

            return serviceResolver;
        });
    }

    /// <summary>
    /// Возвращает зарегистрированный сервис.
    /// </summary>
    /// <exception cref="InvalidOperationException">Сервис не зарегистрирован.</exception>
    public static T ResolveService<T>() 
        where T : class
    {
        return serviceResolver.Resolve<T>();
    }

    /// <summary>
    /// Возвращает сервис, если он зарегистрирован.
    /// </summary>
    public static bool TryResolve<T>(out T service) 
        where T : class
    {
        return serviceResolver.TryResolve<T>(out service);
    }

    /// <summary>
    /// Регистрирует сервис. Работает только со стандартным резолвером.
    /// </summary>
    public static void RegisterService<T>(T service)
    {
        if (serviceResolver is not ServiceResolver defaultServiceResolver)
            throw new InvalidOperationException($"Cannot register service of type {typeof(T).FullName} because the default resolver is not being used."); 

        defaultServiceResolver.Register<T>(service);
    }
}
