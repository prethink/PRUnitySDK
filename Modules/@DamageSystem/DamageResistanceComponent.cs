using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Применяет сопротивления к урону своей сущности до изменения здоровья.
/// </summary>
[RequireComponent(typeof(EntityBase))]
public sealed class DamageResistanceComponent : PRMonoBehaviour, IHookListener<DamageHookEvent>
{
    /// <summary>
    /// Одно правило сопротивления для набора типов урона.
    /// </summary>
    [Serializable]
    public struct ResistanceRule
    {
        /// <summary>
        /// Маска типов урона, активирующих правило.
        /// </summary>
        [Tooltip("Любой совпавший флаг активирует правило. Generic совпадает только с Generic.")]
        public DamageType DamageTypes;

        /// <summary>
        /// Множитель применяемого урона: 0 — иммунитет, 1 — без изменений.
        /// </summary>
        [Min(0f)]
        [Tooltip("0 полностью поглощает урон, 1 оставляет его без изменений.")]
        public float DamageMultiplier;
    }

    [SerializeField] private List<ResistanceRule> resistances = new();

    /// <summary>
    /// Порядок выполнения обработчика в глобальном конвейере хуков.
    /// </summary>
    [field: SerializeField] public int Order { get; private set; }

    private EntityBase entity;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();
        entity = GetComponent<EntityBase>();
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
    /// Применяет подходящие правила сопротивления к урону своей сущности.
    /// </summary>
    /// <param name="eventArgs">Изменяемый контекст попытки нанесения урона.</param>
    public void HandleHook(DamageHookEvent eventArgs)
    {
        // Сравнение идёт через ReferenceEquals: Victim объявлен как IEntity, и оператор !=
        // выбирается для интерфейса, минуя перегрузку UnityEngine.Object. Компилятор
        // предупреждал об этом (CS0252), а поведение зависело бы от статического типа.
        if (!ReferenceEquals(eventArgs.Victim, entity) || eventArgs.DamageProvider == null)
            return;

        var data = eventArgs.DamageProvider.GetDamageData().Clone();
        if (data.RawDamage == 0f && data.Damage != 0f)
            data.RawDamage = data.Damage;

        var damageBeforeResistance = data.Damage;

        foreach (var resistance in resistances)
        {
            if (!Matches(data.DamageType, resistance.DamageTypes))
                continue;

            data.Damage *= Mathf.Max(0f, resistance.DamageMultiplier);
        }

        data.AbsorbedDamage += Mathf.Max(0f, damageBeforeResistance - data.Damage);
        eventArgs.ModifyDamage(this, new CommonDamage(data));
    }

    private static bool Matches(DamageType damageType, DamageType ruleTypes)
    {
        if (ruleTypes == DamageType.Generic)
            return damageType == DamageType.Generic;

        return (damageType & ruleTypes) != 0;
    }
}
