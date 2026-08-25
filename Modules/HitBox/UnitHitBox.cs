using UnityEngine;

/// <summary>
/// Хитбокс живой сущности с зоной и множителем урона.
/// </summary>
public class UnitHitBox : EntityHitBoxBase
{
    /// <summary>
    /// Анатомическая зона, представленная этим хитбоксом.
    /// </summary>
    [field: SerializeField] public HitGroup HitGroup { get; private set; } = HitGroup.Generic;

    /// <summary>
    /// Множитель входящего урона для этой зоны.
    /// </summary>
    [field: SerializeField, Min(0f)] public float DamageMultiplier { get; private set; } = 1f;

    /// <summary>
    /// Следует ли помечать попадание как критическое.
    /// Для зоны <see cref="global::HitGroup.Head"/> включается по умолчанию, но
    /// флаг можно снять: правило про голову задаётся настройкой, а не кодом,
    /// иначе выключить крит для отдельного юнита было бы нельзя.
    /// </summary>
    [field: SerializeField] public bool IsCritical { get; private set; }

    /// <summary>
    /// Выставляет значения по умолчанию для новой зоны: голова считается
    /// критической, остальные - нет.
    /// </summary>
    private void Reset()
    {
        IsCritical = HitGroup == global::HitGroup.Head;
    }

    /// <summary>
    /// Добавляет к урону зону, множитель и при необходимости критический флаг.
    /// </summary>
    /// <param name="damage">Исходный провайдер урона.</param>
    /// <returns>Зональный декоратор урона.</returns>
    public override IDamageProvider GetHandledDamage(IDamageProvider damage)
    {
        return new HitGroupDamageDecorator(
            damage,
            HitGroup,
            DamageMultiplier,
            IsCritical);
    }
}
