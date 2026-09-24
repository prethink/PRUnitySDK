using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Правило урона: выбирает удары фильтрами и что-то с ними делает.
/// </summary>
/// <remarks>
/// Обычный объект, а не компонент: одно и то же правило кладут в настройки проекта,
/// на сцену (<see cref="DamageRuleComponent"/>) или создают в коде — и всё это попадает
/// в один сервис, <see cref="DamageRules"/>. Сервис ставит правило в общую очередь хуков
/// урона (<see cref="DamageHookEvent"/>), поэтому правило видит любой удар — касание,
/// ближний бой, снаряд, взрыв — и стоит в том же порядке, что условия, неуязвимость
/// и сопротивления.
/// <para>
/// Пустой фильтр пропускает всех: правило без настроек касается каждого удара.
/// </para>
/// </remarks>
[Serializable]
public abstract class DamageRule
{
    [SerializeField]
    [Tooltip("Порядок среди хуков урона: меньше - раньше. Отказ в ударе принято ставить на -200, правку урона - на 0.")]
    private int order;

    [SerializeField]
    [Tooltip("Чьи удары касаются правила. Ничего не отмечено - любого атакующего, в том числе не игрока.")]
    private PlayerTypeFlags attackers = PlayerTypeFlags.None;

    [SerializeField]
    [Tooltip("По каким видам сущностей. Пусто - по любым.")]
    private List<EnumerationReference<EntityTypeEnumerations>> victimTypes = new();

    /// <summary>
    /// Порядок среди хуков урона.
    /// </summary>
    /// <remarks>
    /// Читается, когда правило попадает в сервис: поменять его на ходу — значит убрать
    /// правило и добавить снова.
    /// </remarks>
    public int Order
    {
        get => order;
        set => order = value;
    }

    /// <summary>
    /// Чьи удары касаются правила; <see cref="PlayerTypeFlags.None"/> — любого атакующего.
    /// </summary>
    public PlayerTypeFlags Attackers
    {
        get => attackers;
        set => attackers = value;
    }

    /// <summary>
    /// Добавляет вид целей, по которым действует правило.
    /// </summary>
    public DamageRule AddVictimType(Enumeration entityType)
    {
        if (entityType == null)
            return this;

        var reference = new EnumerationReference<EntityTypeEnumerations>();
        reference.Set(entityType);
        victimTypes.Add(reference);

        return this;
    }

    /// <summary>
    /// Обрабатывает удар: подходящий по фильтрам передаётся в <see cref="Apply"/>.
    /// </summary>
    /// <param name="eventArgs">Изменяемый контекст удара.</param>
    /// <param name="source">Слушатель хука, от имени которого правило меняет удар.</param>
    public void Handle(DamageHookEvent eventArgs, IHookListener source)
    {
        if (eventArgs == null || eventArgs.Result == HookResult.Supercede || eventArgs.DamageProvider == null)
            return;

        if (!MatchesAttacker(eventArgs.Attacker) || !MatchesVictim(eventArgs.Victim))
            return;

        Apply(eventArgs, source);
    }

    /// <summary>
    /// Что правило делает с подходящим ударом.
    /// </summary>
    /// <param name="eventArgs">Изменяемый контекст удара.</param>
    /// <param name="source">Слушатель хука, от имени которого меняется удар.</param>
    protected abstract void Apply(DamageHookEvent eventArgs, IHookListener source);

    private bool MatchesAttacker(IEntity attacker)
    {
        if (attackers == PlayerTypeFlags.None)
            return true;

        return attacker is PlayerBase player && (attackers & PlayerBase.ConvertToFlag(player.PlayerType)) != 0;
    }

    private bool MatchesVictim(IEntity victim)
    {
        bool anyType = true;

        foreach (EnumerationReference<EntityTypeEnumerations> reference in victimTypes)
        {
            // Пустая строка списка — недонастроенный слот, а не «любой вид»: иначе она
            // превратилась бы в значение набора по умолчанию.
            if (reference == null || string.IsNullOrEmpty(reference.Value))
                continue;

            anyType = false;

            if (!victim.IsNull() && reference.ToEnumeration() == victim.EntityType)
                return true;
        }

        return anyType;
    }
}
