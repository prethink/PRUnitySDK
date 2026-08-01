/// <summary>
/// Модульная реакция бота, проверяемая по Tick StateManager'а (не каждый кадр).
/// Реакция сама решает, сработала ли, и сама переключает состояние — вызывающему
/// коду достаточно знать только факт срабатывания (true/false).
/// </summary>
public interface IStateReaction<in TStateManager> : IStateReaction
    where TStateManager : IStateManager
{
    bool Initialize(TStateManager stateManager);
}

public interface IStateReaction
{
    /// <summary>Проверяет условие и, если сработало, сама выполняет переход состояния.
    /// Возвращает true, если реакция сработала (используется, чтобы не давать
    /// нескольким реакциям сработать в один и тот же тик).</summary>
    bool TryReact();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    bool CanReact();
}