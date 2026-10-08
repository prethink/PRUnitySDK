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
    /// Отправить метрику веткой: событие → ключ → значение.
    /// </summary>
    /// <remarks>
    /// То же, что <see cref="SendBranch"/> с двумя уровнями. Раньше корнем ветки становился
    /// <paramref name="subKeyEvent"/>, и событие <c>emote</c> в отчёте называлось <c>play</c>,
    /// а покупки расходились по корням <c>start</c>, <c>success</c> и <c>failed</c>.
    /// </remarks>
    /// <param name="rootKeyEvent">Название события; оно же корень ветки.</param>
    /// <param name="subKeyEvent">Дочерний ключ события.</param>
    /// <param name="subValueEvent">Дочернее значение события.</param>
    public void Send(string rootKeyEvent, string subKeyEvent, string subValueEvent)
    {
        SendBranch(rootKeyEvent, subKeyEvent, subValueEvent);
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

    /// <summary>
    /// Отправить метрику одной веткой: событие → ключи по порядку → значение.
    /// </summary>
    /// <remarks>
    /// Корень ветки — имя события: отчёт Метрики строит дерево только из ключей параметров,
    /// а имени цели в нём нет. Ключи одного уровня расходятся в отчёте по несвязанным узлам,
    /// поэтому всё, что нужно видеть вместе, идёт одной веткой, а не кладётся рядом.
    /// <para>
    /// Сбой отправки не пробрасывается: метрика не должна срывать то, о чём сообщает.
    /// </para>
    /// </remarks>
    /// <example>
    /// <c>PRUnitySDK.Metric.SendBranch("shop_buy", "HatDefinition", "Chef's hat-864d…")</c> —
    /// ветка <c>shop_buy → HatDefinition → Chef's hat-864d…</c>.
    /// </example>
    /// <param name="eventName">Название события; оно же корень ветки.</param>
    /// <param name="path">Вложенные ключи; последний элемент — значение.</param>
    public void SendBranch(string eventName, params string[] path)
    {
        try
        {
            if (path == null || path.Length == 0)
            {
                Send(eventName);
                return;
            }

            object node = path[path.Length - 1];

            for (int index = path.Length - 2; index >= 0; index--)
                node = new Dictionary<string, object> { { path[index], node } };

            Send(eventName, new Dictionary<string, object> { { eventName, node } });
        }
        catch (System.Exception exception)
        {
            PRLog.WriteWarning(typeof(MetricBase), $"Метрика '{eventName}' не отправлена: {exception.Message}");
        }
    }

    /// <summary>
    /// Записывает завершённую передачу адаптеру в журнал Debug только в Editor.
    /// </summary>
    /// <param name="eventParams">Фактически переданные параметры после нормализации адаптером.</param>
    /// <param name="ignored">Адаптер намеренно пропустил отправку.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    protected void ReportSend(string eventName, IReadOnlyDictionary<string, object> eventParams = null, bool ignored = false)
    {
#if UNITY_EDITOR
        MetricDebugHistory.Record(eventName, eventParams, GetType().FullName, ignored);
#endif
    }
}
