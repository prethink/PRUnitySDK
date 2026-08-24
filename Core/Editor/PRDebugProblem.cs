using System;
using UnityEngine;

/// <summary>
/// Описывает одну диагностическую проблему PRUnitySDK.
/// </summary>
public sealed class PRDebugProblem
{
    /// <summary>
    /// Важность проблемы.
    /// </summary>
    public PRDebugProblemSeverity Severity { get; }

    /// <summary>
    /// Подсистема, в которой обнаружена проблема.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Стабильный код для поиска и автоматической обработки.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Человекочитаемое описание актуального состояния.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Связанный Unity-объект, который можно выбрать в Inspector.
    /// </summary>
    public UnityEngine.Object Target { get; }

    /// <summary>
    /// Тип реализации, исходник которого можно открыть из Debug-окна.
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// Создаёт диагностическую проблему.
    /// </summary>
    public PRDebugProblem(PRDebugProblemSeverity severity, string category, string code, string message,
        UnityEngine.Object target = null, Type sourceType = null)
    {
        Severity = severity;
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
        Code = string.IsNullOrWhiteSpace(code) ? "Unknown" : code;
        Message = message ?? string.Empty;
        Target = target;
        SourceType = sourceType;
    }
}
