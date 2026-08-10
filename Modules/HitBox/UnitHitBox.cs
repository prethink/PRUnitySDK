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
    /// Следует ли всегда помечать попадание как критическое.
    /// </summary>
    [field: SerializeField] public bool IsCritical { get; private set; }

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
            IsCritical || HitGroup == global::HitGroup.Head);
    }
}
