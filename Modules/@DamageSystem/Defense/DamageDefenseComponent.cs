using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Броня и сопротивления сущности: снижает урон до того, как он дойдёт до здоровья.
/// </summary>
/// <remarks>
/// <para>
/// База — поля компонента. Сверху модификаторы <see cref="StatModifierCollector"/> сущности по ключам
/// <see cref="DefenseStatEnumerations"/>: экипировка, питомец, бустер, способность. Пробивание берётся у атакующего
/// тем же путём. Формулы и потолки — <see cref="DefenseSettings"/>, расчёт — <see cref="DefenseMath"/>.
/// </para>
/// <para>
/// Удар нескольких типов бьёт в самое слабое место. Броня закрывает только физический урон.
/// </para>
/// </remarks>
[RequireComponent(typeof(EntityBase))]
public sealed class DamageDefenseComponent : PRMonoBehaviour, IHookListener<DamageHookEvent>
{
    /// <summary>
    /// Сопротивление набору типов урона.
    /// </summary>
    [Serializable]
    public struct ResistanceEntry
    {
        /// <summary>
        /// Типы урона, которым задано сопротивление. Несколько флагов — то же число каждому.
        /// </summary>
        [Tooltip("Типы урона, которым задано сопротивление. Несколько флагов — то же число каждому. " +
                 "Generic и пометки (AreaOfEffect, Critical, TimeBased) сопротивления не имеют.")]
        public DamageType DamageTypes;

        /// <summary>
        /// Сопротивление, %: 50 — половина урона, −50 — в полтора раза больше.
        /// </summary>
        [Tooltip("Сопротивление, %: 50 — половина урона, −50 — уязвимость, урон в полтора раза больше. " +
                 "Потолок — в настройках проекта (Defense → Max Resistance).")]
        public float Percent;

        /// <summary>
        /// Создаёт сопротивление.
        /// </summary>
        public ResistanceEntry(DamageType damageTypes, float percent)
        {
            DamageTypes = damageTypes;
            Percent = percent;
        }
    }

    [SerializeField, Min(0f)]
    [Tooltip("Броня: снижает физический урон. Броня, равная константе проекта (Defense → Armor Constant, 100), снимает половину.")]
    private float armor;

    [SerializeField]
    [Tooltip("Сопротивление всем типам урона, %. Прибавляется к сопротивлению каждого типа.")]
    private float allResistance;

    [SerializeField]
    [Tooltip("Сопротивления по типам урона. Записи с одним типом складываются.")]
    private List<ResistanceEntry> resistances = new();

    [SerializeField]
    [Tooltip("Иммунитет: урон этих типов не проходит совсем, пробивание его не снимает. " +
             "Удар нескольких типов всё равно бьёт в самое слабое место.")]
    private DamageType immunities;

    /// <summary>
    /// Порядок в конвейере хуков урона: правит число, поэтому 0.
    /// </summary>
    [field: SerializeField] public int Order { get; private set; }

    private EntityBase entity;
    private StatModifierCollector collector;
    private Func<DamageType, float> resistanceOf;

    /// <summary>
    /// Броня с модификаторами.
    /// </summary>
    public float Armor => Mathf.Max(0f, Stat(DefenseStatEnumerations.Armor, Collector(), armor));

    /// <summary>
    /// Доля физического урона, которую сейчас снимает броня (без пробивания атакующего).
    /// </summary>
    public float ArmorReduction
    {
        get
        {
            DefenseSettings settings = DefenseSettings.Current;
            return DefenseMath.ArmorReduction(Armor, 0f, settings.ArmorConstant, settings.MaxArmorReduction);
        }
    }

