using System;
using UnityEngine;

/// <summary>
/// Подпись условия для интерфейса: что нужно, чтобы оно выполнилось.
/// </summary>
/// <remarks>
/// Не готовая строка, а её части: перевод отдельно, числа отдельно, иконка отдельно.
/// Подпись висит на экране, язык могут сменить прямо при ней, а у сокращённого числа
/// переводится разряд - собранная заранее строка осталась бы от прежнего языка.
/// </remarks>
public sealed class ConditionDescription
{
    /// <summary>
    /// Перевод подписи; подставляемое место пишется форматом целиком.
    /// </summary>
    public ILocalizationProvider Text { get; }

    /// <summary>
    /// Аргументы подписи. Функцией, а не готовым массивом: их пересобирают на каждую
    /// смену языка.
    /// </summary>
    public Func<string[]> Args { get; }

    /// <summary>
    /// Картинка рядом с подписью: иконка ресурса, значок правила. Может отсутствовать.
    /// </summary>
    public Sprite Icon { get; }

    /// <summary>
    /// Собирает подпись условия.
    /// </summary>
    /// <param name="text">Перевод подписи.</param>
    /// <param name="args">Аргументы подписи либо <c>null</c>, если подставлять нечего.</param>
    /// <param name="icon">Картинка рядом с подписью либо <c>null</c>.</param>
    public ConditionDescription(ILocalizationProvider text, Func<string[]> args = null, Sprite icon = null)
    {
        Text = text;
        Args = args;
        Icon = icon;
    }
}
