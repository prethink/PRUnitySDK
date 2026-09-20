/// <summary>
/// Как сравнивать накопленное с заданным числом.
/// </summary>
/// <remarks>
/// Имя с приставкой <c>Condition</c> намеренно: неймспейсов в SDK нет, а голое
/// <c>ComparisonOperator</c> — слишком общее слово, чтобы занимать его глобально.
/// </remarks>
public enum ConditionComparison
{
    /// <summary>
    /// Накоплено столько же или больше. Обычное «нужно набрать».
    /// </summary>
    GreaterOrEqual = 0,

    /// <summary>
    /// Накоплено строго больше.
    /// </summary>
    Greater = 1,

    /// <summary>
    /// Накоплено ровно столько.
    /// </summary>
    Equal = 2,

    /// <summary>
    /// Накоплено не столько.
    /// </summary>
    NotEqual = 3,

    /// <summary>
    /// Накоплено столько же или меньше.
    /// </summary>
    LessOrEqual = 4,

    /// <summary>
    /// Накоплено строго меньше.
    /// </summary>
    Less = 5,
}
