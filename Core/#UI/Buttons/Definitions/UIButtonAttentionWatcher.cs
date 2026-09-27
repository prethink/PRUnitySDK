using UnityEngine;

/// <summary>
/// Наблюдает условие одной кнопки. Владелец регистрирует и снимает задачу вместе с UI.
/// </summary>
public sealed class UIButtonAttentionWatcher : WatcherTask<bool>
{
    private readonly ICondition condition;
    private readonly Enumeration key;
    private readonly float interval;

    public UIButtonAttentionWatcher(Enumeration key, ICondition condition, float interval)
    {
        this.key = key;
        this.condition = condition;
        this.interval = Mathf.Max(0.1f, interval);
    }

    /// <inheritdoc />
    public override Enumeration Key => key;
    /// <inheritdoc />
    public override float RepeatSeconds => interval;
    /// <inheritdoc />
    public override bool Read() => condition != null && condition.Evaluate();
}
