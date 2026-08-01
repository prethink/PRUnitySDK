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
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    bool CanReact();
}