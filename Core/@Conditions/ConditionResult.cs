using System;
using UnityEngine;

/// <summary>
/// Исход проверки с локализуемой причиной отказа.
/// </summary>
/// <remarks>
/// Описание условия запрашивается только для интерфейса, а не на каждую проверку.
/// </remarks>
public readonly struct ConditionResult
{
    private readonly ILocalizationProvider reason;
    private readonly Func<string[]> args;
    private readonly Sprite icon;
    private readonly ICondition failedCondition;
    private readonly ConditionContextBase context;

    /// <summary>
    /// Успешный исход проверки условия.
    /// </summary>
    public bool IsSuccess { get; }
    /// <summary>
    /// Отказ; значение по умолчанию также считается отказом.
    /// </summary>
    public bool IsFailed => !IsSuccess;

    /// <summary>
    /// Описание отказа вместе с аргументами и иконкой; может отсутствовать.
    /// </summary>
    public ConditionDescription FailureDescription => IsSuccess ? null :
        failedCondition != null ? ConditionDescriptions.Get(failedCondition, context) :
        reason != null ? new ConditionDescription(reason, args, icon) : null;

    /// <summary>
    /// Перевод причины отказа; может отсутствовать.
    /// </summary>
    public ILocalizationProvider FailureReason => failedCondition == null ? reason : FailureDescription?.Text;
    /// <summary>
    /// Аргументы причины, пересчитываемые при смене языка.
    /// </summary>
    public Func<string[]> FailureReasonArgs => failedCondition == null ? args : FailureDescription?.Args;
    /// <summary>
    /// Необязательная иконка требования.
    /// </summary>
    public Sprite FailureIcon => failedCondition == null ? icon : FailureDescription?.Icon;

    public static ConditionResult Success { get; } = new(true, null, null, null, null, null);

    private ConditionResult(bool isSuccess, ILocalizationProvider reason, Func<string[]> args,
        Sprite icon, ICondition failedCondition, ConditionContextBase context)
    {
        IsSuccess = isSuccess;
        this.reason = reason;
        this.args = args;
        this.icon = icon;
        this.failedCondition = failedCondition;
        this.context = context;
    }

    public static ConditionResult Fail(ILocalizationProvider reason = null, Func<string[]> args = null, Sprite icon = null)
    {
        return new ConditionResult(false, reason, args, icon, null, null);
    }

    /// <summary>
    /// Сохраняет невыполненное условие без построения его описания.
    /// </summary>
    public static ConditionResult Fail(ICondition condition, ConditionContextBase context)
    {
        return new ConditionResult(false, null, null, null, condition, context ?? ConditionContextEmpty.Instance);
    }
}