    /// <summary>
    /// Типы урона, к которым у сущности иммунитет.
    /// </summary>
    public DamageType Immunities => immunities;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        entity = GetComponent<EntityBase>();
        resistanceOf = GetResistance;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RegisterHook();
    }

    protected override void OnDisable()
    {
        UnRegisterHook();
        base.OnDisable();
    }

    /// <summary>
    /// Регистрирует компонент в <see cref="HookManager"/>.
    /// </summary>
    public void RegisterHook()
    {
        HookManager.Instance.Register(this);
    }

    /// <summary>
    /// Удаляет компонент из <see cref="HookManager"/>.
    /// </summary>
    public void UnRegisterHook()
    {
        HookManager.Instance.Unregister(this);
    }

    /// <summary>
    /// Сопротивление одному типу урона с модификаторами, % (до потолка проекта).
    /// </summary>
    /// <param name="kind">Ровно один флаг из <see cref="DefenseMath.Kinds"/>.</param>
    public float GetResistance(DamageType kind)
    {
        Enumeration stat = DefenseStatEnumerations.ResistanceFor(kind);
        if (stat == null)
            return 0f;

        float own = 0f;
        foreach (ResistanceEntry entry in resistances)
        {
            if ((entry.DamageTypes & kind) != 0)
                own += entry.Percent;
        }

        Collector();
        return Stat(stat, collector, own) + Stat(DefenseStatEnumerations.AllResistance, collector, allResistance);
    }

    /// <summary>
    /// Сопротивление, которое сейчас действует на удар этого типа: в границах проекта, %.
    /// </summary>
    public float GetEffectiveResistance(DamageType kind)
    {
        DefenseSettings settings = DefenseSettings.Current;
        return DefenseMath.EffectiveResistance(GetResistance(kind), 0f, settings.MinResistance, settings.MaxResistance);
    }

    /// <summary>
    /// Гасит ли иммунитет удар этого типа целиком: все его типы под иммунитетом (<see cref="DefenseMath.IsImmune"/>).
    /// Огненный взрыв по цели с иммунитетом только к огню — <c>false</c>: удар бьёт в самое слабое место.
    /// </summary>
    public bool IsImmune(DamageType damageType) => DefenseMath.IsImmune(damageType, immunities);

    /// <summary>
    /// Считает защиту от удара, ничего не меняя: для подсказок, окна характеристик, предпросмотра.
    /// </summary>
    /// <param name="damageType">Тип удара.</param>
    /// <param name="attacker">Атакующий: его пробивание брони и сопротивлений; <c>null</c> — без пробивания.</param>
    public DefenseResult Evaluate(DamageType damageType, IEntity attacker = null)
    {
        resistanceOf ??= GetResistance;
        StatModifierCollector attackerStats = FindCollector(attacker);
        return DefenseMath.Calculate(
            damageType,
            resistanceOf,
            immunities,
            Armor,
            Stat(DefenseStatEnumerations.ArmorPenetration, attackerStats, 0f),
            Stat(DefenseStatEnumerations.ResistancePenetration, attackerStats, 0f),
            DefenseSettings.Current);
    }

    /// <summary>
    /// Пробивание брони атакующего, % (0–100). Для своей защиты в обход компонента — панциря босса с окном и подобного:
    /// предмет на пробивание должен действовать и на неё.
    /// </summary>
    public static float ArmorPenetrationOf(IEntity attacker) =>
        Mathf.Clamp(Stat(DefenseStatEnumerations.ArmorPenetration, FindCollector(attacker), 0f), 0f, 100f);

    /// <summary>
    /// Пробивание сопротивлений атакующего, %.
    /// </summary>
    public static float ResistancePenetrationOf(IEntity attacker) =>
        Mathf.Max(0f, Stat(DefenseStatEnumerations.ResistancePenetration, FindCollector(attacker), 0f));

    /// <summary>
    /// Задаёт базовую броню — сборщику уровня или масштабированию врага по этажу.
    /// Временные прибавки (бафф, предмет) — модификатором <see cref="StatModifierCollector"/>, а не здесь.
    /// </summary>
    public void SetBaseArmor(float value)
    {
        armor = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Задаёт базовое сопротивление: заменяет запись с тем же набором типов или добавляет новую.
    /// </summary>
    public void SetBaseResistance(DamageType damageTypes, float percent)
    {
        for (int index = 0; index < resistances.Count; index++)
        {
            if (resistances[index].DamageTypes != damageTypes)
                continue;

            resistances[index] = new ResistanceEntry(damageTypes, percent);
            return;
        }

        resistances.Add(new ResistanceEntry(damageTypes, percent));
    }

    /// <summary>
    /// Задаёт базовое сопротивление всем типам урона.
    /// </summary>
    public void SetBaseAllResistance(float percent)
    {
        allResistance = percent;
    }

    /// <summary>
    /// Задаёт иммунитеты.
    /// </summary>
    public void SetImmunities(DamageType damageTypes)
    {
        immunities = damageTypes;
    }

    /// <summary>
    /// Снижает урон по своей сущности бронёй и сопротивлениями.
    /// </summary>
    /// <param name="eventArgs">Изменяемый контекст попытки нанесения урона.</param>
    public void HandleHook(DamageHookEvent eventArgs)
    {
        // Victim объявлен как IEntity: оператор != выбирался бы для интерфейса, минуя UnityEngine.Object.
        if (eventArgs.Result == HookResult.Supercede || !ReferenceEquals(eventArgs.Victim, entity) || eventArgs.DamageProvider == null)
            return;

        DamageData data = eventArgs.DamageProvider.GetDamageData()?.Clone();
        if (data == null || data.Damage <= 0f)
            return;
        if (data.RawDamage == 0f)
            data.RawDamage = data.Damage;

        float multiplier = Evaluate(data.DamageType, eventArgs.Attacker).Multiplier;
        if (Mathf.Approximately(multiplier, 1f))
            return;

        float before = data.Damage;
        data.Damage = before * multiplier;
        data.AbsorbedDamage += Mathf.Max(0f, before - data.Damage);
        eventArgs.ModifyDamage(this, Damages.From(data));
    }

    private StatModifierCollector Collector()
    {
        if (collector == null && entity != null)
            collector = entity.GetComponentInChildren<StatModifierCollector>(true);

        return collector;
    }

    private static StatModifierCollector FindCollector(IEntity attacker)
    {
        if (attacker.IsNull())
            return null;

        GameObject gameObject = attacker.gameObject;
        return gameObject != null ? gameObject.GetComponentInChildren<StatModifierCollector>(true) : null;
    }

    private static float Stat(Enumeration stat, StatModifierCollector source, float baseValue)
    {
        return EntityStatsUtils.GetStat(stat, null, source, baseValue);
    }
}
