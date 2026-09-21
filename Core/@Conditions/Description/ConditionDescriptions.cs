using System;
using System.Collections.Generic;

/// <summary>
/// Собирает подпись условия: по чему видно, что именно нужно игроку.
/// </summary>
/// <remarks>
/// Отдельно от самих условий намеренно. Условие отвечает на вопрос и ничего не знает
/// про экран; заведи оно подпись у себя, её пришлось бы писать каждой реализации,
/// включая те, которые никто не показывает.
/// <para>
/// Своё условие описывают через <see cref="Register{T}"/>. Незнакомое остаётся
/// без подписи - показывать «условие не выполнено» без объяснения незачем.
/// </para>
/// </remarks>
public static class ConditionDescriptions
{
    private static readonly Dictionary<Type, Func<ICondition, ConditionDescription>> Custom = new();

    /// <summary>
    /// Объявляет, как описывать условие своего типа.
    /// </summary>
    /// <remarks>
    /// Повторная регистрация заменяет прежнее описание - так его переопределяет игра,
    /// которой нужна своя формулировка.
    /// </remarks>
    /// <typeparam name="T">Тип условия.</typeparam>
    /// <param name="describe">Как собрать подпись; <c>null</c> в ответе означает «описывать нечего».</param>
    public static void Register<T>(Func<T, ConditionDescription> describe)
        where T : ICondition
    {
        if (describe == null)
            return;

        Custom[typeof(T)] = condition => describe((T)condition);
    }

    /// <summary>
    /// Снимает описание типа условия.
    /// </summary>
    /// <typeparam name="T">Тип условия.</typeparam>
    public static void Unregister<T>()
        where T : ICondition
    {
        Custom.Remove(typeof(T));
    }

    /// <summary>
    /// Подпись условия либо <c>null</c>, если показывать нечего.
    /// </summary>
    /// <remarks>
    /// Встроенные формы одного правила описываются одинаково: у ассета и у встроенного
    /// условия по ресурсу подпись общая, различается только хранение.
    /// </remarks>
    /// <param name="condition">Условие.</param>
    public static ConditionDescription Get(ICondition condition)
    {
        if (condition == null)
            return null;

        if (Custom.TryGetValue(condition.GetType(), out Func<ICondition, ConditionDescription> describe))
            return describe(condition);

        return condition switch
        {
            ResourceCondition resource => Resource(resource.Resource, resource.Comparison, resource.Amount),
            ResourceInlineCondition inline => Resource(inline.Resource, inline.Comparison, inline.Amount),
            AssetCondition asset => Get(asset.Condition),
            AllCondition all => FromSet(all.Conditions),
            AnyCondition any => FromSet(any.Conditions),
            ConditionCollection collection => FromSet(collection.Conditions),
            _ => null
        };
    }

    /// <summary>
    /// Подпись набора условий.
    /// </summary>
    /// <remarks>
    /// Показывается одно - то, которое ещё не выполнено: игроку нужно знать, что сделать
    /// дальше, а не весь список правил. Когда выполнены все, остаётся первое описуемое,
    /// иначе подпись пропадала бы в момент выполнения и цель выглядела бы сломанной.
    /// </remarks>
    /// <param name="conditions">Вложенные условия.</param>
    private static ConditionDescription FromSet(IEnumerable<ICondition> conditions)
    {
        if (conditions == null)
            return null;

        ConditionDescription first = null;

        foreach (ICondition condition in conditions)
        {
            ConditionDescription description = Get(condition);

            if (description == null)
                continue;

            if (!condition.Evaluate())
                return description;

            first ??= description;
        }

        return first;
    }

    /// <summary>
    /// Подпись требования по ресурсу: иконка ресурса и число.
    /// </summary>
    /// <remarks>
    /// Невыбранный ресурс никого не запирает, поэтому и подписи у такого условия нет.
    /// <para>
    /// Число отдаётся функцией: у сокращённого числа переводится разряд, и на смене
    /// языка подпись пересобирается вместе с ним.
    /// </para>
    /// </remarks>
    private static ConditionDescription Resource(
        ResourceItemDefinition resource,
        ConditionComparison comparison,
        long amount)
    {
        if (resource == null)
            return null;

        return new ConditionDescription(
            ConditionLabels.Requirement(comparison),
            () => new[] { NumberConverter.FormatNumber(amount) },
            resource.Icon);
    }
}
