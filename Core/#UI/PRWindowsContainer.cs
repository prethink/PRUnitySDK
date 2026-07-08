using System;
using Unity.VisualScripting;
using UnityEngine;

public partial class PRWindowsContainer 
{
    /// <summary>
    /// Контейнер для окон.   
    /// </summary>
    public PRContainer Windows;

    /// <summary>
    /// Контейнер для уведомлений.   
    /// </summary>
    public PRContainer Notifiers;

    public RewardNotifier RewardNotifier;

    public void Initialize()
    {
        this.RunMethodHooks(MethodHookStage.PreOperation);

        Windows = MonoBehaviourUtils.CreateContainer("Windows");
        InitializeNotifiers();

        this.RunMethodHooks(MethodHookStage.PostOperation);
    }

    private void InitializeNotifiers()
    {
        Notifiers = MonoBehaviourUtils.CreateContainer("Notifiers");

        var providers = ReflectionExtension.FindClassesImplementingInterface<INotifierFactory>();
        foreach (var provider in providers)
        {
            //TODO;
            var factory = Activator.CreateInstance(provider) as INotifierFactory;
            Debug.Log(factory.ResourcePath);
            var instance = UnityEngine.Object.Instantiate(Resources.Load(factory.ResourcePath));
            instance.GameObject().transform.SetParent(Notifiers.transform);
            RewardNotifier = instance.GetComponent<RewardNotifier>();
        }
    }
}
