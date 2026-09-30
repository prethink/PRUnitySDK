using System;
using UnityEngine;

/// <summary>
/// Результат проверки или выполнения действия с необязательной причиной отказа.
/// </summary>
public readonly struct ActionResult
{
    /// <summary>
    /// Успешный исход проверки или выполнения действия.
    /// </summary>
    public bool IsSuccess { get; }
    /// <summary>
    /// Отказ; значение по умолчанию также считается отказом.
    /// </summary>
    public bool IsFailed => !IsSuccess;

    /// <summary>
    /// Причина отказа для SetLocalization; может отсутствовать.
    /// </summary>
    public ILocalizationProvider FailureReason { get; }

    /// <summary>
    /// Аргументы описания, пересчитываемые при смене языка.
    /// </summary>
    public Func<string[]> FailureReasonArgs { get; }

    /// <summary>
    /// Необязательная иконка рядом с причиной отказа.
    /// </summary>
    public Sprite FailureIcon { get; }

    public static ActionResult Success { get; } = new(true, null, null, null);

    private ActionResult(bool isSuccess, ILocalizationProvider failureReason, Func<string[]> failureReasonArgs, Sprite failureIcon)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        FailureReasonArgs = failureReasonArgs;
        FailureIcon = failureIcon;
    }

    public static ActionResult Fail(ILocalizationProvider reason = null, Func<string[]> args = null, Sprite icon = null)
    {
        return new ActionResult(false, reason, args, icon);
    }
}
