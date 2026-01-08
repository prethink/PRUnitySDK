public partial class PRUnitySDK
{
    /// <summary>
    /// 
    /// </summary>
    private static IServiceResolver serviceResolver;


    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [InitializeMethod(InitializeType.SDK, 0)]
    private static void InitializeResolver()
    {
        InitializeModuleSDK(nameof(IServiceResolver), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IServiceResolver));

            InitializeDefault(nameof(IServiceResolver), () => serviceResolver, () => { serviceResolver = new ServiceResolver(); return serviceResolver; });

            return serviceResolver;
        });
    }

    public static T ResolveService<T>() where T : class
    {
        return serviceResolver.Resolve<T>();
    }

    public static bool TryResolve<T>(out T service) where T : class
    {
        return serviceResolver.TryResolve<T>(out service);
    }

    public static void RegisterService<T>(T service)
    {
        if (serviceResolver is not ServiceResolver defaultServiceResolver)
            return;

        defaultServiceResolver.Register<T>(service);
    }
}
