using System;
using UnityEditor;

/// <summary>
/// Одна проблема, найденная проверкой проекта.
/// </summary>
public sealed class ProjectValidationIssue
{
    /// <summary>
    /// Насколько это серьёзно.
    /// </summary>
    public MessageType Severity { get; }

    /// <summary>
    /// Что не так.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Объект, который окно выделит по клику.
    /// </summary>
    public UnityEngine.Object Target { get; }

    /// <summary>
    /// Подпись кнопки исправления; <c>null</c>, если чинить нечем.
    /// </summary>
    public string FixTitle { get; }

    /// <summary>
    /// Исправление проблемы.
    /// </summary>
    public Action Fix { get; }

    public ProjectValidationIssue(
        MessageType severity,
        string message,
        UnityEngine.Object target = null,
        string fixTitle = null,
        Action fix = null)
    {
        Severity = severity;
        Message = message ?? string.Empty;
        Target = target;
        FixTitle = fixTitle;
        Fix = fix;
    }
}
