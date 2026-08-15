/// <summary>
/// Описывает одну проблему, найденную при проверке базы.
/// </summary>
public sealed class DatabaseValidationIssue
{
    /// <summary>
    /// Стабильный код правила валидации.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Сообщение для разработчика.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Уровень важности проблемы.
    /// </summary>
    public DatabaseValidationSeverity Severity { get; }

    /// <summary>
    /// Индекс проблемного элемента либо <c>-1</c>, если проблема относится ко всей базе.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Создаёт описание одной проблемы базы.
    /// </summary>
    public DatabaseValidationIssue(
        string code,
        string message,
        DatabaseValidationSeverity severity = DatabaseValidationSeverity.Warning,
        int index = -1)
    {
        Code = code ?? string.Empty;
        Message = message ?? string.Empty;
        Severity = severity;
        Index = index;
    }
}
