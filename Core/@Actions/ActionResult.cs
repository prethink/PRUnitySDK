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
    /// Отказ, случившийся после того, как часть действия уже выполнилась.
    /// </summary>
    /// <remarks>
    /// Так отвечает набор действий, прерванный на середине: выданное назад не забрать,
    /// и повторять весь набор нельзя — первая часть выдалась бы дважды.
    /// </remarks>
    public bool IsPartiallyApplied { get; }

    /// <summary>
    /// Действие что-то изменило: выполнилось целиком или частично.
    /// </summary>
    /// <remarks>
    /// Этим отвечают на вопрос «выдано ли», когда повтор недопустим: награды, промокоды, покупки.
    /// </remarks>
    public bool HasApplied => IsSuccess || IsPartiallyApplied;

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

    public static ActionResult Success { get; } = new(true, false, null, null, null);

    private ActionResult(bool isSuccess, bool isPartiallyApplied, ILocalizationProvider failureReason,
        Func<string[]> failureReasonArgs, Sprite failureIcon)
    {
        IsSuccess = isSuccess;
        IsPartiallyApplied = isPartiallyApplied;
        FailureReason = failureReason;
        FailureReasonArgs = failureReasonArgs;
        FailureIcon = failureIcon;
    }

    public static ActionResult Fail(ILocalizationProvider reason = null, Func<string[]> args = null, Sprite icon = null)
    {
        return new ActionResult(false, false, reason, args, icon);
    }

    /// <summary>
    /// Тот же отказ, но с отметкой, что часть действия уже выполнилась.
    /// </summary>
    public ActionResult AsPartiallyApplied()
    {
        return IsSuccess ? this : new ActionResult(false, true, FailureReason, FailureReasonArgs, FailureIcon);
    }
}
