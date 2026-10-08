#if !PRSDK_DISABLE_YG2
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using YG;

/// <summary>
/// Реализация метрик для YGPlugin.
/// </summary>
/// <remarks>
/// Сериализатор плагина понимает только <c>int</c>, <c>float</c>, <c>double</c>, <c>bool</c>,
/// строки, <c>IList&lt;object&gt;</c> и <c>IDictionary&lt;string, object&gt;</c>. Остальное он
/// пишет через <c>ToString()</c> без экранирования, а пустой вложенный словарь ломает JSON,
/// и тогда цель не уходит совсем. Поэтому дерево параметров сначала приводится к этим типам.
/// </remarks>
public class YGMetrics : MetricBase
{
    /// <summary>
    /// Приставка идентификатора цели: в Метрику событие <c>level_up</c> уходит как <c>_level_up</c>.
    /// </summary>
    /// <remarks>
    /// Цели в интерфейсе Яндекс Метрики заводят условием «идентификатор содержит». Без приставки
    /// условие «содержит level_up» цепляет и любую цель, в чьём имени эта строка стоит посередине;
    /// с подчёркиванием в начале имя игры отличимо, и одной записью ловится вся её группа.
    /// <para>
    /// Ставится здесь, в одном месте, а не в каждом вызове: код игры называет события как раньше.
    /// </para>
    /// </remarks>
    public const string GoalPrefix = "_";

    #region Базовый класс

    /// <inheritdoc />
    public override void Send(string eventName)
    {
        eventName = GetGoalId(eventName);

        YG2.MetricaSend(eventName);
        ReportSend(eventName);
    }

    /// <inheritdoc />
    public override void Send(string eventName, IReadOnlyDictionary<string, object> eventParams)
    {
        eventName = GetGoalId(eventName);

        Dictionary<string, object> tree = new();

        if (eventParams != null)
        {
            foreach (KeyValuePair<string, object> pair in eventParams)
                Add(tree, pair.Key, pair.Value);
        }

        // Пустую строку параметров JS-часть плагина считает ошибкой и цель не отправляет.
        if (tree.Count == 0)
            YG2.MetricaSend(eventName);
        else
            YG2.MetricaSend(eventName, tree);

        ReportSend(eventName, tree);
    }

    #endregion

    #region Методы

    /// <summary>
    /// Идентификатор цели в Метрике: имя события с приставкой.
    /// </summary>
    /// <remarks>
    /// Имя, уже начинающееся с приставки, не трогается: второй раз она не добавляется.
    /// </remarks>
    private static string GetGoalId(string eventName)
    {
        return string.IsNullOrEmpty(eventName) || eventName.StartsWith(GoalPrefix, StringComparison.Ordinal)
            ? eventName
            : GoalPrefix + eventName;
    }

    private static void Add(Dictionary<string, object> tree, object key, object value)
    {
        string name = key?.ToString();
        object normalized = Normalize(value);

        if (!string.IsNullOrEmpty(name) && normalized != null)
            tree[name] = normalized;
    }

    /// <returns>Значение в типе, который понимает плагин, или <c>null</c>, если его нужно пропустить.</returns>
    private static object Normalize(object value)
    {
        switch (value)
        {
            case null:
                return null;
            case string or bool or int or float or double:
                return value;
            case Enum enumValue:
                return enumValue.ToString();
            case byte or sbyte or short or ushort:
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            case uint or long or ulong:
                double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return number >= int.MinValue && number <= int.MaxValue ? Convert.ToInt32(value, CultureInfo.InvariantCulture) : number;
            case decimal decimalValue:
                return (double)decimalValue;
            case IDictionary dictionary:
                Dictionary<string, object> tree = new();

                foreach (DictionaryEntry entry in dictionary)
                    Add(tree, entry.Key, entry.Value);

                return tree.Count > 0 ? tree : null;
            case IEnumerable items:
                List<object> list = new();

                foreach (object item in items)
                {
                    object normalized = Normalize(item);

                    // Словари внутри списка плагин не сериализует.
                    if (normalized is IDictionary)
                        PRLog.WriteWarning(typeof(YGMetrics), "Словарь внутри списка параметров метрики пропущен: плагин его не сериализует.");
                    else if (normalized != null)
                        list.Add(normalized);
                }

                return list;
            default:
                return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }

    #endregion
}
#endif
