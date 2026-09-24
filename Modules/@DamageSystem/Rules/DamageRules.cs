using System.Collections.Generic;

/// <summary>
/// Правила урона игры: где они хранятся и как попадают к каждому удару.
/// </summary>
/// <remarks>
/// Один сервис на всю игру. Правила приходят из трёх мест и дальше неотличимы:
/// <list type="bullet">
/// <item>настройки проекта — <see cref="DamageRulesSettings"/>, действуют на всех сценах;</item>
/// <item>сцена — <see cref="DamageRuleComponent"/>, действует, пока компонент включён;</item>
/// <item>код — <see cref="Add"/> и <see cref="Remove(object)"/>, например на время босса.</item>
/// </list>
/// <para>
/// К удару правила приходят через уже существующий хук урона: каждое правило
/// регистрируется в <see cref="HookManager"/> отдельным слушателем со своим
/// <see cref="DamageRule.Order"/>. <c>HealthComponent.TakeDamage</c> публикует хук на
/// каждом ударе, поэтому сервис спрашивается всегда, а правила стоят в общем порядке
/// с условиями, неуязвимостью и сопротивлениями: отказ раньше правки, правка раньше
/// наблюдения. Второго конвейера рядом с хуками нет.
/// </para>
/// </remarks>
public class DamageRules : SingletonProviderBase<DamageRules>
{
    /// <summary>
    /// Правило в очереди хуков: переходник между объектом правила и <see cref="HookManager"/>.
    /// </summary>
    private sealed class Entry : IHookListener<DamageHookEvent>
    {
        public DamageRule Rule { get; }
        public object Owner { get; }

        public Entry(DamageRule rule, object owner)
        {
            Rule = rule;
            Owner = owner;
        }

        public int Order => Rule.Order;

        public void RegisterHook()
        {
            HookManager.Instance.Register(this);
        }

        public void UnRegisterHook()
        {
            HookManager.Instance.Unregister(this);
        }

        public void HandleHook(DamageHookEvent eventArgs)
        {
            Rule.Handle(eventArgs, this);
        }
    }

    /// <summary>
    /// Владелец проектных правил: по нему они снимаются целиком при перечитывании настроек.
    /// </summary>
    private static readonly object ProjectOwner = new();

    private readonly List<Entry> entries = new();

    /// <summary>
    /// Все действующие правила.
    /// </summary>
    public IEnumerable<DamageRule> Rules
    {
        get
        {
            foreach (Entry entry in entries)
                yield return entry.Rule;
        }
    }

    /// <summary>
    /// Добавляет правило.
    /// </summary>
    /// <remarks>
    /// Владелец нужен, чтобы потом снять ровно свои правила: сцена снимает сценовые,
    /// режим — свои, и чужие при этом остаются. То же правило повторно не добавляется.
    /// </remarks>
    /// <param name="rule">Правило.</param>
    /// <param name="owner">Кто добавил правило.</param>
    public void Add(DamageRule rule, object owner)
    {
        if (rule == null || owner == null || Contains(rule))
            return;

        var entry = new Entry(rule, owner);
        entries.Add(entry);
        entry.RegisterHook();
    }

    /// <summary>
    /// Снимает все правила владельца.
    /// </summary>
    /// <returns>Сколько правил снято.</returns>
    public int Remove(object owner)
    {
        if (owner == null)
            return 0;

        int removed = 0;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(entries[i].Owner, owner))
                continue;

            entries[i].UnRegisterHook();
            entries.RemoveAt(i);
            removed++;
        }

        return removed;
    }

    /// <summary>
    /// Снимает одно правило.
    /// </summary>
    public bool Remove(DamageRule rule)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (!ReferenceEquals(entries[i].Rule, rule))
                continue;

            entries[i].UnRegisterHook();
            entries.RemoveAt(i);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Есть ли правило в сервисе.
    /// </summary>
    public bool Contains(DamageRule rule)
    {
        foreach (Entry entry in entries)
        {
            if (ReferenceEquals(entry.Rule, rule))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Ставит правила проекта, заменяя прежние.
    /// </summary>
    /// <remarks>
    /// Прежние снимаются целиком: настройки перечитываются при каждом старте SDK, и без
    /// этого правила копились бы при входе в Play Mode без перезагрузки домена.
    /// </remarks>
    public void SetProjectRules(IEnumerable<DamageRule> rules)
    {
        Remove(ProjectOwner);

        if (rules == null)
            return;

        foreach (DamageRule rule in rules)
            Add(rule, ProjectOwner);
    }
}
