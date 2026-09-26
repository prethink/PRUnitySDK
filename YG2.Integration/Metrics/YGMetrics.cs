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
    #region Базовый класс

    /// <inheritdoc />
    public override void Send(string eventName)
    {
        YG2.MetricaSend(eventName);
    }

    /// <inheritdoc />
    public override void Send(string eventName, IReadOnlyDictionary<string, object> eventParams)
    {
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
    }

    #endregion

    #region Методы

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
