public interface IStateBackgroundReaction : IStateReaction
{
    /// <summary>Проверяет условие и, если сработало, сама выполняет переход состояния.
    /// Возвращает true, если реакция сработала (используется, чтобы не давать
    /// нескольким реакциям сработать в один и тот же тик).</summary>
    bool TryReact();
}
