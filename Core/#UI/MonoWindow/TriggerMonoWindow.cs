using UnityEngine;

/// <summary>
/// Открывает окно, когда в триггер входит игрок.
/// </summary>
/// <remarks>
/// По умолчанию окно открывает любой игрок на коллайдере. Кто именно имеет право
/// его открыть — правило проектное: игра добавляет свои хуки стадии
/// <see cref="ExecutorStage"/>. Без единого хука компонент работает с поведением
/// по умолчанию.
/// </remarks>
public partial class TriggerMonoWindow : PRMonoBehaviour
{
    /// <summary>
    /// Стадия хука «кто открывает окно по триггеру».
    /// </summary>
    /// <remarks>
    /// Сигнатура хука — <c>void Имя(Collider other, ref long executor, ref bool hasExecutor)</c>.
    /// На входе игрок, найденный по умолчанию, и признак того, найден ли он вообще.
    /// Хук может назначить своего исполнителя или запретить открытие, сбросив
    /// <c>hasExecutor</c> в <c>false</c>. Хуков может быть несколько, они идут
    /// по возрастанию <c>Order</c> и видят результат предыдущего. Метод должен быть
    /// <c>protected</c> или выше: reflection не находит private методы базового класса
    /// у наследника.
    /// </remarks>
    public const string ExecutorStage = "TriggerMonoWindowExecutor";

    [SerializeField] private EnumerationReference<MonoWindowKeyEnumerations> windowKey;

    protected override void PROnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerBase>();
        bool hasExecutor = player != null;

        var hookArgs = new object[] { other, hasExecutor ? player.PlayerId : 0L, hasExecutor };
        this.RunMethodHooks(ExecutorStage, hookArgs);

        if (hookArgs[2] is not bool found || !found)
            return;

        if (hookArgs[1] is not long executor)
            return;

        var args = new MonoWindowArgsEmpty
        {
            Executor = executor
        };

        PRUnitySDK.Trackers.MonoWindows.TryShowWindow(windowKey.ToEnumeration(), args);
    }
}
