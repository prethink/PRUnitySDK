using System;
using UnityEngine;

[Serializable]
[SettingsDescription("Режим проекта: что считается релизом, сколько писать в лог, как разрешаются зависимости. Отсюда начинают, когда игра ведёт себя в сборке иначе, чем в редакторе.")]
public partial class PRProjectSettings
{
    [field: SerializeField] public ReleaseType ReleaseType { get; protected set; }

    /// <summary>
    /// Проект в отладочном режиме: <see cref="ReleaseType"/> — <see cref="global::ReleaseType.Debug"/>.
    /// </summary>
    /// <remarks>
    /// По нему включается всё, что нужно разработчику и не должно попасть к игроку: служебные надписи
    /// на экране, диагностика загрузки. В релизном режиме этого нет вовсе.
    /// </remarks>
    public bool IsDebug => ReleaseType == ReleaseType.Debug;

    [field: SerializeField, Range(0, 10)] public int DebugLogLevel { get; private set; }
    [field: SerializeField] public ResolveStrategy ResolveStrategy { get; protected set; }
    [field: SerializeField] public PRMonobehaviourHostSettings PRMonobehaviourHost { get; protected set; }

    [field: SerializeField] public bool PhysicsDebug { get; protected set; }

    [field: SerializeField, Header("TimeScale")] 
    public TimeScaleCombineMode TimeScaleCombineMode { get; protected set; }
}

[Serializable]
public enum ReleaseType
{
    Debug,
    Release
}

[Serializable]
public enum ResolveStrategy
{
    PriorityResolver,
    FirstResolve
}

[Serializable]
[SettingsDescription("Хост для PRMonoBehaviour без своего объекта на сцене: как он создаётся и живёт между сценами.")]
public class PRMonobehaviourHostSettings
{
    [field: SerializeField] public float Tick { get; protected set; }
}