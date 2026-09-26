using System.Collections.Generic;

/// <summary>
/// Базовый класс работы с метриками.
/// </summary>
/// <remarks>
/// Реализация обязана поддержать два вида отправки: без параметров и с деревом параметров.
/// Остальные перегрузки сводятся к дереву здесь.
/// </remarks>
public abstract class MetricBase
{
    /// <summary>
    /// Отправить метрику.
    /// </summary>
    /// <param name="eventName">Название события.</param>
    public abstract void Send(string eventName);

    /// <summary>
    /// Отправить метрику с деревом параметров.
    /// </summary>
    /// <remarks>
    /// Значения: строки, числа, <c>bool</c>, перечисления, вложенные словари со строковыми
    /// ключами и списки. <c>null</c> и пустые вложенные словари пропускаются.
    /// <para>
    /// В Яндекс Метрике строка на последнем уровне становится отдельным узлом отчёта, а число
    /// суммируется в узле своего ключа. Уровень, по которому игроков нужно разложить
    /// (<c>"level": "5"</c>), передавайте строкой, а величину для суммы и среднего числом.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// PRUnitySDK.Metric.Send("ui_interaction", new Dictionary&lt;string, object&gt;
    /// {
    ///     { "action", "click" },
    ///     { "user", new Dictionary&lt;string, object&gt; { { "level", "5" }, { "experience", 1020 } } }
    /// });
    /// </code>
    /// </example>
    /// <param name="eventName">Название события.</param>
    /// <param name="eventParams">Параметры события.</param>
    public abstract void Send(string eventName, IReadOnlyDictionary<string, object> eventParams);

    /// <summary>
    /// Отправить метрику.
    /// </summary>
    /// <param name="rootKeyEvent">Корневое название события.</param>
    /// <param name="subKeyEvent">Дочерний ключ события.</param>
    /// <param name="subValueEvent">Дочернее значение события.</param>
    public void Send(string rootKeyEvent, string subKeyEvent, string subValueEvent)
    {
        Send(rootKeyEvent, new Dictionary<string, object> { { subKeyEvent, subValueEvent } });
    }

    /// <summary>
    /// Отправить метрику.
    /// </summary>
    /// <param name="eventName">Название события.</param>
    /// <param name="eventParams">Параметры события.</param>
    public void Send(string eventName, Dictionary<string, string> eventParams)
    {
        var tree = new Dictionary<string, object>();

        if (eventParams != null)
        {
            foreach (KeyValuePair<string, string> pair in eventParams)
                tree[pair.Key] = pair.Value;
        }

        Send(eventName, tree);
    }
}
