using System;
using UnityEngine;
using Zenject;

public partial class PRUnitySDK
{
    private static void TryResolveModuleZInject<T>(Func<T> getProperty, Action<T> setProperty) 
        where T : class
    {
        if (getProperty() != null)
            return;

        setProperty(ProjectContext.Instance.Container.TryResolve<T>());

        if (getProperty() != null)
            PRLog.WriteDebug(typeof(PRUnitySDK), $"Resolved <color={Color.yellow}>{getProperty().GetType().Name}</color> from Zenject.");
    }

    [OverrideProperty(typeof(IServiceResolver), PrioritySDK.OVERRIDE_PROPERTY_ZINJECTION_PRIORITY)]
    private static void OverrideZInjectResolver()
    {
        serviceResolver = new ZenjectServiceResolver(ProjectContext.Instance.Container);
    }
}
