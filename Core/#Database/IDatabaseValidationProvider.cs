using System.Collections.Generic;

/// <summary>
/// Предоставляет расширяемую проверку содержимого базы.
/// </summary>
public interface IDatabaseValidationProvider
{
    /// <summary>
    /// Возвращает найденные проблемы, не изменяя содержимое базы.
    /// </summary>
    IEnumerable<DatabaseValidationIssue> Validate();
}
