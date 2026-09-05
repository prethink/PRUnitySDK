using System.Collections.Generic;

/// <summary>
/// Проверка проекта для окна «Проверка проекта».
/// </summary>
/// <remarks>
/// Реализации находятся автоматически, поэтому нужен конструктор без параметров.
/// Проверка работает с ассетами; для runtime-состояния есть <see cref="IPRDebugHealthCheck"/>.
/// </remarks>
public interface IProjectValidator
{
    /// <summary>
    /// Название группы в окне.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Найденные проблемы.
    /// </summary>
    IEnumerable<ProjectValidationIssue> Validate();
}
