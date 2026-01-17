public partial class PRUnitySDK
{
    /// <summary>
    /// 
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
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T ResolveService<T>() 
        where T : class
    {
        //if(Settings.Project.ResolveStrategy == ResolveStrategy.PriorityResolver)
        //{
        //    if(TryResolve<T>(out var service))
        //    {
        //        return service;
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException($"Service of type {typeof(T).FullName} is not registered in the resolver.");
        //    }
        //}
        //else
        //{
        //    return serviceResolver.Resolve<T>();
        //}
        return serviceResolver.Resolve<T>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="service"></param>
    /// <returns></returns>
    public static bool TryResolve<T>(out T service) 
        where T : class
    {
        return serviceResolver.TryResolve<T>(out service);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="service"></param>
    public static void RegisterService<T>(T service)
    {
        if (serviceResolver is not ServiceResolver defaultServiceResolver)
            return;

        defaultServiceResolver.Register<T>(service);
    }
}
