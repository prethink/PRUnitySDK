using System.Collections.Generic;

/// <summary>
/// Переводимые названия характеристик для окон: «Сила», а не имя перечисления.
/// </summary>
/// <remarks>
/// Характеристику заводит игра, поэтому и название регистрирует она — один раз, до показа окна.
/// Без названия окно пишет имя перечисления, как раньше.
/// </remarks>
public static class StatLabels
{
    private static readonly Dictionary<string, ILocalizationProvider> Labels = new();

    /// <summary>
    /// Задаёт название характеристики. Повторная регистрация заменяет прежнее.
    /// </summary>
    public static void Register(Enumeration stat, ILocalizationProvider label)
    {
        if (stat == null || string.IsNullOrEmpty(stat.Value) || label == null) return;
        Labels[stat.Value] = label;
    }

    /// <summary>
    /// Название характеристики на текущем языке; без регистрации — имя перечисления.
    /// </summary>
    public static string Name(Enumeration stat)
    {
        if (stat == null) return "?";
        return Labels.TryGetValue(stat.Value ?? string.Empty, out ILocalizationProvider label) ? label.GetTranslate() : stat.Value;
    }
}
