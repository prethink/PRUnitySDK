/// <summary>
/// Связь <see cref="PRMonoBehaviour"/> с <see cref="PRUpdateRunner"/>.
/// </summary>
public abstract partial class PRMonoBehaviour
{
    private PRUpdateSlot updateSlot;
    private PRUpdateSlot lateUpdateSlot;
    private PRUpdateSlot fixedUpdateSlot;

    /// <summary>
    /// Прошёл ли базовый <c>Start</c>. Раньше него раннер объект не вызывает: Unity тоже
    /// не зовёт <c>Update</c> до <c>Start</c>, и созданный посреди кадра объект иначе
    /// получил бы <c>PRUpdate</c> раньше своей инициализации.
    /// </summary>
    internal bool HasStarted { get; private set; }

    /// <summary>
    /// Место объекта в списке фазы.
    /// </summary>
    internal ref PRUpdateSlot GetRunnerSlot(PRUpdatePhase phase)
    {
        if (phase == PRUpdatePhase.LateUpdate)
            return ref lateUpdateSlot;

        if (phase == PRUpdatePhase.FixedUpdate)
            return ref fixedUpdateSlot;

        return ref updateSlot;
    }

    internal void RunnerUpdate()
    {
        if (!PRPreUpdate())
            return;

        PRUpdate();
        PRPostUpdate();
    }

    internal void RunnerLateUpdate() => PRLateUpdate();

    internal void RunnerFixedUpdate() => PRFixedUpdate();

    private void MarkStarted() => HasStarted = true;
}
